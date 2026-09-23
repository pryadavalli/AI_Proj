namespace BankingService;

public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string AccountNumber { get; set; }
    public required string CustomerName { get; set; }
    public decimal Balance { get; set; }
    public List<AccountTransaction> Transactions { get; set; } = [];
}
