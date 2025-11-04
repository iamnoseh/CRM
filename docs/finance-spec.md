## ТЗ: Модулҳои молиявӣ (CRM Kavsar)

### 1) Ҳадаф ва доираи кор
- **Ҳадаф**: Сохтани идоракунии молия бо истифода аз моделҳои мавҷуда, ислоҳи хатоҳо, поксозии нозарурӣ ва иловаи фичаҳое, ки кори ҳаррӯзаро автоматӣ ва шаффоф мекунанд.
- **Доира**:
  - Пардохтҳо (Payments), Хароҷот (Expenses), Ҷамъбастҳо (Finance summaries), Payroll-и менторон.
  - Ҳисоботи рӯзона/моҳона/солона, қарздорон ва квитансия/инвойс.
  - Баста кардани моҳ (optional-lite), кэш/агрегация тавассути Hangfire.

### 2) Вазъи ҷорӣ (Inventory)
- API мавҷуд:
  - `POST /api/payments`, `GET /api/payments/{id}`
  - `GET /api/finance/centers/{centerId}/summary|daily|monthly|yearly`
  - `POST /api/finance/centers/{centerId}/payroll?year&month`
  - `POST/PUT/DELETE/GET /api/expenses`
- Моделҳо мавҷуд: `Payment`, `Expense`, `MonthlyFinancialSummary`.
- Hangfire service: `MonthlyFinanceAggregatorService` (пойдагӣ ҳаст, иҷро холист).

### 3) Муайян кардани камбудҳо
- Қарздорӣ/бақия барои донишҷӯ муайян нест (ниёз ба ҳисоб/эндпоинт).
- Номератсияи ҳуҷҷатҳо (receipt/expense number) нест.
- Refund/Reversal пардохтҳо нест.
- Агрегатсияи моҳона тавассути Hangfire татбиқи амалӣ надорад.
- Секретҳо дар `appsettings.json` — бояд ба муҳит/secret store кӯчонда шаванд.

### 4) Фичаҳое, ки бояд илова/мустаҳкам шаванд (Фаза 1)
1. Қарздорон ва бақия:
   - Endpoint: `GET /api/finance/centers/{centerId}/debts?year=&month=&studentId=`
   - Баргардонад рӯйхати донишҷӯён бо `OriginalAmount, DiscountAmount, PaidAmount, Balance`.
2. Квитансия/инвойс:
   - Endpoint: `GET /api/payments/{id}/receipt` (HTML/PDF URL ё файл), `POST /api/payments/invoice` (optional).
3. Рақамгузории ҳуҷҷатҳо:
   - Генератори рақам барои `Payment.ReceiptNumber` ва `Expense.ExpenseNumber` — формати `CTR-{CenterId}-{YYYY}{MM}-{Seq}`.
4. Hangfire агрегацияи моҳона:
   - Пур кардани `MonthlyFinancialSummary` барои ҳар марказ дар охири моҳ ва кэш кардани Summary.
5. Амният/Center scope:
   - Тасдиқи `CenterId` аз контексти корбар дар ҳама эндпоинтҳои молиявӣ.

### 5) Тағйири моделҳо (минималӣ, мутобиқ бо мавҷуд)
- `Payment` — илова: `ReceiptNumber (string?)`, `DueDate (DateTimeOffset?)`, `PaidPartially (bool) default=false`.
- `Expense` — илова: `ExpenseNumber (string?)`, `Vendor (string?)`, `AttachmentUrl (string?)` (optional барои ҳуҷҷатҳо).
- Мигратсияҳо: эҷоди migration EF Core бо индекси `ReceiptNumber`, `ExpenseNumber` (Unique per center + month optional).

### 6) API Контрактҳо (Request/Response)
- Debts
  - `GET /api/finance/centers/{centerId}/debts?year=2025&month=11&studentId=`
  - 200 OK:
    ```json
    {
      "statusCode": 200,
      "data": [
        {
          "studentId": 123,
          "studentName": "...",
          "groupId": 45,
          "originalAmount": 600.00,
          "discountAmount": 100.00,
          "paidAmount": 400.00,
          "balance": 100.00,
          "month": 11,
          "year": 2025
        }
      ]
    }
    ```
- Receipt
  - `GET /api/payments/{id}/receipt`
  - 200 OK: HTML ё `application/pdf` (URL ба файли тавлидшуда), ҳамчунин JSON бо metadata:
    ```json
    {
      "statusCode": 200,
      "data": {
        "paymentId": 1,
        "receiptNumber": "CTR-1-202511-000123",
        "downloadUrl": "/files/receipts/CTR-1-202511-000123.pdf"
      }
    }
    ```

### 7) Ислоҳҳо ва поксозӣ
- Таъмини истифодаи `UserContextHelper.GetCurrentUserCenterId` барои маҳдудияти марказ дар ҳамаи queries молиявӣ.
- Бартараф кардани майдонҳо/роҳҳои нозарур дар DTO-ҳо (агар такрорӣ ё бе истифода бошанд).
- Сериякунонии enum-ҳо аллакай тоза шудааст (маъқул). Миқдори decimal бо precision танзим шудааст — ҳифз шавад.

### 8) Hangfire: Рӯйхати jobs
- `MonthlyFinanceAggregatorService.RunAsync`:
  - Барои ҳар `Center.Id` ҳисоб кардани Income/Expense моҳона ва пур кардани `MonthlyFinancialSummary` (upsert).
  - Кэш кардани натиҷаҳо барои `FinanceService` (optional Redis: key `fin:sum:{center}:{year}:{month}`).
- Job-и ҳаршаба:
  - Эълонҳои қарздорон (optional) тавассути SMS/Telegram дар асоси `DueDate`.

### 9) Қоидаҳои бизнес
- Пардохтҳои `Status` танҳо ба даромад меафтад агар `Completed|Paid`.
- Хароҷот танҳо агар `!IsDeleted`.
- `DueDate` барои ҳисоб кардани "қарз/мӯҳлат гузашта".
- Бақия = `OriginalAmount - DiscountAmount - Σ(пардохтҳои воқеан пардохтшуда)`; барои моҳ/гурӯҳ.

### 10) Acceptance Criteria (Фаза 1)
1. `GET debts` рӯйхати дуруст бо pagination (optional) бармегардонад; филтрҳо `centerId, month, year, studentId`.
2. Эҷоди пардохт `POST /api/payments` рақами квитансияро насб мекунад (auto numbering) ва дар `GET /receipt` намоён аст.
3. `FinanceService` summary бо ҳамон формулаҳои ҷорӣ кор мекунад; натиҷа барои рӯз/моҳ/сол дақиқ аст.
4. Hangfire моҳона `MonthlyFinancialSummary`-ро пур мекунад; ҳангоми нокомӣ логҳо мавҷуданд.
5. Дастрасии нақшҳо: `Manager` танҳо ба маркази худ; `SuperAdmin` ба ҳама.
6. Секретҳо аз `appsettings.json` берун карда шудаанд (env/secret store) — production policy.

### 11) Тағйироти маводи фронтенд (Guidelines)
- Экранҳо:
  - Дашборди молиявӣ (Income, Expense, Net, трендҳо, қарздорон).
  - Пардохтҳо: ҷадвали рӯзона, ҷустуҷӯ, Receipt Preview/Download.
  - Хароҷот: CRUD, категория/таъминкунанда/иловаи ҳуҷҷат.
  - Отчетҳо: моҳона/солона, категорияҳо, қарздорон.
- Қисмҳои API-ро истифода баред бо headers-и auth; санҷиши нақш.

### 12) Мигратсияҳо ва деплой
- EF Core migration барои майдонҳои нав (`ReceiptNumber`, `DueDate`, `ExpenseNumber`, `Vendor`, `AttachmentUrl`).
- Индексҳо: `ReceiptNumber`, `ExpenseNumber`, инчунин composite барои `CenterId+Year+Month` (агар талаб шавад).
- CI/CD: татбиқи мигратсияҳо бо `ApplyMigrationsOnStartup=true` дар staging, дар prod — мигратсияҳо ҷудо.

### 13) Қадамҳои иҷро (Roadmap кӯтоҳ)
- S1: Debts endpoint, авто-номер, receipt export (HTML/pdf placeholder), ислоҳи Hangfire RunAsync.
- S2: Refund/Reversal (optional), бастани моҳ (IsClosed) — lite.
- S3: Оптимизатсия ва кэш, риёлкатсия (optional), огоҳсозиҳо.

### 14) Масоили амният
- Бартараф кардани калидҳо/паролҳо аз `appsettings.json` ба env/secret store.
- Сервери Swagger бо ҳимоя (қайд шудааст), нигоҳ дошта шавад.

### 15) Эзоҳҳои имплементатсионӣ (Backend)
- Номератсия: сервис ё helper `DocumentNumberGenerator` (per center, per month), бо қулф/транзаксия.
- Receipt: HTML template + сервиси export; файл дар `wwwroot/receipts` нигаҳдорӣ шавад ва URL дода шавад.
- Debts: query бо join-и `Payments`, `StudentGroups`, `Courses.Price`, `Discounts` барои ҳисобкунии динамикӣ.

### 16) Талаботи логинг ва аудит
- Логҳои муҳими молиявӣ (эҷод/тағйир/ҳазф) бо Serilog.
- Audit trail (optional): навъи сабт бо пеш/пас арзишҳо барои пардохт/хароҷот.

— Ин ҳуҷҷат барои ҳамоҳангии фронтенд/бекенд мебошад; ҳама API/моделҳо дар ҳамин доира татбиқ ва ҳамоҳанг карда мешаванд.


