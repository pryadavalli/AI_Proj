namespace BankingService;

public sealed class AccountBalance
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
}
