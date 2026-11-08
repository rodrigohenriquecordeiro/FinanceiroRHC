using FinanceiroRHC.Api.Data;
using FinanceiroRHC.Api.Models;
using FinanceiroRHC.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroRHC.Api.Services;

public class FinanceService(ApplicationDbContext context, IRepositoryProvider repositories) : IFinanceService
{
    public Task<List<T>> GetAsync<T>(string userId, CancellationToken cancellationToken)
        where T : class, IUserOwnedEntity =>
        repositories.For<T>().GetByUserAsync(userId, cancellationToken);

    public Task<T?> GetByIdAsync<T>(int id, string userId, CancellationToken cancellationToken)
        where T : class, IUserOwnedEntity =>
        repositories.For<T>().GetByIdAsync(id, userId, cancellationToken);

    public async Task<T> SaveAsync<T>(T entity, int? id, string userId, CancellationToken cancellationToken)
        where T : class, IUserOwnedEntity
    {
        entity.UserId = userId;
        var repository = repositories.For<T>();
        if (id is null)
        {
            await repository.AddAsync(entity, cancellationToken);
        }
        else
        {
            var existing = await repository.GetByIdAsync(id.Value, userId, cancellationToken)
                ?? throw new KeyNotFoundException("Registro não encontrado.");
            context.Entry(existing).CurrentValues.SetValues(entity);
            entity = existing;
        }
        await repository.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> DeleteAsync<T>(int id, string userId, CancellationToken cancellationToken)
        where T : class, IUserOwnedEntity
    {
        var repository = repositories.For<T>();
        var entity = await repository.GetByIdAsync(id, userId, cancellationToken);
        if (entity is null)
        {
            return false;
        }
        if (entity is Expense { InvestmentId: not null } or Expense { InvestmentWithdrawalId: not null })
        {
            throw new ArgumentException("Este ajuste é vinculado a um investimento. Exclua ou edite o investimento correspondente.");
        }
        if (entity is Income { InvestmentWithdrawalId: not null } investmentIncome)
        {
            var withdrawal = await context.InvestmentWithdrawals.FirstOrDefaultAsync(
                item => item.Id == investmentIncome.InvestmentWithdrawalId && item.UserId == userId,
                cancellationToken);
            if (withdrawal is not null)
            {
                context.InvestmentWithdrawals.Remove(withdrawal);
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }
        }
        if (entity is Expense { RecurrenceRuleId: not null } recurringExpense)
        {
            recurringExpense.IsDeleted = true;
        }
        else if (entity is CreditCardPurchase { RecurrenceRuleId: not null } recurringPurchase)
        {
            recurringPurchase.IsDeleted = true;
        }
        else
        {
            repository.Remove(entity);
        }
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<ExpenseResponse>> GetExpensesForMonthAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        ValidateYearMonth(year, month);
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);
        await EnsureRecurringExpensesForRangeAsync(userId, start, end, cancellationToken);

        return await context.Expenses
            .Where(item => item.UserId == userId && item.ExpenseDate >= start && item.ExpenseDate < end)
            .OrderBy(item => item.ExpenseDate)
            .Select(item => new ExpenseResponse(
                item.Id,
                item.Description,
                item.CategoryId,
                item.ExpenseDate,
                item.Amount,
                item.IsPaid,
                item.RecurrenceRuleId == null ? ExpenseRecurrenceType.None : item.RecurrenceRule!.RecurrenceType,
                item.InstallmentNumber,
                item.TotalInstallments,
                item.InvestmentId != null || item.InvestmentWithdrawalId != null,
                context.CreditCardInvoicePayments.Any(payment => payment.ExpenseId == item.Id)))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExpenseResponse> SaveExpenseAsync(
        ExpenseRequest request,
        int? id,
        string userId,
        CancellationToken cancellationToken)
    {
        if (id is not null)
        {
            var existing = await context.Expenses.FirstOrDefaultAsync(
                expense => expense.Id == id && expense.UserId == userId,
                cancellationToken)
                ?? throw new KeyNotFoundException("Despesa não encontrada.");
            if (existing.InvestmentId is not null || existing.InvestmentWithdrawalId is not null)
            {
                throw new ArgumentException("Este ajuste é vinculado a um investimento. Edite o investimento correspondente.");
            }
            if (await context.CreditCardInvoicePayments.AnyAsync(
                    payment => payment.ExpenseId == existing.Id,
                    cancellationToken))
            {
                throw new ArgumentException("Esta despesa registra o pagamento de uma fatura. Use a área do Cartão de Crédito para gerenciá-la.");
            }
            existing.Description = request.Description.Trim();
            existing.CategoryId = request.CategoryId;
            existing.ExpenseDate = request.ExpenseDate;
            existing.Amount = request.Amount;
            existing.IsPaid = request.IsPaid;
            await context.SaveChangesAsync(cancellationToken);
            return ToExpenseResponse(existing);
        }

        if (request.RecurrenceType == ExpenseRecurrenceType.None)
        {
            var expense = new Expense
            {
                UserId = userId,
                Description = request.Description.Trim(),
                CategoryId = request.CategoryId,
                ExpenseDate = request.ExpenseDate,
                Amount = request.Amount,
                IsPaid = request.IsPaid
            };
            context.Expenses.Add(expense);
            await context.SaveChangesAsync(cancellationToken);
            return ToExpenseResponse(expense);
        }

        if (request.RecurrenceType == ExpenseRecurrenceType.Installments
            && (request.StartInstallment < 1
                || request.TotalInstallments is null
                || request.TotalInstallments < request.StartInstallment))
        {
            throw new ArgumentException("Informe uma parcela inicial e um total válido.");
        }
        if (request.RecurrenceType == ExpenseRecurrenceType.Unlimited && request.StartInstallment < 1)
        {
            throw new ArgumentException("A parcela inicial deve ser maior que zero.");
        }
        if (request.RecurrenceType is not (ExpenseRecurrenceType.Installments or ExpenseRecurrenceType.Unlimited))
        {
            throw new ArgumentException("Selecione um tipo de recorrência válido.");
        }

        var rule = new ExpenseRecurrenceRule
        {
            UserId = userId,
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId,
            StartDate = request.ExpenseDate,
            Amount = request.Amount,
            RecurrenceType = request.RecurrenceType,
            StartInstallment = request.StartInstallment,
            TotalInstallments = request.RecurrenceType == ExpenseRecurrenceType.Installments
                ? request.TotalInstallments
                : null,
        };
        var firstExpense = new Expense
        {
            UserId = userId,
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId,
            ExpenseDate = request.ExpenseDate,
            Amount = request.Amount,
            IsPaid = request.IsPaid,
            RecurrenceRule = rule,
            InstallmentNumber = request.StartInstallment,
            TotalInstallments = rule.TotalInstallments
        };
        context.ExpenseRecurrenceRules.Add(rule);
        context.Expenses.Add(firstExpense);
        await context.SaveChangesAsync(cancellationToken);
        return ToExpenseResponse(firstExpense, request.RecurrenceType);
    }

    public async Task<List<CardPurchaseResponse>> GetCardPurchasesForMonthAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        ValidateYearMonth(year, month);
        var start = new DateOnly(year, month, 1);
        await EnsureRecurringCardPurchasesForRangeAsync(userId, start, start.AddMonths(1), cancellationToken);

        return await context.CreditCardPurchases
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.PurchaseDate)
            .Select(item => new CardPurchaseResponse(
                item.Id,
                item.CreditCardId,
                item.CategoryId,
                item.Description,
                item.PurchaseDate,
                item.Amount,
                item.RecurrenceRuleId == null ? ExpenseRecurrenceType.None : item.RecurrenceRule!.RecurrenceType,
                item.InstallmentNumber,
                item.TotalInstallments))
            .ToListAsync(cancellationToken);
    }

    public async Task<CardPurchaseResponse> SaveCardPurchaseAsync(
        CreditCardPurchaseRequest request,
        int? id,
        string userId,
        CancellationToken cancellationToken)
    {
        if (id is not null)
        {
            var existing = await context.CreditCardPurchases.FirstOrDefaultAsync(
                purchase => purchase.Id == id && purchase.UserId == userId,
                cancellationToken)
                ?? throw new KeyNotFoundException("Compra não encontrada.");
            existing.CreditCardId = request.CreditCardId;
            existing.CategoryId = request.CategoryId;
            existing.Description = request.Description.Trim();
            existing.PurchaseDate = request.PurchaseDate;
            existing.Amount = request.Amount;
            await context.SaveChangesAsync(cancellationToken);
            return ToCardPurchaseResponse(existing, existing.RecurrenceRuleId is null
                ? ExpenseRecurrenceType.None
                : (await context.CardPurchaseRecurrenceRules
                    .Where(rule => rule.Id == existing.RecurrenceRuleId)
                    .Select(rule => rule.RecurrenceType)
                    .FirstAsync(cancellationToken)));
        }

        if (request.RecurrenceType == ExpenseRecurrenceType.None)
        {
            var purchase = new CreditCardPurchase
            {
                UserId = userId,
                CreditCardId = request.CreditCardId,
                CategoryId = request.CategoryId,
                Description = request.Description.Trim(),
                PurchaseDate = request.PurchaseDate,
                Amount = request.Amount
            };
            context.CreditCardPurchases.Add(purchase);
            await context.SaveChangesAsync(cancellationToken);
            return ToCardPurchaseResponse(purchase, ExpenseRecurrenceType.None);
        }

        ValidateRecurrence(request.RecurrenceType, request.StartInstallment, request.TotalInstallments);
        var rule = new CardPurchaseRecurrenceRule
        {
            UserId = userId,
            CreditCardId = request.CreditCardId,
            CategoryId = request.CategoryId,
            Description = request.Description.Trim(),
            StartDate = request.PurchaseDate,
            Amount = request.Amount,
            RecurrenceType = request.RecurrenceType,
            StartInstallment = request.StartInstallment,
            TotalInstallments = request.RecurrenceType == ExpenseRecurrenceType.Installments
                ? request.TotalInstallments
                : null
        };
        var firstPurchase = new CreditCardPurchase
        {
            UserId = userId,
            CreditCardId = request.CreditCardId,
            CategoryId = request.CategoryId,
            Description = request.Description.Trim(),
            PurchaseDate = request.PurchaseDate,
            Amount = request.Amount,
            RecurrenceRule = rule,
            InstallmentNumber = request.StartInstallment,
            TotalInstallments = rule.TotalInstallments
        };
        context.CardPurchaseRecurrenceRules.Add(rule);
        context.CreditCardPurchases.Add(firstPurchase);
        await context.SaveChangesAsync(cancellationToken);
        return ToCardPurchaseResponse(firstPurchase, request.RecurrenceType);
    }

    public async Task<bool> CancelRecurringCardPurchasesFromAsync(
        int purchaseId,
        string userId,
        CancellationToken cancellationToken)
    {
        var purchase = await context.CreditCardPurchases.FirstOrDefaultAsync(
            item => item.Id == purchaseId && item.UserId == userId,
            cancellationToken);
        if (purchase?.RecurrenceRuleId is null)
        {
            return false;
        }

        var rule = await context.CardPurchaseRecurrenceRules.FirstOrDefaultAsync(
            item => item.Id == purchase.RecurrenceRuleId && item.UserId == userId,
            cancellationToken);
        if (rule is null)
        {
            return false;
        }
        rule.IsActive = false;
        rule.EndInstallment = purchase.InstallmentNumber - 1;
        var remainingPurchases = await context.CreditCardPurchases
            .IgnoreQueryFilters()
            .Where(item => item.RecurrenceRuleId == rule.Id
                && item.InstallmentNumber >= purchase.InstallmentNumber)
            .ToListAsync(cancellationToken);
        foreach (var remainingPurchase in remainingPurchases)
        {
            remainingPurchase.IsDeleted = true;
        }
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CreditCardInvoicePaymentResponse> GetCardInvoicePaymentAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        ValidateYearMonth(year, month);
        var payment = await context.CreditCardInvoicePayments.FirstOrDefaultAsync(
            item => item.UserId == userId && item.Year == year && item.Month == month,
            cancellationToken);
        return payment is null
            ? new CreditCardInvoicePaymentResponse(false, 0, null)
            : new CreditCardInvoicePaymentResponse(true, payment.Amount, payment.PaidAt);
    }

    public async Task<CreditCardInvoicePaymentResponse> PayCardInvoiceAsync(
        string userId,
        CreditCardInvoicePaymentRequest request,
        CancellationToken cancellationToken)
    {
        ValidateYearMonth(request.Year, request.Month);
        if (await context.CreditCardInvoicePayments.AnyAsync(
                item => item.UserId == userId && item.Year == request.Year && item.Month == request.Month,
                cancellationToken))
        {
            throw new ArgumentException("A fatura deste mês já foi paga.");
        }

        var invoiceStart = new DateOnly(request.Year, request.Month, 1);
        var invoiceEnd = invoiceStart.AddMonths(1);
        await EnsureRecurringCardPurchasesForRangeAsync(userId, invoiceStart, invoiceEnd, cancellationToken);
        var amount = await context.CreditCardPurchases
            .Where(item => item.UserId == userId && item.PurchaseDate >= invoiceStart && item.PurchaseDate < invoiceEnd)
            .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
        if (amount <= 0)
        {
            throw new ArgumentException("Não há compras nesta fatura para pagar.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var category = await context.Categories.FirstOrDefaultAsync(
            item => item.UserId == userId && item.Type == "expense" && item.Name == "Cartão de crédito",
            cancellationToken);
        if (category is null)
        {
            category = new FinancialCategory
            {
                UserId = userId,
                Name = "Cartão de crédito",
                Type = "expense"
            };
            context.Categories.Add(category);
        }

        var expense = new Expense
        {
            UserId = userId,
            Description = $"Pagamento da fatura do cartão - {request.Month:D2}/{request.Year}",
            Category = category,
            ExpenseDate = request.PaidAt,
            Amount = amount,
            IsPaid = true
        };
        context.Expenses.Add(expense);
        await context.SaveChangesAsync(cancellationToken);
        var payment = new CreditCardInvoicePayment
        {
            UserId = userId,
            Year = request.Year,
            Month = request.Month,
            Amount = amount,
            PaidAt = request.PaidAt,
            ExpenseId = expense.Id
        };
        context.CreditCardInvoicePayments.Add(payment);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CreditCardInvoicePaymentResponse(true, amount, request.PaidAt);
    }

    public async Task<List<InvestmentResponse>> GetInvestmentsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var investments = await context.Investments
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.ApplicationDate)
            .ToListAsync(cancellationToken);
        var withdrawals = await context.InvestmentWithdrawals
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.WithdrawalDate)
            .ToListAsync(cancellationToken);
        return investments.Select(investment => ToInvestmentResponse(
            investment,
            withdrawals.Where(item => item.InvestmentId == investment.Id).ToList())).ToList();
    }

    public async Task<InvestmentResponse> SaveInvestmentAsync(
        InvestmentRequest request,
        int? id,
        string userId,
        CancellationToken cancellationToken)
    {
        ValidateInvestment(request);
        Investment investment;
        FinancialCategory? category = null;
        if (id is null)
        {
            investment = new Investment
            {
                UserId = userId,
                Description = request.Description.Trim(),
                ApplicationDate = request.ApplicationDate,
                MaturityDate = request.MaturityDate,
                Amount = request.Amount
            };
            category = await GetOrCreateInvestmentCategoryAsync(userId, cancellationToken);
            context.Investments.Add(investment);
            context.Expenses.Add(new Expense
            {
                UserId = userId,
                Description = $"Aporte de investimento: {request.Description.Trim()}",
                Category = category,
                ExpenseDate = request.ApplicationDate,
                Amount = request.Amount,
                IsPaid = true,
                Investment = investment
            });
        }
        else
        {
            investment = await context.Investments.FirstOrDefaultAsync(
                item => item.Id == id && item.UserId == userId,
                cancellationToken)
                ?? throw new KeyNotFoundException("Investimento não encontrado.");
            var existingWithdrawals = await context.InvestmentWithdrawals
                .Where(item => item.InvestmentId == investment.Id && item.UserId == userId)
                .ToListAsync(cancellationToken);
            if (existingWithdrawals.Any(item => item.WithdrawalDate < request.ApplicationDate)
                || existingWithdrawals.Sum(item => item.Amount) > request.Amount)
            {
                throw new ArgumentException("A aplicação não pode ser alterada para ficar anterior ou inferior às retiradas já registradas.");
            }
            investment.Description = request.Description.Trim();
            investment.ApplicationDate = request.ApplicationDate;
            investment.MaturityDate = request.MaturityDate;
            investment.Amount = request.Amount;
            var expense = await context.Expenses.IgnoreQueryFilters().FirstOrDefaultAsync(
                item => item.InvestmentId == investment.Id && item.UserId == userId,
                cancellationToken);
            if (expense is not null)
            {
                expense.Description = $"Aporte de investimento: {request.Description.Trim()}";
                expense.ExpenseDate = request.ApplicationDate;
                expense.Amount = request.Amount;
                expense.IsPaid = true;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        var investmentWithdrawals = await context.InvestmentWithdrawals
            .Where(item => item.UserId == userId && item.InvestmentId == investment.Id)
            .OrderBy(item => item.WithdrawalDate)
            .ToListAsync(cancellationToken);
        return ToInvestmentResponse(investment, investmentWithdrawals);
    }

    public async Task<InvestmentWithdrawalResponse> WithdrawInvestmentAsync(
        int investmentId,
        InvestmentWithdrawalRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        var investment = await context.Investments.FirstOrDefaultAsync(
            item => item.Id == investmentId && item.UserId == userId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Investimento não encontrado.");
        if (request.WithdrawalDate < investment.ApplicationDate)
        {
            throw new ArgumentException("A data da retirada não pode ser anterior à aplicação.");
        }
        var withdrawn = await context.InvestmentWithdrawals
            .Where(item => item.UserId == userId && item.InvestmentId == investmentId)
            .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
        if (request.Amount > investment.Amount - withdrawn)
        {
            throw new ArgumentException("O valor da retirada excede o saldo disponível do investimento.");
        }
        if (request.ReturnAmount < -request.Amount)
        {
            throw new ArgumentException("A perda não pode ser maior que o valor retirado do principal.");
        }

        var withdrawal = new InvestmentWithdrawal
        {
            UserId = userId,
            InvestmentId = investmentId,
            WithdrawalDate = request.WithdrawalDate,
            Amount = request.Amount,
            ReturnAmount = request.ReturnAmount
        };
        context.InvestmentWithdrawals.Add(withdrawal);
        context.Incomes.Add(new Income
        {
            UserId = userId,
            Description = $"Retirada de investimento: {investment.Description}",
            AccountName = "Investimentos",
            CategoryName = "Retirada de investimento",
            ReceivedAt = request.WithdrawalDate,
            Amount = request.Amount + Math.Max(0, request.ReturnAmount),
            Type = IncomeType.Regular,
            InvestmentWithdrawal = withdrawal
        });
        if (request.ReturnAmount < 0)
        {
            var category = await GetOrCreateInvestmentCategoryAsync(userId, cancellationToken);
            context.Expenses.Add(new Expense
            {
                UserId = userId,
                Description = $"Perda em investimento: {investment.Description}",
                Category = category,
                ExpenseDate = request.WithdrawalDate,
                Amount = -request.ReturnAmount,
                IsPaid = true,
                InvestmentWithdrawal = withdrawal
            });
        }
        await context.SaveChangesAsync(cancellationToken);
        return new InvestmentWithdrawalResponse(
            withdrawal.Id,
            withdrawal.WithdrawalDate,
            withdrawal.Amount,
            withdrawal.ReturnAmount);
    }

    public async Task<bool> DeleteInvestmentWithdrawalAsync(
        int investmentId,
        int withdrawalId,
        string userId,
        CancellationToken cancellationToken)
    {
        var withdrawal = await context.InvestmentWithdrawals.FirstOrDefaultAsync(
            item => item.Id == withdrawalId
                && item.InvestmentId == investmentId
                && item.UserId == userId,
            cancellationToken);
        if (withdrawal is null)
        {
            return false;
        }
        context.InvestmentWithdrawals.Remove(withdrawal);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> HasExpensesUsingCategoryAsync(
        int categoryId,
        string userId,
        CancellationToken cancellationToken) =>
        await context.Expenses.AnyAsync(
            expense => expense.UserId == userId && expense.CategoryId == categoryId,
            cancellationToken)
        || await context.ExpenseRecurrenceRules.AnyAsync(
            rule => rule.UserId == userId && rule.CategoryId == categoryId,
            cancellationToken)
        || await context.CreditCardPurchases.IgnoreQueryFilters().AnyAsync(
            purchase => purchase.UserId == userId && purchase.CategoryId == categoryId,
            cancellationToken)
        || await context.CardPurchaseRecurrenceRules.AnyAsync(
            rule => rule.UserId == userId && rule.CategoryId == categoryId,
            cancellationToken);

    public async Task<bool> CancelRecurringExpensesFromAsync(
        int expenseId,
        string userId,
        CancellationToken cancellationToken)
    {
        var expense = await context.Expenses.FirstOrDefaultAsync(
            item => item.Id == expenseId && item.UserId == userId,
            cancellationToken);
        if (expense?.RecurrenceRuleId is null)
        {
            return false;
        }

        var rule = await context.ExpenseRecurrenceRules.FirstOrDefaultAsync(
            item => item.Id == expense.RecurrenceRuleId && item.UserId == userId,
            cancellationToken);
        if (rule is null)
        {
            return false;
        }
        rule.IsActive = false;
        rule.EndInstallment = expense.InstallmentNumber - 1;
        var remainingExpenses = await context.Expenses
            .IgnoreQueryFilters()
            .Where(item => item.RecurrenceRuleId == rule.Id
                && item.InstallmentNumber >= expense.InstallmentNumber)
            .ToListAsync(cancellationToken);
        foreach (var remainingExpense in remainingExpenses)
        {
            remainingExpense.IsDeleted = true;
        }
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MonthlySummary> GetMonthlySummaryAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        ValidateYearMonth(year, month);
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);
        await EnsureRecurringExpensesForRangeAsync(userId, start, end, cancellationToken);
        var incomes = await context.Incomes.Where(item => item.UserId == userId && item.ReceivedAt >= start && item.ReceivedAt < end)
            .ToListAsync(cancellationToken);
        var expenses = await context.Expenses.Where(item => item.UserId == userId && item.ExpenseDate >= start && item.ExpenseDate < end)
            .ToListAsync(cancellationToken);
        var openingBalance = await GetOpeningBalanceAsync(userId, start, cancellationToken);
        return FinanceCalculator.Calculate(year, month, openingBalance, incomes, expenses);
    }

    public async Task<MonthlySummary> SaveOpeningBalanceAsync(
        string userId,
        int year,
        int month,
        decimal amount,
        CancellationToken cancellationToken)
    {
        ValidateYearMonth(year, month);
        var balance = await context.MonthlyBalanceOverrides.FirstOrDefaultAsync(
            item => item.UserId == userId && item.Year == year && item.Month == month,
            cancellationToken);
        if (balance is null)
        {
            context.MonthlyBalanceOverrides.Add(new MonthlyBalanceOverride
            {
                UserId = userId,
                Year = year,
                Month = month,
                Amount = amount
            });
        }
        else
        {
            balance.Amount = amount;
        }
        await context.SaveChangesAsync(cancellationToken);
        return await GetMonthlySummaryAsync(userId, year, month, cancellationToken);
    }

    public async Task<AnnualSummary> GetAnnualSummaryAsync(
        string userId,
        int year,
        CancellationToken cancellationToken)
    {
        if (year is < 1900 or > 9998)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Ano inválido.");
        }
        await EnsureRecurringExpensesForRangeAsync(
            userId,
            new DateOnly(year, 1, 1),
            new DateOnly(year + 1, 1, 1),
            cancellationToken);
        await EnsureRecurringCardPurchasesForRangeAsync(
            userId,
            new DateOnly(year, 1, 1),
            new DateOnly(year + 1, 1, 1),
            cancellationToken);
        var incomes = await context.Incomes.Where(item => item.UserId == userId && item.ReceivedAt.Year == year).ToListAsync(cancellationToken);
        var expenses = await context.Expenses.Where(item => item.UserId == userId && item.ExpenseDate.Year == year).ToListAsync(cancellationToken);
        var purchases = await context.CreditCardPurchases.Where(item => item.UserId == userId && item.PurchaseDate.Year == year).ToListAsync(cancellationToken);
        var benefitEntries = await context.BenefitEntries.Where(item => item.UserId == userId && item.EntryDate.Year == year).ToListAsync(cancellationToken);
        var months = new List<MonthlySummary>(12);
        for (var month = 1; month <= 12; month++)
        {
            var currentMonth = month;
            months.Add(await GetMonthlySummaryAsync(userId, year, currentMonth, cancellationToken));
        }
        return new AnnualSummary(
            year,
            incomes.Sum(item => item.Amount),
            expenses.Sum(item => item.Amount),
            purchases.Sum(item => item.Amount),
            benefitEntries.Where(item => item.Type == BenefitEntryType.Recharge).Sum(item => item.Amount)
                + incomes.Where(item => item.Type == IncomeType.BenefitCredit).Sum(item => item.Amount),
            benefitEntries.Where(item => item.Type == BenefitEntryType.Purchase).Sum(item => item.Amount),
            months);
    }

    private async Task EnsureRecurringExpensesForRangeAsync(
        string userId,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        CancellationToken cancellationToken)
    {
        var firstRangeMonth = new DateOnly(rangeStart.Year, rangeStart.Month, 1);
        var lastRangeMonth = new DateOnly(rangeEnd.Year, rangeEnd.Month, 1);
        var rules = await context.ExpenseRecurrenceRules
            .Where(rule => rule.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            var firstRuleMonth = new DateOnly(rule.StartDate.Year, rule.StartDate.Month, 1);
            if (lastRangeMonth <= firstRuleMonth)
            {
                continue;
            }

            var firstOccurrenceMonth = firstRangeMonth < firstRuleMonth ? firstRuleMonth : firstRangeMonth;
            var monthOffset = (firstOccurrenceMonth.Year - rule.StartDate.Year) * 12
                + firstOccurrenceMonth.Month - rule.StartDate.Month;
            var firstInstallment = rule.StartInstallment + monthOffset;
            var monthsInRange = (lastRangeMonth.Year - firstOccurrenceMonth.Year) * 12
                + lastRangeMonth.Month - firstOccurrenceMonth.Month;
            var lastInstallment = firstInstallment + monthsInRange - 1;
            if (rule.TotalInstallments is not null)
            {
                lastInstallment = Math.Min(lastInstallment, rule.TotalInstallments.Value);
            }
            if (rule.EndInstallment is not null)
            {
                lastInstallment = Math.Min(lastInstallment, rule.EndInstallment.Value);
            }
            if (lastInstallment < firstInstallment)
            {
                continue;
            }

            var existingInstallments = await context.Expenses
                .IgnoreQueryFilters()
                .Where(expense => expense.RecurrenceRuleId == rule.Id
                    && expense.InstallmentNumber >= firstInstallment
                    && expense.InstallmentNumber <= lastInstallment)
                .Select(expense => expense.InstallmentNumber)
                .ToListAsync(cancellationToken);
            var existingSet = existingInstallments.ToHashSet();
            for (var installment = firstInstallment; installment <= lastInstallment; installment++)
            {
                if (existingSet.Contains(installment))
                {
                    continue;
                }
                var occurrenceDate = ExpenseRecurrenceSchedule.GetInstallmentDate(rule, installment);
                if (occurrenceDate < rangeStart || occurrenceDate >= rangeEnd)
                {
                    continue;
                }
                context.Expenses.Add(new Expense
                {
                    UserId = userId,
                    Description = rule.Description,
                    CategoryId = rule.CategoryId,
                    ExpenseDate = occurrenceDate,
                    Amount = rule.Amount,
                    IsPaid = false,
                    RecurrenceRule = rule,
                    InstallmentNumber = installment,
                    TotalInstallments = rule.TotalInstallments
                });
            }
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureRecurringCardPurchasesForRangeAsync(
        string userId,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        CancellationToken cancellationToken)
    {
        var firstRangeMonth = new DateOnly(rangeStart.Year, rangeStart.Month, 1);
        var lastRangeMonth = new DateOnly(rangeEnd.Year, rangeEnd.Month, 1);
        var rules = await context.CardPurchaseRecurrenceRules
            .Where(rule => rule.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            var firstRuleMonth = new DateOnly(rule.StartDate.Year, rule.StartDate.Month, 1);
            if (lastRangeMonth <= firstRuleMonth)
            {
                continue;
            }

            var firstOccurrenceMonth = firstRangeMonth < firstRuleMonth ? firstRuleMonth : firstRangeMonth;
            var monthOffset = (firstOccurrenceMonth.Year - rule.StartDate.Year) * 12
                + firstOccurrenceMonth.Month - rule.StartDate.Month;
            var firstInstallment = rule.StartInstallment + monthOffset;
            var monthsInRange = (lastRangeMonth.Year - firstOccurrenceMonth.Year) * 12
                + lastRangeMonth.Month - firstOccurrenceMonth.Month;
            var lastInstallment = firstInstallment + monthsInRange - 1;
            if (rule.TotalInstallments is not null)
            {
                lastInstallment = Math.Min(lastInstallment, rule.TotalInstallments.Value);
            }
            if (rule.EndInstallment is not null)
            {
                lastInstallment = Math.Min(lastInstallment, rule.EndInstallment.Value);
            }
            if (lastInstallment < firstInstallment)
            {
                continue;
            }

            var existingInstallments = await context.CreditCardPurchases
                .IgnoreQueryFilters()
                .Where(purchase => purchase.RecurrenceRuleId == rule.Id
                    && purchase.InstallmentNumber >= firstInstallment
                    && purchase.InstallmentNumber <= lastInstallment)
                .Select(purchase => purchase.InstallmentNumber)
                .ToListAsync(cancellationToken);
            var existingSet = existingInstallments.ToHashSet();
            for (var installment = firstInstallment; installment <= lastInstallment; installment++)
            {
                if (existingSet.Contains(installment))
                {
                    continue;
                }
                var occurrenceDate = ExpenseRecurrenceSchedule.GetInstallmentDate(rule, installment);
                if (occurrenceDate < rangeStart || occurrenceDate >= rangeEnd)
                {
                    continue;
                }
                context.CreditCardPurchases.Add(new CreditCardPurchase
                {
                    UserId = userId,
                    CreditCardId = rule.CreditCardId,
                    CategoryId = rule.CategoryId,
                    Description = rule.Description,
                    PurchaseDate = occurrenceDate,
                    Amount = rule.Amount,
                    RecurrenceRule = rule,
                    InstallmentNumber = installment,
                    TotalInstallments = rule.TotalInstallments
                });
            }
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static void ValidateRecurrence(
        ExpenseRecurrenceType recurrenceType,
        int startInstallment,
        int? totalInstallments)
    {
        if (recurrenceType == ExpenseRecurrenceType.Installments
            && (startInstallment < 1
                || totalInstallments is null
                || totalInstallments < startInstallment))
        {
            throw new ArgumentException("Informe uma parcela inicial e um total válido.");
        }
        if (recurrenceType == ExpenseRecurrenceType.Unlimited && startInstallment < 1)
        {
            throw new ArgumentException("A parcela inicial deve ser maior que zero.");
        }
        if (recurrenceType is not (ExpenseRecurrenceType.Installments or ExpenseRecurrenceType.Unlimited))
        {
            throw new ArgumentException("Selecione um tipo de recorrência válido.");
        }
    }

    private static ExpenseResponse ToExpenseResponse(
        Expense expense,
        ExpenseRecurrenceType? recurrenceType = null) =>
        new(
            expense.Id,
            expense.Description,
            expense.CategoryId,
            expense.ExpenseDate,
            expense.Amount,
            expense.IsPaid,
            recurrenceType ?? expense.RecurrenceRule?.RecurrenceType ?? ExpenseRecurrenceType.None,
            expense.InstallmentNumber,
            expense.TotalInstallments);

    private static CardPurchaseResponse ToCardPurchaseResponse(
        CreditCardPurchase purchase,
        ExpenseRecurrenceType recurrenceType) =>
        new(
            purchase.Id,
            purchase.CreditCardId,
            purchase.CategoryId,
            purchase.Description,
            purchase.PurchaseDate,
            purchase.Amount,
            recurrenceType,
            purchase.InstallmentNumber,
            purchase.TotalInstallments);

    private async Task<FinancialCategory> GetOrCreateInvestmentCategoryAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var category = await context.Categories.FirstOrDefaultAsync(
            item => item.UserId == userId && item.Type == "expense" && item.Name == "Investimentos",
            cancellationToken);
        if (category is not null)
        {
            return category;
        }

        category = new FinancialCategory
        {
            UserId = userId,
            Name = "Investimentos",
            Type = "expense"
        };
        context.Categories.Add(category);
        return category;
    }

    private static void ValidateInvestment(InvestmentRequest request)
    {
        if (request.MaturityDate < request.ApplicationDate)
        {
            throw new ArgumentException("O vencimento não pode ser anterior à data da aplicação.");
        }
        if (request.Amount <= 0)
        {
            throw new ArgumentException("O valor aplicado deve ser maior que zero.");
        }
    }

    private static InvestmentResponse ToInvestmentResponse(
        Investment investment,
        IReadOnlyList<InvestmentWithdrawal> withdrawals)
    {
        var withdrawnAmount = withdrawals.Sum(item => item.Amount);
        return new InvestmentResponse(
            investment.Id,
            investment.Description,
            investment.ApplicationDate,
            investment.MaturityDate,
            investment.Amount,
            withdrawnAmount,
            investment.Amount - withdrawnAmount,
            withdrawals.Select(item => new InvestmentWithdrawalResponse(
                item.Id,
                item.WithdrawalDate,
                item.Amount,
                item.ReturnAmount)).ToList());
    }

    private async Task<decimal> GetOpeningBalanceAsync(
        string userId,
        DateOnly currentMonth,
        CancellationToken cancellationToken)
    {
        var overrideBalance = await context.MonthlyBalanceOverrides.FirstOrDefaultAsync(
            item => item.UserId == userId && item.Year == currentMonth.Year && item.Month == currentMonth.Month,
            cancellationToken);
        if (overrideBalance is not null)
        {
            return overrideBalance.Amount;
        }

        var previousOverride = await context.MonthlyBalanceOverrides
            .Where(item => item.UserId == userId &&
                (item.Year < currentMonth.Year || (item.Year == currentMonth.Year && item.Month < currentMonth.Month)))
            .OrderByDescending(item => item.Year)
            .ThenByDescending(item => item.Month)
            .FirstOrDefaultAsync(cancellationToken);
        var startingAmount = previousOverride?.Amount ?? 0;
        var incomeHistory = context.Incomes.Where(item => item.UserId == userId && item.ReceivedAt < currentMonth);
        var expenseHistory = context.Expenses.Where(item =>
            item.UserId == userId && item.ExpenseDate < currentMonth && item.RecurrenceRuleId == null);
        DateOnly? historyStart = null;
        if (previousOverride is not null)
        {
            var overrideMonth = new DateOnly(previousOverride.Year, previousOverride.Month, 1);
            historyStart = overrideMonth;
            incomeHistory = incomeHistory.Where(item => item.ReceivedAt >= overrideMonth);
            expenseHistory = expenseHistory.Where(item => item.ExpenseDate >= overrideMonth);
        }
        var incomesBeforeMonth = await incomeHistory.SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
        var expensesBeforeMonth = await expenseHistory.SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
        var recurringExpensesBeforeMonth = await GetRecurringExpensesTotalBeforeMonthAsync(
            userId,
            currentMonth,
            historyStart,
            cancellationToken);
        return startingAmount + incomesBeforeMonth - expensesBeforeMonth - recurringExpensesBeforeMonth;
    }

    private async Task<decimal> GetRecurringExpensesTotalBeforeMonthAsync(
        string userId,
        DateOnly currentMonth,
        DateOnly? historyStart,
        CancellationToken cancellationToken)
    {
        var rules = await context.ExpenseRecurrenceRules
            .Where(rule => rule.UserId == userId)
            .ToListAsync(cancellationToken);
        if (rules.Count == 0)
        {
            return 0;
        }

        var expenses = await context.Expenses
            .IgnoreQueryFilters()
            .Where(expense => expense.UserId == userId && expense.RecurrenceRuleId != null)
            .ToListAsync(cancellationToken);
        var total = 0m;
        foreach (var rule in rules)
        {
            var startMonth = new DateOnly(rule.StartDate.Year, rule.StartDate.Month, 1);
            var monthOffset = (currentMonth.Year - startMonth.Year) * 12 + currentMonth.Month - startMonth.Month;
            if (monthOffset <= 0)
            {
                continue;
            }
            var firstInstallment = rule.StartInstallment;
            if (historyStart is not null && historyStart.Value > startMonth)
            {
                var monthsToHistoryStart = (historyStart.Value.Year - startMonth.Year) * 12
                    + historyStart.Value.Month - startMonth.Month;
                firstInstallment += monthsToHistoryStart;
            }
            var lastInstallment = rule.StartInstallment + monthOffset - 1;
            if (rule.TotalInstallments is not null)
            {
                lastInstallment = Math.Min(lastInstallment, rule.TotalInstallments.Value);
            }
            if (rule.EndInstallment is not null)
            {
                lastInstallment = Math.Min(lastInstallment, rule.EndInstallment.Value);
            }
            if (lastInstallment < firstInstallment)
            {
                continue;
            }

            var occurrenceCount = lastInstallment - firstInstallment + 1;
            total += occurrenceCount * rule.Amount;
            foreach (var occurrence in expenses.Where(item =>
                item.RecurrenceRuleId == rule.Id
                && item.InstallmentNumber >= firstInstallment
                && item.InstallmentNumber <= lastInstallment))
            {
                total += occurrence.IsDeleted
                    || occurrence.ExpenseDate >= currentMonth
                    || (historyStart is not null && occurrence.ExpenseDate < historyStart.Value)
                    ? -rule.Amount
                    : occurrence.Amount - rule.Amount;
            }
        }
        return total;
    }

    private static void ValidateYearMonth(int year, int month)
    {
        if (year is < 1900 or > 9998 || month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Ano ou mês inválido.");
        }
    }
}
