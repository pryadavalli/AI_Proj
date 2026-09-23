namespace BankingService;

public sealed record CreateAccountRequest(string CustomerName, decimal InitialDeposit);
