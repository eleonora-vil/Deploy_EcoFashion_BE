namespace EcoFashionBackEnd.Dtos.Wallet
{
    public class GetWithdrawalRequestDto
    {
        public int TransactionId { get; set; }
        public string Name { get; set; }
        public double Amount { get; set; }
        public double BalanceBefore { get; set; }
        public double BalanceAfter { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
