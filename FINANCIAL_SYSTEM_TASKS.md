# Системаи ягонаи молиявӣ - Task List

## Марҳилаи 1: Entities ва Database
- [ ] Эҷоди `Transaction.cs`
- [ ] Эҷоди `TransactionCategory.cs`
- [ ] Эҷоди `FinancialPeriod.cs`
- [ ] Эҷоди `TransactionType.cs` enum
- [ ] Тағйир додани `DataContext.cs` (DbSets + OnModelCreating)
- [ ] Migration: `dotnet ef migrations add UnifiedFinancialSystem`
- [ ] Database update

## Марҳилаи 2: DTOs
- [ ] `CreateTransactionDto.cs`
- [ ] `UpdateTransactionDto.cs`
- [ ] `GetTransactionDto.cs`
- [ ] `CreateTransactionCategoryDto.cs`
- [ ] `UpdateTransactionCategoryDto.cs`
- [ ] `GetTransactionCategoryDto.cs`
- [ ] `FinancialSummaryDto.cs`
- [ ] `CategoryBreakdownDto.cs`
- [ ] `DailySummaryDto.cs`
- [ ] `MonthlySummaryDto.cs`
- [ ] `TransactionFilter.cs`

## Марҳилаи 3: Services
- [ ] `ITransactionService.cs` interface
- [ ] `TransactionService.cs` implementation
- [ ] `ITransactionCategoryService.cs` interface
- [ ] `TransactionCategoryService.cs` implementation
- [ ] `IFinancialPeriodService.cs` interface
- [ ] `FinancialPeriodService.cs` implementation

## Марҳилаи 4: Integration
- [ ] Payment → Income auto-sync
- [ ] Payroll → Expense auto-sync
- [ ] Default categories seed

## Марҳилаи 5: Controllers
- [ ] `TransactionController.cs`
- [ ] `TransactionCategoryController.cs`
- [ ] `FinancialPeriodController.cs`

## Марҳилаи 6: Migration from Old System
- [ ] Delete `FinanceService.cs`
- [ ] Delete `ExpenseService.cs`
- [ ] Delete `IFinanceService.cs`
- [ ] Delete `IExpenseService.cs`
- [ ] Update/Delete `FinanceController.cs`
- [ ] Update/Delete `ExpenseController.cs`
- [ ] Update `Register.cs` (remove old, add new)

## Марҳилаи 7: Messages
- [ ] Add `Messages.Transaction` class
- [ ] Add `Messages.TransactionCategory` class

## Марҳилаи 8: DI Registration
- [ ] Register new services in `Register.cs`

## Марҳилаи 9: Testing
- [ ] Manual CRUD testing
- [ ] Auto-sync testing (Payment → Income)
- [ ] Auto-sync testing (Payroll → Expense)
- [ ] Analytics testing
- [ ] Month close testing

## Марҳилаи 10: Documentation
- [ ] API documentation
- [ ] README for frontend
- [ ] Walkthrough creation
