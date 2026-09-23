namespace BankingService;

public sealed record BalanceResponse(Guid Id, string AccountNumber, string CustomerName, decimal Balance);
