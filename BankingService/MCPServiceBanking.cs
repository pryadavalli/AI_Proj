using BankingService.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace BankingService;

[McpServerToolType]
public sealed class MCPServiceBanking
{
    private readonly BankingDbContext _database;

    public MCPServiceBanking(BankingDbContext database)
    {
        _database = database;
    }

    [McpServerTool]
    [Description("Lists all bank accounts with account IDs, account numbers, customer names, and current balances.")]
    public async Task<List<AccountResponse>> ListAccounts()
    {
        return await _database.Accounts
            .AsNoTracking()
            .OrderBy(account => account.AccountNumber)
            .Select(account => new AccountResponse(
                account.Id,
                account.AccountNumber,
                account.CustomerName,
                account.Balance))
            .ToListAsync();
    }

    [McpServerTool]
    [Description("Creates a bank account with an optional initial deposit.")]
    public async Task<AccountResponse> CreateAccount(
        [Description("Customer name")] string customerName,
        [Description("Initial deposit amount")] decimal initialDeposit = 0)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name is required.", nameof(customerName));
        if (initialDeposit < 0)
            throw new ArgumentOutOfRangeException(nameof(initialDeposit), "Initial deposit cannot be negative.");

        var account = new Account
        {
            AccountNumber = $"AC{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            CustomerName = customerName.Trim(),
            Balance = initialDeposit
        };

        account.Transactions.Add(new AccountTransaction
        {
            Type = TransactionType.Deposit,
            Amount = initialDeposit,
            Description = "Initial deposit"
        });

        _database.Accounts.Add(account);
        await _database.SaveChangesAsync();
        return new AccountResponse(account.Id, account.AccountNumber, account.CustomerName, account.Balance);
    }

    [McpServerTool]
    [Description("Gets the current account balance for a bank account.")]
    public async Task<AccountBalance> GetAccountBalance(
        [Description("The bank account ID")] string accountId)
    {
        var account = await FindAccountAsync(accountId) ?? throw new KeyNotFoundException("Account was not found.");
        return new AccountBalance { AccountId = account.Id.ToString(), Balance = account.Balance };
    }

    [McpServerTool]
    [Description("Deposits money into an existing bank account.")]
    public async Task<BalanceResponse> Deposit(
        [Description("Account ID or account number")] string accountId,
        [Description("Deposit amount")] decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be greater than zero.");

        var account = await FindAccountAsync(accountId) ?? throw new KeyNotFoundException("Account was not found.");
        account.Balance += amount;
        _database.Transactions.Add(new AccountTransaction
        {
            AccountId = account.Id,
            Type = TransactionType.Deposit,
            Amount = amount,
            Description = "Cash deposit"
        });

        await _database.SaveChangesAsync();
        return new BalanceResponse(account.Id, account.AccountNumber, account.CustomerName, account.Balance);
    }

    [McpServerTool]
    [Description("Withdraws money from a bank account.")]
    public async Task<BalanceResponse> Withdraw(
        [Description("Account ID or account number")] string accountId,
        [Description("Withdrawal amount")] decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Withdrawal amount must be greater than zero.");

        var account = await FindAccountAsync(accountId) ?? throw new KeyNotFoundException("Account was not found.");
        if (account.Balance < amount)
            throw new InvalidOperationException("Insufficient funds.");

        account.Balance -= amount;
        _database.Transactions.Add(new AccountTransaction
        {
            AccountId = account.Id,
            Type = TransactionType.Withdrawal,
            Amount = amount,
            Description = "Cash withdrawal"
        });

        await _database.SaveChangesAsync();
        return new BalanceResponse(account.Id, account.AccountNumber, account.CustomerName, account.Balance);
    }

    [McpServerTool]
    [Description("Gets recent transactions for a bank account.")]
    public async Task<List<BankingTransactionDto>> GetTransactions(
        [Description("The bank account ID")] string accountId,
        [Description("Number of transactions to retrieve")] int count = 10)
    {
        var account = await FindAccountAsync(accountId) ?? throw new KeyNotFoundException("Account was not found.");
        return await _database.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId == account.Id)
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .Take(Math.Max(count, 1))
            .Select(transaction => new BankingTransactionDto
            {
                TransactionId = transaction.Id.ToString(),
                Date = transaction.CreatedAtUtc,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Type = transaction.Type.ToString()
            })
            .ToListAsync();
    }

    [McpServerTool]
    [Description("Gets customer profile information.")]
    public async Task<CustomerProfile> GetCustomerProfile(
        [Description("The customer ID")] string customerId)
    {
        var account = await FindAccountAsync(customerId) ?? throw new KeyNotFoundException("Account was not found.");
        return new CustomerProfile { CustomerId = account.Id.ToString(), Name = account.CustomerName };
    }

    [McpServerTool]
    [Description("Gets the current status of a bank account.")]
    public async Task<AccountStatus> GetAccountStatus(
        [Description("The bank account ID")] string accountId)
    {
        var account = await FindAccountAsync(accountId) ?? throw new KeyNotFoundException("Account was not found.");
        return new AccountStatus { AccountId = account.Id.ToString(), Status = "Active", IsActive = true };
    }

    private async Task<Account?> FindAccountAsync(string accountId)
    {
        if (Guid.TryParse(accountId, out var id))
            return await _database.Accounts.SingleOrDefaultAsync(account => account.Id == id);

        return await _database.Accounts.SingleOrDefaultAsync(account => account.AccountNumber == accountId);
    }
}
