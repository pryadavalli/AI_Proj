using Microsoft.EntityFrameworkCore;
using BankingService.Data;

namespace BankingService;

internal static class BankingApiService
{
    public static void MapBankingEndpoints(this WebApplication app)
    {
        app.MapGet("/api/accounts", async (
            int? limit,
            string? customerName,
            BankingDbContext database) =>
        {
            var take = Math.Clamp(limit ?? 100, 1, 500);
            var accounts = database.Accounts.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                accounts = accounts.Where(account => account.CustomerName.Contains(customerName.Trim()));
            }

            var results = await accounts
                .OrderBy(account => account.AccountNumber)
                .Take(take)
                .Select(account => new
                {
                    account.Id,
                    account.AccountNumber,
                    account.CustomerName,
                    account.Balance
                })
                .ToListAsync();

            return Results.Ok(new
            {
                count = results.Count,
                limit = take,
                accounts = results
            });
        })
        .WithName("ListAccounts")
        .WithOpenApi();

        app.MapGet("/api/accounts/{id:guid}", async (Guid id, BankingDbContext database) =>
        {
            var account = await database.Accounts
                .AsNoTracking()
                .Where(item => item.Id == id)
                .Select(item => new
                {
                    item.Id,
                    item.AccountNumber,
                    item.CustomerName,
                    item.Balance,
                    transactions = item.Transactions
                        .OrderByDescending(transaction => transaction.CreatedAtUtc)
                        .Select(transaction => new
                        {
                            transaction.Id,
                            transaction.Type,
                            transaction.Amount,
                            transaction.Description,
                            transaction.CreatedAtUtc
                        })
                        .ToList()
                })
                .SingleOrDefaultAsync();

            return account is null
                ? Results.NotFound(new { message = "Account was not found." })
                : Results.Ok(account);
        })
        .WithName("GetAccountDetails")
        .WithOpenApi();

        app.MapPost("/api/accounts", async (CreateAccountRequest request, BankingDbContext database) =>
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.CustomerName)] = ["Customer name is required."]
                });
            }

            if (request.InitialDeposit < 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.InitialDeposit)] = ["Initial deposit cannot be negative."]
                });
            }

            var account = new Account
            {
                AccountNumber = CreateAccountNumber(),
                CustomerName = request.CustomerName.Trim(),
                Balance = request.InitialDeposit
            };

            account.Transactions.Add(new AccountTransaction
            {
                Type = TransactionType.Deposit,
                Amount = request.InitialDeposit,
                Description = "Initial deposit"
            });

            database.Accounts.Add(account);
            await database.SaveChangesAsync();

            return Results.Created($"/api/accounts/{account.Id}/balance", new AccountResponse(
                account.Id, account.AccountNumber, account.CustomerName, account.Balance));
        })
        .WithName("CreateAccount")
        .WithOpenApi();

        app.MapGet("/api/accounts/{id:guid}/balance", async (Guid id, BankingDbContext database) =>
        {
            var account = await database.Accounts.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
            return account is null
                ? Results.NotFound(new { message = "Account was not found." })
                : Results.Ok(new BalanceResponse(account.Id, account.AccountNumber, account.CustomerName, account.Balance));
        })
        .WithName("CheckBalance")
        .WithOpenApi();

        app.MapGet("/api/accounts/{id:guid}/transactions", async (
            Guid id,
            int? limit,
            BankingDbContext database) =>
        {
            var take = Math.Clamp(limit ?? 50, 1, 500);
            var accountExists = await database.Accounts.AnyAsync(account => account.Id == id);
            if (!accountExists)
            {
                return Results.NotFound(new { message = "Account was not found." });
            }

            var transactions = await database.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.AccountId == id)
                .OrderByDescending(transaction => transaction.CreatedAtUtc)
                .Take(take)
                .Select(transaction => new
                {
                    transaction.Id,
                    transaction.Type,
                    transaction.Amount,
                    transaction.Description,
                    transaction.CreatedAtUtc
                })
                .ToListAsync();

            return Results.Ok(new { count = transactions.Count, limit = take, transactions });
        })
        .WithName("ListAccountTransactions")
        .WithOpenApi();

        app.MapPost("/api/accounts/{id:guid}/withdraw", async (Guid id, WithdrawRequest request, BankingDbContext database) =>
        {
            if (request.Amount <= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Amount)] = ["Withdrawal amount must be greater than zero."]
                });
            }

            var account = await database.Accounts.SingleOrDefaultAsync(item => item.Id == id);
            if (account is null)
            {
                return Results.NotFound(new { message = "Account was not found." });
            }

            if (account.Balance < request.Amount)
            {
                return Results.BadRequest(new { message = "Insufficient funds." });
            }

            account.Balance -= request.Amount;
            database.Transactions.Add(new AccountTransaction
            {
                AccountId = account.Id,
                Type = TransactionType.Withdrawal,
                Amount = request.Amount,
                Description = "Cash withdrawal"
            });

            await database.SaveChangesAsync();

            return Results.Ok(new BalanceResponse(account.Id, account.AccountNumber, account.CustomerName, account.Balance));
        })
        .WithName("Withdraw")
        .WithOpenApi();
    }

    private static string CreateAccountNumber() => $"AC{Guid.NewGuid():N}"[..12].ToUpperInvariant();
}
