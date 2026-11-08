using System.Security.Claims;
using FinanceiroRHC.Api.Models;
using FinanceiroRHC.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceiroRHC.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class FinanceController(IFinanceService financeService) : ControllerBase
{
    [HttpGet("incomes")]
    public Task<List<Income>> GetIncomes(CancellationToken token) => GetAsync<Income>(token);

    [HttpPost("incomes")]
    public Task<Income> CreateIncome(IncomeRequest request, CancellationToken token) =>
        SaveAsync<Income, IncomeRequest>(request, null, (body, id, userId) => new Income
        {
            Id = id ?? 0,
            UserId = userId,
            Description = body.Description,
            AccountName = body.AccountName,
            CategoryName = body.CategoryName,
            ReceivedAt = body.ReceivedAt,
            Amount = body.Amount
        }, token);

    [HttpPut("incomes/{id:int}")]
    public Task<Income> UpdateIncome(int id, IncomeRequest request, CancellationToken token) =>
        SaveAsync<Income, IncomeRequest>(request, id, (body, entityId, userId) => new Income
        {
            Id = entityId ?? 0,
            UserId = userId,
            Description = body.Description,
            AccountName = body.AccountName,
            CategoryName = body.CategoryName,
            ReceivedAt = body.ReceivedAt,
            Amount = body.Amount
        }, token);

    [HttpDelete("incomes/{id:int}")]
    public Task<bool> DeleteIncome(int id, CancellationToken token) => DeleteAsync<Income>(id, token);

    [HttpGet("expenses")]
    public async Task<List<ExpenseResponse>> GetExpenses(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken token)
    {
        var current = DateTime.UtcNow;
        return await financeService.GetExpensesForMonthAsync(
            UserId,
            year ?? current.Year,
            month ?? current.Month,
            token);
    }

    [HttpPost("expenses")]
    public Task<ExpenseResponse> CreateExpense(ExpenseRequest request, CancellationToken token) =>
        SaveExpenseAsync(request, null, token);

    [HttpPut("expenses/{id:int}")]
    public Task<ExpenseResponse> UpdateExpense(int id, ExpenseRequest request, CancellationToken token) =>
        SaveExpenseAsync(request, id, token);

    [HttpDelete("expenses/{id:int}")]
    public Task<bool> DeleteExpense(int id, CancellationToken token) => DeleteAsync<Expense>(id, token);

    [HttpDelete("expenses/{id:int}/recurrence")]
    public async Task<IActionResult> CancelExpenseRecurrence(int id, CancellationToken token) =>
        await financeService.CancelRecurringExpensesFromAsync(id, UserId, token)
            ? NoContent()
            : NotFound();

    [HttpGet("categories")]
    public Task<List<FinancialCategory>> GetCategories(CancellationToken token) => GetAsync<FinancialCategory>(token);

    [HttpPost("categories")]
    public Task<FinancialCategory> CreateCategory(CategoryRequest request, CancellationToken token) =>
        SaveCategoryAsync(request, null, token);

    [HttpPut("categories/{id:int}")]
    public Task<FinancialCategory> UpdateCategory(int id, CategoryRequest request, CancellationToken token) =>
        SaveCategoryAsync(request, id, token);

    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken token)
    {
        if (await financeService.GetByIdAsync<FinancialCategory>(id, UserId, token) is null)
        {
            return NotFound();
        }
        if (await financeService.HasExpensesUsingCategoryAsync(id, UserId, token))
        {
            return Conflict(new { message = "Esta categoria está vinculada a lançamentos ou recorrências e não pode ser excluída." });
        }
        await financeService.DeleteAsync<FinancialCategory>(id, UserId, token);
        return NoContent();
    }

    [HttpGet("cards")]
    public Task<List<CreditCard>> GetCards(CancellationToken token) => GetAsync<CreditCard>(token);

    [HttpPost("cards")]
    public Task<CreditCard> CreateCard(CardRequest request, CancellationToken token) =>
        SaveAsync<CreditCard, CardRequest>(request, null, (body, id, userId) => new CreditCard
        {
            Id = id ?? 0, UserId = userId, Name = body.Name
        }, token);

    [HttpPut("cards/{id:int}")]
    public Task<CreditCard> UpdateCard(int id, CardRequest request, CancellationToken token) =>
        SaveAsync<CreditCard, CardRequest>(request, id, (body, entityId, userId) => new CreditCard
        {
            Id = entityId ?? 0, UserId = userId, Name = body.Name
        }, token);

    [HttpDelete("cards/{id:int}")]
    public async Task<IActionResult> DeleteCard(int id, CancellationToken token)
    {
        if (await financeService.GetByIdAsync<CreditCard>(id, UserId, token) is null)
        {
            return NotFound();
        }
        var purchases = await financeService.GetAsync<CreditCardPurchase>(UserId, token);
        if (purchases.Any(purchase => purchase.CreditCardId == id))
        {
            return Conflict(new { message = "Este cartão possui compras registradas e não pode ser excluído." });
        }
        await financeService.DeleteAsync<CreditCard>(id, UserId, token);
        return NoContent();
    }

    [HttpGet("card-purchases")]
    public async Task<List<CardPurchaseResponse>> GetCardPurchases(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken token)
    {
        var current = DateTime.UtcNow;
        return await financeService.GetCardPurchasesForMonthAsync(
            UserId,
            year ?? current.Year,
            month ?? current.Month,
            token);
    }

    [HttpPost("card-purchases")]
    public Task<CardPurchaseResponse> CreateCardPurchase(CreditCardPurchaseRequest request, CancellationToken token) =>
        SaveCardPurchaseAsync(request, null, token);

    [HttpPut("card-purchases/{id:int}")]
    public Task<CardPurchaseResponse> UpdateCardPurchase(int id, CreditCardPurchaseRequest request, CancellationToken token) =>
        SaveCardPurchaseAsync(request, id, token);

    [HttpDelete("card-purchases/{id:int}")]
    public Task<bool> DeleteCardPurchase(int id, CancellationToken token) => DeleteAsync<CreditCardPurchase>(id, token);

    [HttpDelete("card-purchases/{id:int}/recurrence")]
    public async Task<IActionResult> CancelCardPurchaseRecurrence(int id, CancellationToken token) =>
        await financeService.CancelRecurringCardPurchasesFromAsync(id, UserId, token)
            ? NoContent()
            : NotFound();

    [HttpGet("benefit-entries")]
    public Task<List<BenefitEntry>> GetBenefitEntries(CancellationToken token) => GetAsync<BenefitEntry>(token);

    [HttpPost("benefit-entries")]
    public Task<BenefitEntry> CreateBenefitEntry(BenefitEntryRequest request, CancellationToken token) =>
        SaveBenefitEntryAsync(request, null, token);

    [HttpPut("benefit-entries/{id:int}")]
    public Task<BenefitEntry> UpdateBenefitEntry(int id, BenefitEntryRequest request, CancellationToken token) =>
        SaveBenefitEntryAsync(request, id, token);

    [HttpDelete("benefit-entries/{id:int}")]
    public Task<bool> DeleteBenefitEntry(int id, CancellationToken token) => DeleteAsync<BenefitEntry>(id, token);

    [HttpGet("summary/monthly")]
    public async Task<ActionResult<MonthlySummary>> GetMonthlySummary([FromQuery] int year, [FromQuery] int month, CancellationToken token) =>
        await financeService.GetMonthlySummaryAsync(UserId, year, month, token);

    [HttpPut("summary/opening-balance")]
    public async Task<ActionResult<MonthlySummary>> SetOpeningBalance(BalanceOverrideRequest request, CancellationToken token) =>
        await financeService.SaveOpeningBalanceAsync(UserId, request.Year, request.Month, request.Amount, token);

    [HttpGet("summary/annual")]
    public async Task<ActionResult<AnnualSummary>> GetAnnualSummary([FromQuery] int year, CancellationToken token) =>
        await financeService.GetAnnualSummaryAsync(UserId, year, token);

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("O token não contém o identificador do usuário.");

    private Task<List<T>> GetAsync<T>(CancellationToken token) where T : class, IUserOwnedEntity =>
        financeService.GetAsync<T>(UserId, token);

    private async Task<T> SaveAsync<T, TRequest>(
        TRequest request,
        int? id,
        Func<TRequest, int?, string, T> create,
        CancellationToken token) where T : class, IUserOwnedEntity
    {
        var entity = create(request, id, UserId);
        if (id is not null && await financeService.GetByIdAsync<T>(id.Value, UserId, token) is null)
        {
            throw new KeyNotFoundException("Registro não encontrado.");
        }
        return await financeService.SaveAsync(entity, id, UserId, token);
    }

    private async Task<ExpenseResponse> SaveExpenseAsync(ExpenseRequest request, int? id, CancellationToken token)
    {
        var category = await financeService.GetByIdAsync<FinancialCategory>(request.CategoryId, UserId, token);
        if (category is null || category.Type != "expense")
        {
            throw new ArgumentException("Selecione uma categoria de despesa válida.");
        }
        return await financeService.SaveExpenseAsync(request, id, UserId, token);
    }

    private Task<FinancialCategory> SaveCategoryAsync(CategoryRequest request, int? id, CancellationToken token) =>
        SaveAsync<FinancialCategory, CategoryRequest>(request, id, (body, entityId, userId) => new FinancialCategory
        {
            Id = entityId ?? 0, UserId = userId, Name = body.Name.Trim(), Type = body.Type
        }, token);

    private async Task<CardPurchaseResponse> SaveCardPurchaseAsync(CreditCardPurchaseRequest request, int? id, CancellationToken token)
    {
        if (await financeService.GetByIdAsync<CreditCard>(request.CreditCardId, UserId, token) is null)
        {
            throw new ArgumentException("Selecione um cartão válido.");
        }
        var category = await financeService.GetByIdAsync<FinancialCategory>(request.CategoryId, UserId, token);
        if (category is null || category.Type != "expense")
        {
            throw new ArgumentException("Selecione uma categoria de despesa válida.");
        }
        return await financeService.SaveCardPurchaseAsync(request, id, UserId, token);
    }

    private Task<BenefitEntry> SaveBenefitEntryAsync(BenefitEntryRequest request, int? id, CancellationToken token) =>
        SaveAsync<BenefitEntry, BenefitEntryRequest>(request, id, (body, entityId, userId) => new BenefitEntry
        {
            Id = entityId ?? 0,
            UserId = userId,
            Description = body.Description,
            EntryDate = body.EntryDate,
            Amount = body.Amount,
            Type = body.Type
        }, token);

    private Task<bool> DeleteAsync<T>(int id, CancellationToken token) where T : class, IUserOwnedEntity =>
        financeService.DeleteAsync<T>(id, UserId, token);
}
