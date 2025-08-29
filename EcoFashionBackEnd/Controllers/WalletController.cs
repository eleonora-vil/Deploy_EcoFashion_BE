namespace EcoFashionBackEnd.Controllers
{
    using EcoFashionBackEnd.Common;
    using EcoFashionBackEnd.Common.Payloads.Requests.Wallet;
    using EcoFashionBackEnd.Dtos.Wallet;
    using EcoFashionBackEnd.Entities;
    using EcoFashionBackEnd.Extensions.NewFolder;
    using EcoFashionBackEnd.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using System.Transactions;

    [ApiController]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly WalletService _walletService;
        private readonly IConfiguration _configuration;

        public WalletController(WalletService walletService, IConfiguration configuration)
        {
            _walletService = walletService;
            _configuration = configuration;
        }

        // Get user's wallet information
        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized("Không thể xác định người dùng.");

            var wallet = await _walletService.GetWalletByUserIdAsync(userId);
            if (wallet == null)
                return NotFound("Không tìm thấy ví của người dùng.");

            return Ok(wallet);
        }

        //  Lấy số dư ví
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized("Không thể xác định người dùng.");

            var balance = await _walletService.GetBalanceAsync(userId);
            return Ok(new { balance = balance });
        }

        // Get wallet summary with recent transactions and stats
        [HttpGet("summary")]
        public async Task<IActionResult> GetWalletSummary()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized("Không thể xác định người dùng.");

            var summary = await _walletService.GetWalletSummaryAsync(userId);
            return Ok(summary);
        }

        
        //  Lấy lịch sử giao dịch ví
       
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized("Không thể xác định người dùng.");

            var transactions = await _walletService.GetTransactionsPaginatedAsync(userId, page, pageSize);
            return Ok(transactions);
        }

        // Get specific transaction by ID
        [HttpGet("transaction/{transactionId}")]
        public async Task<IActionResult> GetTransaction(int transactionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized("Không thể xác định người dùng.");

            var transaction = await _walletService.GetTransactionByIdAsync(transactionId, userId);
            if (transaction == null)
                return NotFound("Không tìm thấy giao dịch.");

            return Ok(transaction);
        }


        // taọ link deposit 
        [HttpPost("deposit")]
        public async Task<IActionResult> CreateDepositLink([FromBody] DepositRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest("Số tiền nạp phải > 0");

            // Xác định userId từ token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized("Không thể xác định người dùng.");

            var result = await _walletService.CreateDepositAsync(userId, request.Amount, HttpContext);

            return Ok(result);
        }

        // Callback từ VNPay sau khi nạp tiền - redirect về frontend thay vì trả JSON
        [HttpGet("deposit/callback")]
        public async Task<IActionResult> DepositCallback()
        {
            var query = HttpContext.Request.Query;

            var result = await _walletService.HandleVNPayDepositReturnAsync(query);

            // Tạo URL redirect về frontend với thông tin kết quả
            string frontendUrl = _configuration["Frontend:WalletUrl"] ?? "http://localhost:5173";
            string redirectUrl;

            if (result == null)
            {
                // Nạp tiền thất bại - redirect về error page
                redirectUrl = $"{frontendUrl}/wallet?error=deposit_failed";
            }
            else if (result.VnPayResponseCode == "00")
            {
                // Lấy orderId để xác định giao dịch, nhưng sử dụng amount từ VNPay response
                var orderId = int.Parse(result.OrderId);
                var amount = result.Amount;
                
                // Nạp tiền thành công - redirect về success page với thông tin
                redirectUrl = $"{frontendUrl}/wallet?success=deposit&transactionId={result.TransactionId}&amount={amount}";
            }
            else
            {
                // Nạp tiền thất bại với mã lỗi VNPay - redirect về error page  
                redirectUrl = $"{frontendUrl}/wallet?error=deposit_failed&code={result.VnPayResponseCode}";
            }

            // Redirect về frontend thay vì trả JSON
            return Redirect(redirectUrl);
        }
        // taọ RequestWithdrawal
        [HttpPost("withdrawal/request")]
        public async Task<IActionResult> RequestWithdrawal([FromBody] WithdrawalRequestDto request)
        {
            if (request.Amount <= 0)
                return BadRequest("Số tiền rút phải > 0");

            // Lấy userId từ token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized("Không thể xác định người dùng.");

            try
            {
                var transaction = await _walletService.RequestWithdrawalAsync(userId, request.Amount, request.Description);
                return Ok(transaction); // hoặc map sang DTO trước
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("withdrawal")]
        public async Task<IActionResult> CreateWithdrawalLink([FromBody] ApproveWithdrawalDto request)
        {
            if (request.WalletTransactionId <= 0)
                return BadRequest("Thiếu WalletTransactionId hợp lệ.");

            // 👤 Xác định userId từ token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized("Không thể xác định người dùng.");

            var result = await _walletService.CreateWithdrawalPaymentAsync(request.WalletTransactionId, HttpContext);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // Callback từ VNPay sau khi rút tiền - redirect về frontend thay vì trả JSON  
        [HttpGet("withdrawal/callback")]
        public async Task<IActionResult> WithdrawalCallback()
        {
            // 1️⃣ VNPay gọi GET với query vnp_*
            var query = HttpContext.Request.Query;

            // 2️⃣ Gọi service để handle transaction
            var result = await _walletService.HandleVNPayWithdrawalReturnAsync(query);

            // 3️⃣ Tạo URL redirect về frontend với thông tin kết quả
            string frontendUrl = _configuration["Frontend:WalletUrl"] ?? "http://localhost:5173";
            string redirectUrl;

            if (result == null)
            {
                // Rút tiền thất bại - redirect về error page
                redirectUrl = $"{frontendUrl}/wallet?error=withdrawal_failed";
            }
            else if (result.VnPayResponseCode == "00")
            {
                // Lấy orderId để xác định giao dịch, nhưng sử dụng amount từ VNPay response
                var orderId = int.Parse(result.OrderId);
                var amount = result.Amount;
                
                // Rút tiền thành công - redirect về success page với thông tin
                redirectUrl = $"{frontendUrl}/wallet?success=withdrawal&transactionId={result.TransactionId}&amount={amount}";
            }
            else
            {
                // Rút tiền thất bại với mã lỗi VNPay - redirect về error page
                redirectUrl = $"{frontendUrl}/wallet?error=withdrawal_failed&code={result.VnPayResponseCode}";
            }

            // Redirect về frontend thay vì trả JSON
            return Redirect(redirectUrl);
        }


    }




}
