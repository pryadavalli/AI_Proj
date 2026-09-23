using Microsoft.EntityFrameworkCore;

namespace BankingService.Data;

using BankingService;

public sealed class BankingDbContext(DbContextOptions<BankingDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountTransaction> Transactions => Set<AccountTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>().Property(account => account.Balance).HasPrecision(18, 2);
        modelBuilder.Entity<Account>().HasIndex(account => account.AccountNumber).IsUnique();
        modelBuilder.Entity<AccountTransaction>().Property(transaction => transaction.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Account>()
            .HasMany(account => account.Transactions)
            .WithOne(transaction => transaction.Account)
            .HasForeignKey(transaction => transaction.AccountId);
    }
}
