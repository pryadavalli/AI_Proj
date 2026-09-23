namespace BankingService;

public sealed record AccountResponse(Guid Id, string AccountNumber, string CustomerName, decimal Balance);
