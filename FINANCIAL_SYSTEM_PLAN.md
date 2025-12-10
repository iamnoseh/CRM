# Нақшаи Системаи Ягонаи Молиявӣ (Unified Financial System)

**Версия:** 2.0  
**Модул:** Income & Expense Management  
**Тартиб:** Идея #1 - Системаи пурраи даромад ва харочот

---

## 📋 Хулоса

Сохтани системаи ягонаи молиявӣ барои идораи ҳамаи даромадҳо, харочотҳо, категорияҳо ва таҳлили молиявӣ.

### Тағйироти асосӣ:
- ✅ **Нигоҳ доштан:** PayrollService (нав сохта шуд)
- ❌ **Нест кардан:** FinanceService (қадимӣ, методҳои норавшан)
- ❌ **Нест кардан:** ExpenseService (дубликатсия)
- ✅ **Интегратсия:** ReceiptService → дар системаи нав

---

## 🎯 Мақсадҳо

1. **Consolidation:** Ҳамаи функсияҳои молиявиро ба як системаи ягона ҷамъ кардан
2. **Clarity:** Категорияҳои равшан (Income/Expense Types)
3. **Analytics:** Ҳисоботҳо, статистика, графикҳо
4. **Automation:** Даромадҳо аз Payment автоматӣ ва Payroll integration

---

## 🏗️ Архитектураи нав

### Entities

#### 1. Transaction (Асосӣ)
```csharp
public class Transaction : BaseEntity
{
    public int CenterId { get; set; }
    public TransactionType Type { get; set; } // Income, Expense
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public int CategoryId { get; set; }
    public TransactionCategory Category { get; set; }
    
    // Источники
    public int? PaymentId { get; set; } // Link to Payment
    public int? PayrollRecordId { get; set; } // Link to Payroll
    public int? ManualEntryById { get; set; } // Manual entry user
    
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? ReceiptPath { get; set; }
    
    // Metadata
    public bool IsReconciled { get; set; }
    public DateTime? ReconciledDate { get; set; }
}
```

#### 2. TransactionCategory
```csharp
public class TransactionCategory : BaseEntity
{
    public int CenterId { get; set; }
    public TransactionType Type { get; set; }
    public string Name { get; set; } // "Кироя", "Маош", "Пардохтҳои донишҷӯён"
    public string? Description { get; set; }
    public string? Icon { get; set; } // For UI
    public string? Color { get; set; } // For charts
    public bool IsSystem { get; set; } // Cannot be deleted
    public int DisplayOrder { get; set; }
    
    public List<Transaction> Transactions { get; set; }
}
```

#### 3. FinancialPeriod (Барои "Month Closed")
```csharp
public class FinancialPeriod : BaseEntity
{
    public int CenterId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedDate { get; set; }
    public int? ClosedByUserId { get; set; }
    
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }
}
```

### Enums

```csharp
public enum TransactionType
{
    Income = 0,
    Expense = 1
}
```

---

## 📦 Марҳилаҳои амалсозӣ

### Марҳилаи 1: Entities ва Database (2-3 рӯз)

#### Entities эҷод
- [ ] `Domain/Entities/Transaction.cs`
- [ ] `Domain/Entities/TransactionCategory.cs`
- [ ] `Domain/Entities/FinancialPeriod.cs`
- [ ] `Domain/Enums/TransactionType.cs`

#### DataContext
- [ ] DbSets илова
- [ ] OnModelCreating конфигуратсия
  - Relationships
  - Indexes: (CenterId, TransactionDate), (CategoryId), (Type)
  - Decimal precision (18,2)
  - Default categories seed

#### Migration
- [ ] `dotnet ef migrations add UnifiedFinancialSystem`
- [ ] `dotnet ef database update`

---

### Марҳилаи 2: DTOs (1 рӯз)

#### Transaction DTOs
- [ ] `CreateTransactionDto` - Manual entry
- [ ] `UpdateTransactionDto`
- [ ] `GetTransactionDto` - With category name, source info
- [ ] `TransactionSummaryDto` - For lists

#### Category DTOs
- [ ] `CreateTransactionCategoryDto`
- [ ] `UpdateTransactionCategoryDto`
- [ ] `GetTransactionCategoryDto`

#### Analytics DTOs
- [ ] `FinancialSummaryDto` - Total income, expense, profit
- [ ] `CategoryBreakdownDto` - Pie chart data
- [ ] `DailySummaryDto` - Daily stats
- [ ] `MonthlySummaryDto` - Monthly trends
- [ ] `PeriodComparisonDto` - Month-over-month

#### Filter
- [ ] `TransactionFilter` - Date range, type, category

---

### Марҳилаи 3: Core Services (3-4 рӯз)

#### TransactionService
```csharp
public interface ITransactionService
{
    // CRUD
    Task<Response<GetTransactionDto>> CreateAsync(CreateTransactionDto dto);
    Task<Response<GetTransactionDto>> UpdateAsync(int id, UpdateTransactionDto dto);
    Task<Response<string>> DeleteAsync(int id);
    Task<Response<GetTransactionDto>> GetByIdAsync(int id);
    Task<PaginationResponse<List<GetTransactionDto>>> GetPaginatedAsync(TransactionFilter filter);
    
    // Analytics
    Task<Response<FinancialSummaryDto>> GetSummaryAsync(int month, int year);
    Task<Response<List<CategoryBreakdownDto>>> GetCategoryBreakdownAsync(int month, int year, TransactionType type);
    Task<Response<List<DailySummaryDto>>> GetDailyTrendAsync(DateTime startDate, DateTime endDate);
    
    // Auto-sync
    Task SyncPaymentAsIncomeAsync(int paymentId); // Called from Payment creation
    Task SyncPayrollAsExpenseAsync(int payrollRecordId); // Called from Payroll paid
}
```

#### TransactionCategoryService
```csharp
public interface ITransactionCategoryService
{
    Task<Response<GetTransactionCategoryDto>> CreateAsync(CreateTransactionCategoryDto dto);
    Task<Response<GetTransactionCategoryDto>> UpdateAsync(int id, UpdateTransactionCategoryDto dto);
    Task<Response<string>> DeleteAsync(int id); // Cannot delete if transactions exist or IsSystem
    Task<Response<List<GetTransactionCategoryDto>>> GetAllAsync(TransactionType? type);
    Task SeedDefaultCategoriesAsync(int centerId); // Called on center creation
}
```

#### FinancialPeriodService
```csharp
public interface IFinancialPeriodService
{
    Task<Response<string>> CloseMonthAsync(int month, int year);
    Task<Response<bool>> IsMonthClosedAsync(int month, int year);
    Task<Response<FinancialPeriod>> GetPeriodAsync(int month, int year);
}
```

---

### Марҳилаи 4: Integration (2 рӯз)

#### Payment Integration
Дар `PaymentService` ё `StudentAccountService`:
```csharp
public async Task ProcessPaymentAsync(...)
{
    // Existing payment logic
    var payment = await SavePayment(...);
    
    // NEW: Auto-create income transaction
    await transactionService.SyncPaymentAsIncomeAsync(payment.Id);
}
```

#### Payroll Integration
Дар `PayrollService.MarkAsPaidAsync`:
```csharp
public async Task<Response<GetPayrollRecordDto>> MarkAsPaidAsync(int id, MarkAsPaidDto dto)
{
    // Existing logic
    record.Status = PayrollStatus.Paid;
    await context.SaveChangesAsync();
    
    // NEW: Auto-create expense transaction
    await transactionService.SyncPayrollAsExpenseAsync(id);
}
```

---

### Марҳилаи 5: Controllers (1 рӯз)

#### TransactionController
```csharp
[Route("api/[controller]")]
public class TransactionController
{
    [HttpPost] CreateTransaction
    [HttpPut("{id}")] UpdateTransaction
    [HttpDelete("{id}")] DeleteTransaction
    [HttpGet] GetTransactions (paginated)
    [HttpGet("{id}")] GetById
    
    [HttpGet("summary")] GetSummary
    [HttpGet("category-breakdown")] GetCategoryBreakdown
    [HttpGet("daily-trend")] GetDailyTrend
}
```

#### TransactionCategoryController
```csharp
[Route("api/[controller]")]
public class TransactionCategoryController
{
    [HttpPost] Create
    [HttpPut("{id}")] Update
    [HttpDelete("{id}")] Delete
    [HttpGet] GetAll
}
```

#### FinancialPeriodController
```csharp
[Route("api/financial-period")]
public class FinancialPeriodController
{
    [HttpPost("close")] CloseMonth
    [HttpGet("is-closed")] IsMonthClosed
    [HttpGet("{month}/{year}")] GetPeriod
}
```

---

### Марҳилаи 6: Default Categories (30 мин)

Дар `SeedData.cs` ё `TransactionCategoryService`:

#### Income Categories (Даромадҳо)
- `"Пардохтҳои донишҷӯён"` - IsSystem = true
- `"Грантҳо"`
- `"Кумакҳои молиявӣ"`
- `"Хизматҳои иловагӣ"`
- `"Дигар даромадҳо"`

#### Expense Categories (Харочотҳо)
- `"Маошҳо"` - IsSystem = true
- `"Кироя"`
- `"Коммуналка"`
- `"Таҷҳизот"`
- `"Маводҳои таълимӣ"`
- `"Маркетинг"`
- `"Таъмирот"`
- `"Транспорт"`
- `"Дигар харочотҳо"`

---

### Марҳилаи 7: Migration from Old System (2 рӯз)

#### Нест кардани сервисҳои қадимӣ
- [ ] `FinanceService.cs` → Delete
- [ ] `ExpenseService.cs` → Delete
- [ ] `IFinanceService.cs` → Delete
- [ ] `IExpenseService.cs` → Delete

#### Мувофиқсозии контроллерҳо
- [ ] `FinanceController.cs` → Update to use new TransactionService
- [ ] `ExpenseController.cs` → Delete or redirect

#### Тағйироти Register.cs
- [ ] Remove old service registrations
- [ ] Add new service registrations

#### Data Migration Script (агар маълумоти кӯҳна бошад)
- [ ] Мигратсияи `Expense` → `Transaction`
- [ ] Категорияҳоро созед

---

### Марҳилаи 8: Messages (30 мин)

Дар `Infrastructure/Constants/Messages.cs`:
```csharp
public static class Transaction
{
    public const string Created = "Сабт бомуваффақият эҷод шуд";
    public const string Updated = "Сабт навсозӣ шуд";
    public const string Deleted = "Сабт нест карда шуд";
    public const string NotFound = "Сабт ёфт нашуд";
    
    public const string TypeIncome = "Даромад";
    public const string TypeExpense = "Харочот";
    
    public const string MonthClosed = "Моҳ пӯшида шуд";
    public const string MonthAlreadyClosed = "Моҳ аллакай пӯшида шудааст";
    public const string CannotEditClosedMonth = "Моҳи пӯшидашударо тағйир додан мумкин нест";
}

public static class TransactionCategory
{
    public const string Created = "Категория эҷод шуд";
    public const string CannotDeleteSystem = "Категорияи системавиро нест кардан мумкин нест";
    public const string CannotDeleteWithTransactions = "Категорияи дорои сабтҳоро нест кардан мумкин нест";
}
```

---

### Марҳилаи 9: Testing (2 рӯз)

#### Manual Testing
- [ ] Эҷоди Transaction (Income/Expense)
- [ ] Санҷиши автоматии синк бо Payment
- [ ] Санҷиши автоматии синк бо Payroll
- [ ] Гирифтани Summary
- [ ] Category Breakdown
- [ ] Month Close

#### Integration Testing
- [ ] Пардохти донишҷӯ → Income автоматӣ эҷод мешавад?
- [ ] Payroll Paid → Expense автоматӣ эҷод мешавад?

---

### Марҳилаи 10: Documentation (1 рӯз)

- [ ] API Documentation (Swagger)
- [ ] README барои Frontend
- [ ] Мисолҳои Postman

---

## 🎨 UI/UX Recommendations

### Dashboard
```
┌─────────────────────────────────────┐
│ 💰 Молиявии январ 2025              │
├──────────────┬──────────────────────┤
│ Даромад      │ 250,000с    ↗️ +15% │
│ Харочот      │ 180,000с    ↗️ +5%  │
│ Фоида        │  70,000с    ↗️ +50% │
└──────────────┴──────────────────────┘

📊 Харочотҳо аз рӯи категория:
[Pie Chart: Маош 44%, Кироя 17%, ...]

📈 Тенденсия (тренд):
[Line Chart: даромад/харочот 6 моҳи охир]
```

---

## ⚠️ Риски ва Тавзеҳот

### 1. Data Migration
Агар `Expense` entities мавҷуд бошанд, бояд миграт карда шаванд.

### 2. Receipt Service
`ReceiptService` нигоҳ дошта шавад, аммо дар `TransactionService` интегратсия карда шавад.

### 3. Month Closing
Вақте моҳ пӯшида шуд, ҳеҷ гуна тағйирот барои он моҳ иҷозат дода намешавад.

---

## 📊 Timeline

| Марҳила | Вақт | Приоритет |
|---------|------|-----------|
| 1. Entities & DB | 2-3 рӯз | 🔴 Баланд |
| 2. DTOs | 1 рӯз | 🔴 Баланд |
| 3. Services | 3-4 рӯз | 🔴 Баланд |
| 4. Integration | 2 рӯз | 🟡 Миёна |
| 5. Controllers | 1 рӯз | 🟡 Миёна |
| 6. Categories | 30 мин | 🟢 Паст |
| 7. Migration | 2 рӯз | 🔴 Баланд |
| 8. Messages | 30 мин | 🟢 Паст |
| 9. Testing | 2 рӯз | 🔴 Баланд |
| 10. Documentation | 1 рӯз | 🟢 Паст |

**Умумӣ:** 12-15 рӯзи корӣ

---

## ✅ Критерияҳои қабул

- [ ] Ҳамаи даромадҳо ва харочотҳо дар Transaction ҷамъ шудаанд
- [ ] Manual entry кор мекунад
- [ ] Auto-sync аз Payment кор мекунад
- [ ] Auto-sync аз Payroll кор мекунад
- [ ] Analytics ҳисоботҳо дуруст ҳастанд
- [ ] Month Close functionality кор мекунад
- [ ] Сервисҳои қадимӣ нест карда шуданд
- [ ] Frontend метавонад маълумотро гирад

---

**Тайёрият:** Зиндагии амалӣ барои эҷод! 🚀
