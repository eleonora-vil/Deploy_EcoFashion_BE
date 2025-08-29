using EcoFashionBackEnd.Common;
using EcoFashionBackEnd.Dtos.Wallet;
using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Extensions.NewFolder;
using EcoFashionBackEnd.Helpers;
using EcoFashionBackEnd.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Transactions;

namespace EcoFashionBackEnd.Services
{
    public class WalletService
    {
        private readonly IRepository<Wallet, int> _walletRepository;
        private readonly IRepository<WalletTransaction, int> _walletTransactionRepository;
        private readonly IVnPayService _vnPayService;
        private readonly IRepository<PaymentTransaction, int> _paymentTransactionRepository;


        public WalletService(
            IRepository<Wallet, int> walletRepository,
            IRepository<WalletTransaction, int> walletTransactionRepository,
            IVnPayService vnPayService            ,
            IRepository<PaymentTransaction, int> paymentTransactionRepository
            )
        {
            _walletRepository = walletRepository;
            _walletTransactionRepository = walletTransactionRepository;
            _vnPayService = vnPayService;
            _paymentTransactionRepository = paymentTransactionRepository;
        }
        public async Task<ApiResult<object>> CreateDepositAsync(int userId, double amount, HttpContext httpContext)
        {
            // Đảm bảo ví tồn tại cho người dùng
            var wallet = await _walletRepository
                .GetAll()
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };
                await _walletRepository.AddAsync(wallet);
                await _walletRepository.Commit();
            }

            // 1. Tạo transaction pending trong DB, liên kết đúng WalletId
            var transaction = new WalletTransaction
            {
                WalletId = wallet.WalletId,
                Amount = amount,
                BalanceBefore = wallet.Balance,
                BalanceAfter = wallet.Balance,
                Type = TransactionType.Deposit,
                Status = Entities.TransactionStatus.Pending,
                Description = $"Yêu cầu nạp {amount:N0} VND",
                CreatedAt = DateTime.Now
            };

            await _walletTransactionRepository.AddAsync(transaction);
            await _walletTransactionRepository.Commit();

            // 2. Build VNPay model
            var vnPayModel = new VnPaymentRequestModel
            {
                OrderId = transaction.Id, // dùng ID làm mã duy nhất
                Amount = amount,
                CreatedDate = DateTime.Now
            };

            var paymentUrl = await _vnPayService.CreateDepositPaymentUrlAsync(httpContext, vnPayModel);

            return ApiResult<object>.Succeed(new { PaymentUrl = paymentUrl });
        }


        public async Task<VnPaymentResponseModel> HandleVNPayDepositReturnAsync(IQueryCollection collection)
        {
            var response = _vnPayService.PaymentExecute(collection);

            if (!int.TryParse(response.OrderId, out int orderId))
                throw new Exception("Invalid OrderId from VNPay");


            var walletTransaction = await _walletTransactionRepository.GetByIdAsync(orderId) ;
            if (walletTransaction == null)
            {
                throw new Exception($"WalletTransaction not found for TxnRef: {response.TxnRef}");
            }

            var wallet = await _walletRepository.GetByIdAsync(walletTransaction.WalletId);
            if (wallet == null)
            {
                throw new Exception($"Wallet not found for user: {walletTransaction.WalletId}");
            }

            // Idempotent check: nếu WalletTransaction đã Success/Fail thì bỏ qua
            if (walletTransaction.Status == Entities.TransactionStatus.Success ||
                walletTransaction.Status == Entities.TransactionStatus.Fail)
            {
                return response;
            }

            if (response.VnPayResponseCode == "00")
            {
                // Thành công -> cộng tiền vào ví
                walletTransaction.Status = Entities.TransactionStatus.Success;
                walletTransaction.BalanceBefore = wallet.Balance;
                wallet.Balance += walletTransaction.Amount;
                wallet.LastUpdatedAt = DateTime.Now;
                walletTransaction.BalanceAfter = wallet.Balance;

                await _walletRepository.UpdateAsync(wallet);
                await _walletRepository.Commit();
            }
            else
            {
                // Thất bại
                walletTransaction.Status = Entities.TransactionStatus.Fail;
            }

            await _walletTransactionRepository.UpdateAsync(walletTransaction);
            await _walletTransactionRepository.Commit();

            return response;
        }


        public async Task<WalletTransactionDto> RequestWithdrawalAsync(int userId, double amount, string? description = null)
        {
            var wallet = await _walletRepository.GetAll()
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
                throw new Exception("Wallet not found");

            if (wallet.Balance < amount)
                throw new Exception("Insufficient balance");

            var transaction = new WalletTransaction
            {
                WalletId = wallet.WalletId,
                Amount = amount,
                BalanceBefore = wallet.Balance,
                BalanceAfter = wallet.Balance, // chưa trừ tiền
                Type = TransactionType.Withdrawal,
                Status = Entities.TransactionStatus.Pending,
                Description = description ?? $"Request withdrawal {amount} VND",
                CreatedAt = DateTime.UtcNow
            };

            await _walletTransactionRepository.AddAsync(transaction);
            await _walletTransactionRepository.Commit();

            return new WalletTransactionDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                BalanceBefore = transaction.BalanceBefore,
                BalanceAfter = transaction.BalanceAfter,
                Type = transaction.Type,
                Status = transaction.Status,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt,
                OrderId = transaction.OrderId,
                OrderGroupId = transaction.OrderGroupId
            }; 
        }


        public async Task<ApiResult<object>> CreateWithdrawalPaymentAsync(int walletTransactionId, HttpContext httpContext)
        {
            var withdrawalRequest = await _walletTransactionRepository.GetByIdAsync(walletTransactionId);
            if (withdrawalRequest == null)
                return ApiResult<object>.Fail("Không tìm thấy yêu cầu rút tiền");

            if (withdrawalRequest.Type != TransactionType.Withdrawal)
                return ApiResult<object>.Fail("TransactionId không phải yêu cầu rút tiền");

            if (withdrawalRequest.Status != Entities.TransactionStatus.Pending)
                return ApiResult<object>.Fail("Yêu cầu này đã được xử lý rồi");

            // Không thay đổi trạng thái/ số dư cho đến khi VNPay callback
            var vnPayModel = new VnPaymentRequestModel
            {
                OrderId = withdrawalRequest.Id,
                Amount = withdrawalRequest.Amount,
                CreatedDate = DateTime.Now
            };

            var paymentUrl = await _vnPayService.CreateWithdrawalPaymentUrlAsync(httpContext, vnPayModel);

            return ApiResult<object>.Succeed(new { PaymentUrl = paymentUrl });
        }

        public async Task<VnPaymentResponseModel> HandleVNPayWithdrawalReturnAsync(IQueryCollection collection)
        {
            var response = _vnPayService.PaymentExecute(collection);

            if (!int.TryParse(response.OrderId, out int orderId))
                throw new Exception("Invalid OrderId from VNPay");

            var withdrawalTxn = await _walletTransactionRepository.GetByIdAsync(orderId);
            if (withdrawalTxn == null)
                throw new Exception($"WalletTransaction not found for TxnRef: {response.TxnRef}");

            var wallet = await _walletRepository.GetByIdAsync(withdrawalTxn.WalletId);
            if (wallet == null)
                throw new Exception($"Wallet not found for WalletId: {withdrawalTxn.WalletId}");

            if (withdrawalTxn.Status == Entities.TransactionStatus.Success ||
                withdrawalTxn.Status == Entities.TransactionStatus.Fail)
            {
                return response;
            }

            if (response.VnPayResponseCode == "00")
            {
                withdrawalTxn.Status = Entities.TransactionStatus.Success;
                withdrawalTxn.BalanceBefore = wallet.Balance;
                wallet.Balance -= withdrawalTxn.Amount;
                wallet.LastUpdatedAt = DateTime.Now;
                withdrawalTxn.BalanceAfter = wallet.Balance;
                await _walletRepository.UpdateAsync(wallet);
                await _walletRepository.Commit();
            }
            else
            {
                withdrawalTxn.Status = Entities.TransactionStatus.Fail;
            }
            await _walletTransactionRepository.UpdateAsync(withdrawalTxn);
            await _walletTransactionRepository.Commit();

            return response;
        }

        // Unified: ensure wallet exists, then return it
        public async Task<Wallet> GetWalletByUserIdAsync(int userId)
        {
            var wallet = await _walletRepository.GetAll()
                .FirstOrDefaultAsync(w => w.UserId == userId);
            
            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };
                await _walletRepository.AddAsync(wallet);
                await _walletRepository.Commit();
            }
            
            return wallet;
        }

        public async Task<object> GetWalletSummaryAsync(int userId)
        {
            var wallet = await _walletRepository.GetAll().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null) return null;

            // Get recent transactions (last 10)
            var recentTransactions = await _walletTransactionRepository.GetAll()
                .Where(t => t.WalletId == wallet.WalletId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => new WalletTransactionDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    BalanceBefore = t.BalanceBefore,
                    BalanceAfter = t.BalanceAfter,
                    Type = t.Type,
                    Status = t.Status,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    OrderId = t.OrderId,
                    OrderGroupId = t.OrderGroupId
                })
                .ToListAsync();

            // Get total transaction count
            var totalTransactions = await _walletTransactionRepository.GetAll()
                .Where(t => t.WalletId == wallet.WalletId)
                .CountAsync();

            // Get monthly stats (current month)
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var monthlyTransactions = await _walletTransactionRepository.GetAll()
                .Where(t => t.WalletId == wallet.WalletId && 
                           t.CreatedAt.Month == currentMonth && 
                           t.CreatedAt.Year == currentYear)
                .ToListAsync();

            var deposited = monthlyTransactions.Where(t => t.Type == TransactionType.Deposit || t.Type == TransactionType.Refund)
                                              .Sum(t => t.Amount);
            var spent = monthlyTransactions.Where(t => t.Type == TransactionType.Payment || t.Type == TransactionType.Withdrawal)
                                          .Sum(t => t.Amount);

            return new
            {
                wallet = new
                {
                    walletId = wallet.WalletId,
                    userId = wallet.UserId,
                    balance = wallet.Balance,
                    status = wallet.Status.ToString(),
                    createdAt = wallet.CreatedAt,
                    lastUpdatedAt = wallet.LastUpdatedAt
                },
                recentTransactions = recentTransactions,
                totalTransactions = totalTransactions,
                monthlyStats = new
                {
                    deposited = deposited,
                    spent = spent,
                    net = deposited - spent
                }
            };
        }

        public async Task<decimal> GetBalanceAsync(int userId)
        {
            var wallet = await _walletRepository.GetAll().FirstOrDefaultAsync(w => w.UserId == userId);
            return (decimal)(wallet?.Balance ?? 0);
        }


        public async Task<List<WalletTransactionDto>> GetTransactionsAsync(int userId)
        {
            var wallet = await _walletRepository.GetAll().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null) return new List<WalletTransactionDto>();

            var transactions = await _walletTransactionRepository.GetAll()
                .Where(t => t.WalletId == wallet.WalletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return transactions.Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                Status = t.Status,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                OrderId = t.OrderId,
                OrderGroupId = t.OrderGroupId
            }).ToList();
        }

        public async Task<object> GetTransactionsPaginatedAsync(int userId, int page = 1, int pageSize = 20)
        {
            var wallet = await _walletRepository.GetAll().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null) 
                return new { transactions = new List<WalletTransactionDto>(), totalCount = 0, currentPage = page, totalPages = 0 };

            var totalCount = await _walletTransactionRepository.GetAll()
                .Where(t => t.WalletId == wallet.WalletId)
                .CountAsync();

            var transactions = await _walletTransactionRepository.GetAll()
                .Where(t => t.WalletId == wallet.WalletId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new WalletTransactionDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    BalanceBefore = t.BalanceBefore,
                    BalanceAfter = t.BalanceAfter,
                    Type = t.Type,
                    Status = t.Status,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    OrderId = t.OrderId,
                    OrderGroupId = t.OrderGroupId
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new
            {
                transactions = transactions,
                totalCount = totalCount,
                currentPage = page,
                totalPages = totalPages
            };
        }

        public async Task<WalletTransactionDto> GetTransactionByIdAsync(int transactionId, int userId)
        {
            var wallet = await _walletRepository.GetAll().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null) return null;

            var transaction = await _walletTransactionRepository.GetAll()
                .Where(t => t.Id == transactionId && t.WalletId == wallet.WalletId)
                .FirstOrDefaultAsync();

            if (transaction == null) return null;

            return new WalletTransactionDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                BalanceBefore = transaction.BalanceBefore,
                BalanceAfter = transaction.BalanceAfter,
                Type = transaction.Type,
                Status = transaction.Status,
                Description = transaction.Description ?? GetTransactionDescription(transaction.Type),
                CreatedAt = transaction.CreatedAt,
                OrderId = transaction.OrderId,
                OrderGroupId = transaction.OrderGroupId
            };
        }

        private string GetTransactionDescription(TransactionType type)
        {
            return type switch
            {
                TransactionType.Deposit => "Nạp tiền vào ví",
                TransactionType.Withdrawal => "Rút tiền từ ví", 
                TransactionType.Payment => "Thanh toán đơn hàng",
                TransactionType.PaymentReceived => "Nhận thanh toán từ đơn hàng",
                TransactionType.Refund => "Hoàn tiền",
                TransactionType.Transfer => "Chuyển tiền",
                _ => "Giao dịch"
            };
        }

        private string GetMessageFromResponseCode(string code)
        {
            return code switch
            {
                "00" => "Giao dịch thành công",
                "07" => "Giao dịch bị nghi ngờ (liên hệ ngân hàng)",
                "09" => "Thẻ/Tài khoản chưa đăng ký InternetBanking",
                "10" => "Xác thực thông tin thẻ/tài khoản thất bại",
                "51" => "Tài khoản không đủ số dư",
                "65" => "Tài khoản bị khóa",
                _ => "Giao dịch thất bại"
            };
        }

        

        public async Task CreateTransactionAsync(int walletId, TransactionType type, double amount, 
            int? orderId = null, Guid? settlementId = null, Guid? orderGroupId = null, string? description = null)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null) throw new Exception($"Wallet with ID {walletId} not found");

            var balanceBefore = wallet.Balance;
            wallet.Balance += amount;
            wallet.LastUpdatedAt = DateTime.UtcNow;

            var transaction = new WalletTransaction
            {
                WalletId = walletId,
                Amount = amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance,
                Type = type,
                Status = Entities.TransactionStatus.Success,
                Description = description ?? GetTransactionDescriptionWithDetails(type, orderId, settlementId, orderGroupId),
                CreatedAt = DateTime.UtcNow,
                OrderId = orderId,
                SettlementId = settlementId,
                OrderGroupId = orderGroupId
            };

            _walletRepository.Update(wallet);
            await _walletTransactionRepository.AddAsync(transaction);
            await _walletRepository.Commit();
        }

        // Method tạo description chi tiết hơn với thông tin đơn hàng
        private string GetTransactionDescriptionWithDetails(TransactionType type, int? orderId = null, Guid? settlementId = null, Guid? orderGroupId = null)
        {
            var baseDescription = GetTransactionDescription(type);
            
            // Ưu tiên orderGroupId trước nếu có (giao dịch nhóm đơn hàng)
            if (orderGroupId.HasValue)
            {
                return type switch
                {
                    TransactionType.Payment => $"{baseDescription} nhóm #GR{orderGroupId.ToString()[..8]}",
                    TransactionType.PaymentReceived => $"{baseDescription} nhóm #GR{orderGroupId.ToString()[..8]}",
                    TransactionType.Refund => $"{baseDescription} nhóm #GR{orderGroupId.ToString()[..8]}",
                    _ => $"{baseDescription} #GR{orderGroupId.ToString()[..8]}"
                };
            }
            
            // Thêm thông tin chi tiết nếu có orderId đơn lẻ
            if (orderId.HasValue)
            {
                return type switch
                {
                    TransactionType.Payment => $"{baseDescription} #ĐH{orderId}",
                    TransactionType.PaymentReceived => $"{baseDescription} #ĐH{orderId}",
                    TransactionType.Refund => $"{baseDescription} #ĐH{orderId}",
                    _ => baseDescription
                };
            }

            if (settlementId.HasValue)
            {
                return $"{baseDescription} #ST{settlementId.ToString()[..8]}";
            }

            return baseDescription;
        }
    }
}

        //public async Task DepositAsync(int userId, double amount, string externalTxnId)
        //{
        //    // 1. Tìm ví user
        //    var wallet = await _walletRepository.GetAll().FirstOrDefaultAsync(w => w.UserId == userId);
        //    if (wallet == null)
        //    {
        //        wallet = new Wallet
        //        {
        //            UserId = userId,
        //            Balance = 0,
        //            CreatedAt = DateTime.UtcNow
        //        };
        //        await _walletRepository.AddAsync(wallet);
        //    }

        //    // 2. Update balance
        //    wallet.Balance += amount;
        //    await _walletRepository.UpdateAsync(wallet);

        //    // 3. Ghi transaction TransactionScope nếu được 
        //    var transaction = new WalletTransaction
        //    {
        //        WalletId = wallet.WalletId,
        //        Amount = amount,
        //        Type =TransactionType.Deposit,
        //        Description = $"Nạp tiền qua VNPay - Ref: {externalTxnId}",
        //        CreatedAt = DateTime.UtcNow,
        //        BalanceAfter = wallet.Balance
        //    };
        //    await _walletTransactionRepository.AddAsync(transaction);

        //    // 4. Update ví hệ thống (admin)
        //    var adminWallet = await _walletRepository.GetAll().FirstOrDefaultAsync(w => w.UserId == 1);
        //    if (adminWallet == null)
        //    {
        //        adminWallet = new Wallet
        //        {
        //            WalletId = 1,
        //            UserId = 1, // Admin
        //            Balance = 0,
        //            CreatedAt = DateTime.UtcNow
        //        };
        //        await _walletRepository.AddAsync(adminWallet);
        //    }

        //    adminWallet.Balance += amount;
        //    await _walletRepository.UpdateAsync(adminWallet);

        //    var adminTransaction = new WalletTransaction
        //    {
        //        WalletId = adminWallet.WalletId,
        //        Amount = amount,
        //        Type = TransactionType.Deposit,
        //        Description = $"User {userId} nạp tiền qua VNPay - Ref: {externalTxnId}",
        //        CreatedAt = DateTime.UtcNow,
        //        BalanceAfter = adminWallet.Balance
        //    };
        //    await _walletTransactionRepository.AddAsync(adminTransaction);
        //}