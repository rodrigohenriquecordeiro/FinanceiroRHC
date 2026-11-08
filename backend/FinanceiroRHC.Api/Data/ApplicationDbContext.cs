using FinanceiroRHC.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroRHC.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<FinancialCategory> Categories => Set<FinancialCategory>();
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseRecurrenceRule> ExpenseRecurrenceRules => Set<ExpenseRecurrenceRule>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<CreditCardPurchase> CreditCardPurchases => Set<CreditCardPurchase>();
    public DbSet<CardPurchaseRecurrenceRule> CardPurchaseRecurrenceRules => Set<CardPurchaseRecurrenceRule>();
    public DbSet<BenefitEntry> BenefitEntries => Set<BenefitEntry>();
    public DbSet<MonthlyBalanceOverride> MonthlyBalanceOverrides => Set<MonthlyBalanceOverride>();
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<InvestmentWithdrawal> InvestmentWithdrawals => Set<InvestmentWithdrawal>();
    public DbSet<CreditCardInvoicePayment> CreditCardInvoicePayments => Set<CreditCardInvoicePayment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<FinancialCategory>()
            .HasIndex(category => new { category.UserId, category.Name, category.Type })
            .IsUnique();
        builder.Entity<MonthlyBalanceOverride>()
            .HasIndex(balance => new { balance.UserId, balance.Year, balance.Month })
            .IsUnique();
        builder.Entity<CreditCardInvoicePayment>()
            .HasIndex(payment => new { payment.UserId, payment.Year, payment.Month })
            .IsUnique();
        builder.Entity<CreditCardInvoicePayment>()
            .HasIndex(payment => payment.ExpenseId)
            .IsUnique();
        builder.Entity<CreditCardInvoicePayment>()
            .HasOne(payment => payment.Expense)
            .WithMany()
            .HasForeignKey(payment => payment.ExpenseId)
            .HasConstraintName("FK_InvoicePayments_Expense")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Investment>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(investment => investment.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<InvestmentWithdrawal>()
            .HasOne(withdrawal => withdrawal.Investment)
            .WithMany(investment => investment.Withdrawals)
            .HasForeignKey(withdrawal => withdrawal.InvestmentId)
            .HasConstraintName("FK_InvestmentWithdrawals_Investment")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<InvestmentWithdrawal>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(withdrawal => withdrawal.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Income>()
            .HasOne(income => income.InvestmentWithdrawal)
            .WithMany()
            .HasForeignKey(income => income.InvestmentWithdrawalId)
            .HasConstraintName("FK_Incomes_InvestmentWithdrawal")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Income>()
            .HasIndex(income => income.InvestmentWithdrawalId)
            .IsUnique();
        builder.Entity<Expense>()
            .HasOne(expense => expense.Investment)
            .WithMany()
            .HasForeignKey(expense => expense.InvestmentId)
            .HasConstraintName("FK_Expenses_Investment")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Expense>()
            .HasOne(expense => expense.InvestmentWithdrawal)
            .WithMany()
            .HasForeignKey(expense => expense.InvestmentWithdrawalId)
            .HasConstraintName("FK_Expenses_InvestmentWithdrawal")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Expense>()
            .HasIndex(expense => expense.InvestmentWithdrawalId)
            .IsUnique();
        builder.Entity<Expense>()
            .HasOne(expense => expense.Category)
            .WithMany(category => category.Expenses)
            .HasForeignKey(expense => expense.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Expense>()
            .HasOne(expense => expense.RecurrenceRule)
            .WithMany(rule => rule.Expenses)
            .HasForeignKey(expense => expense.RecurrenceRuleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Expense>()
            .HasQueryFilter(expense => !expense.IsDeleted);
        builder.Entity<Expense>()
            .HasIndex(expense => new { expense.RecurrenceRuleId, expense.InstallmentNumber })
            .IsUnique();
        builder.Entity<ExpenseRecurrenceRule>()
            .HasOne(rule => rule.Category)
            .WithMany()
            .HasForeignKey(rule => rule.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ExpenseRecurrenceRule>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(rule => rule.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ExpenseRecurrenceRule>()
            .HasIndex(rule => new { rule.UserId, rule.IsActive });
        builder.Entity<CreditCard>()
            .HasOne<ApplicationUser>()
            .WithMany(user => user.CreditCards)
            .HasForeignKey(card => card.UserId);
        builder.Entity<CreditCardPurchase>()
            .HasOne<FinancialCategory>()
            .WithMany()
            .HasForeignKey(purchase => purchase.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CreditCardPurchase>()
            .HasOne(purchase => purchase.RecurrenceRule)
            .WithMany(rule => rule.Purchases)
            .HasForeignKey(purchase => purchase.RecurrenceRuleId)
            .HasConstraintName("FK_CardPurchases_RecurrenceRule")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<CreditCardPurchase>()
            .HasQueryFilter(purchase => !purchase.IsDeleted);
        builder.Entity<CreditCardPurchase>()
            .HasIndex(purchase => new { purchase.RecurrenceRuleId, purchase.InstallmentNumber })
            .IsUnique();
        builder.Entity<CardPurchaseRecurrenceRule>()
            .HasOne<CreditCard>()
            .WithMany()
            .HasForeignKey(rule => rule.CreditCardId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CardPurchaseRecurrenceRule>()
            .HasOne<FinancialCategory>()
            .WithMany()
            .HasForeignKey(rule => rule.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CardPurchaseRecurrenceRule>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(rule => rule.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<CardPurchaseRecurrenceRule>()
            .HasIndex(rule => new { rule.UserId, rule.IsActive });
        builder.Entity<Income>().Property(income => income.Amount).HasPrecision(12, 2);
        builder.Entity<Expense>().Property(expense => expense.Amount).HasPrecision(12, 2);
        builder.Entity<ExpenseRecurrenceRule>().Property(rule => rule.Amount).HasPrecision(12, 2);
        builder.Entity<CreditCardPurchase>().Property(purchase => purchase.Amount).HasPrecision(12, 2);
        builder.Entity<CardPurchaseRecurrenceRule>().Property(rule => rule.Amount).HasPrecision(12, 2);
        builder.Entity<BenefitEntry>().Property(entry => entry.Amount).HasPrecision(12, 2);
        builder.Entity<MonthlyBalanceOverride>().Property(balance => balance.Amount).HasPrecision(12, 2);
        builder.Entity<Investment>().Property(investment => investment.Amount).HasPrecision(12, 2);
        builder.Entity<InvestmentWithdrawal>().Property(withdrawal => withdrawal.Amount).HasPrecision(12, 2);
        builder.Entity<InvestmentWithdrawal>().Property(withdrawal => withdrawal.ReturnAmount).HasPrecision(12, 2);
        builder.Entity<CreditCardInvoicePayment>().Property(payment => payment.Amount).HasPrecision(12, 2);
    }
}
