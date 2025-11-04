## Finance API Reference (CRM Kavsar)

Ин ҳуҷҷат истифодаи эндпоинтҳо ва шарҳи майдонҳо (fields) барои модулҳои молиявиро тавсиф мекунад.

### Аутентификсия ва нақшҳо
- Ҳамаи эндпоинтҳо бо JWT ҳифз шудаанд.
- Нақшҳои иҷозатдодашуда дар поён барои ҳар endpoint нишон дода мешаванд.

---

## Payments

### POST /api/payments
- **Roles**: Admin, SuperAdmin, Manager
- **Purpose**: Эҷоди пардохти донишҷӯ (бо ҳисобкунии тахфиф аз сервиси Discount).
- **Body (CreatePaymentDto)**:
  - `studentId` (int, required): ID донишҷӯ.
  - `groupId` (int, required): ID гурӯҳ.
  - `month` (int, 1–12, required): Моҳ.
  - `year` (int, required): Сол.
  - `monthsCount` (int?, optional, 1–12): пардохти пайдарпай барои чанд моҳ. Агар >1 бошад, система барои ҳар моҳ пардохти алоҳида месозад.
  - `paymentMethod` (enum PaymentMethod, required): Тарзи пардохт (Cash, Card, Transfer, Other...).
  - `amount` (decimal?, optional): барои partial payment. Барои `monthsCount>1` ҳоло иҷозат дода намешавад (маблағи пурра барои ҳар моҳ истифода мешавад).
  - `transactionId` (string, optional): ID транзаксия барои ғайринақдӣ.
  - `description` (string, optional): Тавзеҳ/эзоҳ.
  - `status` (enum PaymentStatus, default=Completed): Ҳолати пардохт (Pending, Completed, Paid...).
- **Response (GetPaymentDto)**: пардохти сохта бо рақами квитансия.

### GET /api/payments/{id}
- **Roles**: Admin, SuperAdmin, Manager
- **Purpose**: Гирифтани пардохт бо ID.
- **Response (GetPaymentDto)**: тафсилоти пардохт.

### GET /api/payments/{id}/receipt
- **Roles**: Admin, SuperAdmin, Manager
- **Purpose**: Бозгардонидани маълумоти квитансия (placeholder барои HTML/PDF).
- **Response (JSON)**:
  - `paymentId` (int)
  - `receiptNumber` (string) — рақами квитансия
  - `downloadUrl` (string) — роҳи боргирӣ (placeholder)

---

## Expenses

### POST /api/expenses
- **Roles**: Manager, SuperAdmin
- **Purpose**: Эҷоди хароҷот.
- **Body (CreateExpenseDto)**: майдонҳои зарурӣ барои хароҷот (марказ, сана, маблағ, категория...).
- **Response (GetExpenseDto)**: объект бо ID ва майдонҳои калидӣ.

### PUT /api/expenses/{id}
- **Roles**: Manager, SuperAdmin
- **Purpose**: Тағйири хароҷот.

### DELETE /api/expenses/{id}
- **Roles**: Manager, SuperAdmin
- **Purpose**: Нест кардани мулоим (soft-delete) хароҷот.

### GET /api/expenses/{id}
- **Roles**: Manager, SuperAdmin
- **Purpose**: Гирифтани хароҷот бо ID.

### GET /api/expenses
- **Roles**: Manager, SuperAdmin
- **Purpose**: Рӯйхати хароҷот бо фильтр (ExpenseFilter).

---

## Finance Summaries ва Payroll

### GET /api/finance/centers/{centerId}/summary?start=&end=
- **Roles**: Manager, SuperAdmin
- **Purpose**: Ҷамъбасти молиявии марказ дар давра (даромад/хароҷот/трендҳо/категорияҳо).

### GET /api/finance/centers/{centerId}/daily?date=
- **Roles**: Manager, SuperAdmin
- **Purpose**: Ҷамъбасти рӯзона барои марказ.

### GET /api/finance/centers/{centerId}/monthly?year=&month=
- **Roles**: Manager, SuperAdmin
- **Purpose**: Ҷамъбасти моҳона барои марказ.

### GET /api/finance/centers/{centerId}/yearly?year=
- **Roles**: Manager, SuperAdmin
- **Purpose**: Ҷамъбасти солона барои марказ.

### POST /api/finance/centers/{centerId}/payroll?year=&month=
- **Roles**: Manager, SuperAdmin
- **Purpose**: Генератсияи хароҷоти маош барои менторҳо дар моҳ/сол (категория Salary).
- **Response**: шумораи қайдҳои эҷодшуда.

### GET /api/finance/centers/{centerId}/debts?year=&month=&studentId=
- **Roles**: Manager, SuperAdmin
- **Purpose**: Рӯйхати қарздорон барои моҳ/сол (бо ҳисобкунии тахфиф ва маблағи пардохташуда).
- **Response (List<DebtDto>)**: студент, гурӯҳ, original/discount/paid/balance, моҳ/сол.

---

## Шарҳи Майдонҳо (Fields)

### Entity: Payment
- `id` (int): Идентификатор.
- `studentId` (int): Донишҷӯ.
- `groupId` (int?): Гурӯҳ.
- `receiptNumber` (string?): Рақами квитансия, автоматикунонидашуда ҳангоми эҷод.
- `originalAmount` (decimal): Нарх пеш аз тахфиф.
- `discountAmount` (decimal): Ҳаҷми тахфиф.
- `amount` (decimal): Маблағи воқеан пардохтшаванда.
- `paymentMethod` (PaymentMethod): Тарзи пардохт (enum ба string).
- `transactionId` (string?): ID транзаксияи ғайринақдӣ.
- `description` (string?): Эзоҳ.
- `status` (PaymentStatus): Ҳолати пардохт.
- `paymentDate` (DateTime): Сана/соати пардохт.
- `centerId` (int?): Марказ.
- `month` (int): Моҳ.
- `year` (int): Сол.
- `createdAt`, `updatedAt` (DateTimeOffset): Мӯҳрҳои вақт.

### DTO: GetPaymentDto
- Ҳамон майдонҳои калидӣ аз `Payment` барои баргардонидани ба фронтенд, аз ҷумла `receiptNumber`.

### DTO: CreatePaymentDto
- `studentId`, `groupId`, `month`, `year`, `paymentMethod`, `transactionId?`, `description?`, `status`.

### Entity: Expense
- `id` (int)
- `centerId` (int)
- `amount` (decimal)
- `expenseDate` (DateTimeOffset)
- `category` (ExpenseCategory)
- `paymentMethod` (PaymentMethod)
- `description` (string?)
- `mentorId` (int?)
- `month` (int)
- `year` (int)

### DTO: DebtDto (қисми Debts API)
- `studentId` (int), `studentName` (string?)
- `groupId` (int), `groupName` (string?)
- `originalAmount` (decimal), `discountAmount` (decimal)
- `paidAmount` (decimal), `balance` (decimal)
- `month` (int), `year` (int)

### Entity: MonthlyFinancialSummary
- `centerId` (int), `month` (int), `year` (int)
- `totalIncome` (decimal), `totalExpense` (decimal), `netProfit` (decimal)
- `generatedDate` (DateTimeOffset), `isClosed` (bool)

---

## Рамзи генератори рақам (ReceiptNumber)
- Формат: `CTR-{CenterId}-{YYYY}{MM}-{Seq6}`.
- Рақам дар `PaymentService` ҳангоми `Create` тавассути `DocumentNumberGenerator.GenerateReceiptNumberAsync` тавлид мешавад.

---

## Эзоҳҳо
- Ба даромад танҳо пардохтҳои `Completed|Paid` дохил мешаванд.
- Хароҷот танҳо вақте ҳисоб мешавад, ки `!IsDeleted` бошад.
- Debts барои моҳ/сол аз `Course.Price` минус тахфиф ва минус суммаи пардохтҳои воқеӣ ҳисоб мешавад.


