using System.Net;

namespace Infrastructure.Constants;

public static class Messages
{
    public static class Common
    {
        public const string Success = "Операция выполнена успешно";
        public const string InternalError = "Внутренняя ошибка сервера";
        public const string InvalidRequest = "Некорректный запрос";
        public const string NotFound = "Не найдено";
        public const string Unauthorized = "Пользователь не авторизован";
        public const string AccessDenied = "Доступ запрещен";
        public const string AlreadyExists = "Уже существует";
        public const string InvalidData = "Недопустимые данные";
        public const string Unknown = "Неизвестно";
    }

    public static class User
    {
        public const string NotFound = "Пользователь не найден";
        public const string Created = "Пользователь успешно создан";
        public const string Updated = "Пользователь успешно обновлен";
        public const string Deleted = "Пользователь успешно удален";
        public const string SearchTermRequired = "Требуется поисковый запрос";
        public const string NoUsersFound = "Пользователи не найдены по заданным критериям";
        public const string RoleRequired = "Требуется указать роль";
        public const string RoleNotFound = "Роль '{0}' не найдена";
        public const string NoUsersWithRole = "Не найдено пользователей с ролью '{0}'";
        public const string UserNotAuthenticated = "Пользователь не аутентифицирован";
        public const string UserIdNotFoundInToken = "UserId не найден в токене";
        public const string ProfileImageUpdated = "Фото профиля успешно обновлено";
        public const string ProfileImageUpdateError = "Ошибка при обновлении фото профиля: {0}";
        public const string EmailAlreadyInUse = "Этот email уже используется";
        public const string EmailUpdated = "Email успешно обновлен";
        public const string EmailUpdateError = "Ошибка при обновлении email: {0}";
    }

    public static class Student
    {
        public const string NotFound = "Студент не найден";
        public const string Created = "Студент успешно создан";
        public const string Updated = "Студент успешно обновлен";
        public const string Deleted = "Студент успешно удален";
        public const string CreationFailed = "Не удалось создать студента";
        public const string UpdateFailed = "Не удалось обновить студента";
        public const string DeleteFailed = "Не удалось удалить студента";
        public const string AccessDenied = "Доступ запрещен или студент не найден";
        public const string NoStudentsFound = "Студенты не найдены";
        public const string DocumentRequired = "Требуется документ";
        public const string DocumentUpdated = "Документ студента успешно обновлен";
        public const string DocumentUpdateFailed = "Не удалось обновить документ студента";
        public const string ProfileImageRequired = "Требуется фото профиля";
        public const string ProfileImageUpdated = "Фото профиля успешно обновлено";
        public const string ProfileImageUpdateFailed = "Не удалось обновить фото профиля";
        public const string PaymentStatusUpdated = "Статус оплаты успешно обновлен";
        public const string PaymentStatusUpdateFailed = "Не удалось обновить статус оплаты";
        public const string WalletCreated = "Студент создан успешно. Кошелек создан и код отправлен по SMS.";
    }

    public static class StudentGroup
    {
        public const string StudentAlreadyAssigned = "Студент уже назначен в эту группу";
        public const string Reactivated = "Членство студента успешно реактивировано";
        public const string ReactivateFailed = "Не удалось реактивировать членство студента";
        public const string Created = "Студент успешно добавлен в группу";
        public const string CreateFailed = "Не удалось добавить студента в группу";
        public const string Updated = "Членство студента в группе успешно обновлено";
        public const string UpdateFailed = "Не удалось обновить членство студента в группе";
        public const string MembershipNotFound = "Членство студента в группе не найдено";
        public const string Deleted = "Студент успешно удален из группы";
        public const string DeleteFailed = "Не удалось удалить студента из группы";
        public const string NoMembershipsFound = "Членства студентов в группах не найдены";
        public const string StudentNotInGroups = "Студент не назначен ни в одну группу";
        public const string StudentsNotProvided = "Список студентов не указан";
        public const string SourceAndTargetSame = "Группа-источник и группа-назначение не могут совпадать";
    }

    public static class Group
    {
        public const string NotFound = "Группа не найдена";
        public const string Created = "Группа успешно создана";
        public const string Updated = "Группа успешно обновлена";
        public const string Deleted = "Группа успешно удалена";
        public const string CreationError = "Ошибка при создании группы: {0}";
        public const string UpdateError = "Ошибка при обновлении группы: {0}";
        public const string DeleteError = "Ошибка при удалении группы: {0}";
        public const string GetError = "Ошибка при получении группы: {0}";
        public const string GetListError = "Ошибка при получении списка групп: {0}";
        public const string CenterIdNotFound = "CenterId не найден в токене";
        public const string MentorNotFound = "Преподаватель не найден или не принадлежит этому учебному центру";
        public const string CourseNotFound = "Курс не найден или не принадлежит этому учебному центру";
        public const string ClassroomNotFound = "Аудитория не найдена, неактивна или не принадлежит этому учебному центру";
        public const string AlreadyExists = "Группа с таким названием уже существует";
        public const string CannotDeleteWithStudents = "Невозможно удалить группу, так как в ней есть активные студенты";
        public const string GetSimpleListError = "Ошибка при получении простого списка групп: {0}";
        public const string GetByStudentIdError = "Ошибка при получении групп по studentId: {0}";
        public const string GetByMentorIdError = "Ошибка при получении групп по mentorId: {0}";
        public const string NoGroupsFoundForCourse = "Группы для этого курса не найдены";
    }

    public static class Center
    {
        public const string NotFound = "Учебный центр не найден";
        public const string Created = "Учебный центр успешно создан";
        public const string Updated = "Учебный центр успешно обновлен";
        public const string Deleted = "Учебный центр успешно удален";
        public const string AlreadyExists = "Учебный центр с таким названием уже существует";
        public const string CreationError = "Ошибка при создании учебного центра: {0}";
        public const string UpdateError = "Ошибка при обновлении учебного центра: {0}";
        public const string DeleteError = "Ошибка при удалении учебного центра: {0}";
        public const string CannotDeleteWithDependencies = "Невозможно удалить центр с активными студентами, преподавателями или курсами";
        public const string SuperAdminOnly = "Только SuperAdmin может рассчитать доход всех центров";
        public const string IncomeUpdated = "Доход центра обновлен: Месяц: {0}, Год: {1}";
        public const string AllIncomesUpdated = "Успешно обновлен доход для всех {0} центров";
        public const string IncomeUpdatePartial = "Обновлено {0} центров. Ошибки: {1}";
        public const string IncomeUpdateErrorFormat = "Центр {0}: {1}";
    }

    public static class Course
    {
        public const string NotFound = "Курс не найден";
        public const string Created = "Курс успешно создан";
        public const string Updated = "Курс успешно обновлен";
        public const string Deleted = "Курс успешно удален";
        public const string AlreadyExists = "Курс с таким названием уже существует";
        public const string CreationError = "Ошибка при создании курса: {0}";
        public const string UpdateError = "Ошибка при обновлении курса: {0}";
        public const string DeleteError = "Ошибка при удалении курса: {0}";
    }

    public static class Mentor
    {
        public const string NotFound = "Преподаватель не найден";
        public const string Created = "Преподаватель успешно создан";
        public const string Updated = "Преподаватель успешно обновлен";
        public const string Deleted = "Преподаватель успешно удален";
        public const string CreationError = "Ошибка при создании преподавателя: {0}";
        public const string UpdateError = "Ошибка при обновлении преподавателя: {0}";
        public const string DeleteError = "Ошибка при удалении преподавателя: {0}";
        public const string DocumentUpdated = "Документ преподавателя успешно обновлен";
        public const string DocumentUpdateFailed = "Не удалось обновить документ преподавателя";
        public const string NoMentorsFoundForGroup = "Преподаватели для этой группы не найдены";
        public const string NoMentorsFoundForCourse = "Преподаватели для этого курса не найдены";
        public const string ProfileImageUpdateFailed = "Не удалось обновить фото профиля";
        public const string PaymentStatusAlreadySet = "Статус оплаты уже установлен";
        public const string PaymentStatusUpdated = "Статус оплаты успешно обновлен";
        public const string PaymentStatusUpdateFailed = "Не удалось обновить статус оплаты";
        public const string NoMentorsFound = "Преподаватели не найдены";
    }

    public static class Employee
    {
        public const string NotFound = "Сотрудник не найден";
        public const string Created = "Сотрудник успешно создан";
        public const string Updated = "Сотрудник успешно обновлен";
        public const string Deleted = "Сотрудник успешно удален";
        public const string CreationError = "Ошибка при создании сотрудника: {0}";
        public const string UpdateError = "Ошибка при обновлении сотрудника: {0}";
        public const string DeleteError = "Ошибка при удалении сотрудника: {0}";
        public const string GetError = "Ошибка при получении сотрудника: {0}";
        public const string GetListError = "Ошибка при получении списка сотрудников: {0}";
        public const string CreatedWithAuth = "Сотрудник успешно добавлен. Данные для входа отправлены на email и/или SMS. Username: {0}";
    }

    public static class Finance
    {
        public const string InsufficientBalance = "Недостаточно средств на счете";
        public const string PaymentSuccess = "Оплата успешно проведена";
        public const string PaymentFailed = "Ошибка при проведении оплаты";
        public const string TopUpSuccess = "Счет успешно пополнен";
        public const string WithdrawSuccess = "Средства успешно списаны";
        public const string AccountNotFound = "Счет не найден";
        public const string TransactionFailed = "Транзакция не выполнена";
        public const string InvalidAmount = "Некорректная сумма";
        public const string SummaryCalculationFailed = "Не удалось рассчитать сводку";
        public const string MonthlySummaryFailed = "Не удалось рассчитать месячную сводку";
        public const string YearlySummaryFailed = "Не удалось рассчитать годовую сводку";
        public const string CategoryBreakdownFailed = "Не удалось рассчитать разбивку по категориям";
        public const string PayrollGenerationFailed = "Не удалось сформировать начисление зарплат";
        public const string DebtCalculationFailed = "Не удалось рассчитать задолженности";
        public const string OperationFailed = "Операция не выполнена";
        
        // Log messages
        public const string LogSummaryError = "Ошибка при расчёте финансового отчёта для центра {0}";
        public const string LogMonthlySummaryError = "Ошибка при расчёте месячной сводки {0}-{1} для центра {2}";
        public const string LogYearlySummaryError = "Ошибка при расчёте годовой сводки {0} для центра {1}";
        public const string LogCategoryBreakdownError = "Ошибка при расчёте разбивки по категориям для центра {0}";
        public const string LogPayrollGenerationSuccess = "Пользователь {0} сформировал начисление зарплат для центра {1} {2}-{3}: {4} записей";
        public const string LogPayrollGenerationError = "Ошибка при формировании начисления зарплат для центра {0} {1}-{2}";
        public const string LogDebtCalculationError = "Ошибка при расчёте задолженностей для центра {0} {1}-{2}";
        public const string LogMonthCloseError = "Ошибка при закрытии/открытии месяца {0}-{1} для центра {2}";
    }

    public static class Discount
    {
        public const string NotFound = "Скидка не найдена";
        public const string Created = "Скидка успешно создана";
        public const string Updated = "Скидка успешно обновлена";
        public const string Deleted = "Скидка успешно удалена";
        public const string StudentNotMember = "Студент не является членом этой группы";
        
        // Log messages
        public const string LogAssigned = "Скидка назначена | DiscountId={0} StudentId={1} GroupId={2} Discount={3}";
        public const string LogUpdated = "Скидка обновлена | Id={0} Discount={1}";
        public const string LogDeleted = "Скидка удалена | Id={0}";
        public const string LogFound = "Скидка найдена | Id={0} StudentId={1} GroupId={2}";
        public const string LogList = "Список скидок получен | StudentId={0} GroupId={1} Count={2}";
        public const string LogPreview = "Preview рассчитан | StudentId={0} GroupId={1} Original={2} Discount={3} Net={4}";
        public const string LogRecalculateError = "RecalculateStudentPaymentStatusAsync failed in DiscountService for StudentId={0}";
        public const string LogAssignRequest = "Запрос: назначить скидку | StudentId={0} GroupId={1} Discount={2}";
        public const string LogInvalidAmount = "Сумма скидки некорректна (отрицательная) | Discount={0}";
        public const string LogNotMember = "Студент не является членом этой группы | StudentId={0} GroupId={1}";
        public const string LogInternalError = "Внутренняя ошибка при назначении скидки | StudentId={0} GroupId={1}";
        public const string LogUpdateError = "Внутренняя ошибка при обновлении скидки | Id={0}";
        public const string LogNotFound = "Скидка не найдена | Id={0}";
        public const string LogGroupNotFound = "Группа не найдена | GroupId={0}";
    }

    public static class Journal
    {
        public const string NotFound = "Журнал не найден";
        public const string EntryNotFound = "Запись журнала не найдена";
        public const string Created = "Запись успешно создана";
        public const string Updated = "Запись успешно обновлена";
        public const string Deleted = "Запись успешно удалена";
        public const string AccessDenied = "Доступ к журналу запрещен";
        public const string GroupNotFound = "Группа не найдена";
        public const string WeekAlreadyExists = "Журнал для этой недели уже существует";
        public const string WeekSequenceError = "Нарушена последовательность. Создайте сначала неделю {0}";
        public const string WeekExceedsTotal = "Невозможно создать неделю {0}. Всего недель: {1}. Следующая: {2}";
        public const string WeekCreated = "Еженедельный журнал успешно создан";
        public const string WeekCreatedFromDate = "Журнал первой недели от {0} успешно создан";
        public const string Week1AlreadyExists = "Журнал первой недели уже существует";
        public const string NoJournalsFound = "Журналы не найдены";
        public const string JournalNotFoundForDate = "Журнал для этой даты не найден";
        public const string StudentNotActive = "Студент не активен в группе";
        public const string NoSlotsForWeek = "Для этой недели нет слотов";
        public const string NoNewEntriesNeeded = "Новые записи для студента не требуются";
        public const string EntriesCreated = "Создано {0} записей для студента";
        public const string EntriesCreatedMultiple = "Создано {0} записей для студентов";
        public const string NoActiveStudents = "Нет активных студентов для backfill";
        public const string NoStudentsSpecified = "Студенты не указаны";
        public const string NoFutureWeeks = "Будущих недель нет — удалять нечего";
        public const string NoFutureEntries = "Будущих записей для студента нет";
        public const string FutureEntriesDeleted = "Удалено будущих записей: {0}";
        public const string CurrentWeekNotFound = "Журнал текущей недели для этой группы не найден";
        public const string WeekDeleted = "Журнал недели {0} успешно удален";
        public const string WeekDeleteFailed = "Не удалось удалить журнал";
        public const string AllDeleted = "Все журналы группы ({0} шт.) успешно удалены";
        public const string AllDeleteFailed = "Не удалось удалить журналы";
        public const string NoJournalsToDelete = "Журналов для удаления не найдено";
        
        public static class Days
        {
            public const string Monday = "Понедельник";
            public const string Tuesday = "Вторник";
            public const string Wednesday = "Среда";
            public const string Thursday = "Четверг";
            public const string Friday = "Пятница";
            public const string Saturday = "Суббота";
            public const string Sunday = "Воскресенье";
        }

        public static class Comments
        {
            public const string General = "Общий";
            public const string Positive = "Положительный";
            public const string Warning = "Предупреждение";
            public const string Behavior = "Поведение";
            public const string Homework = "Домашнее задание";
            public const string Participation = "Участие";
        }
    }

    public static class StudentAccount
    {
        public const string NotFound = "Счет студента не найден";
        public const string AccountNotCreated = "Счет пока не создан";
        public const string AccountNotActive = "Счет не найден или неактивен";
        public const string AmountMustBePositive = "Сумма должна быть больше нуля";
        public const string TopUpSuccess = "Баланс успешно пополнен";
        public const string WithdrawSuccess = "Средства успешно списаны";
        public const string LogsLoaded = "Последние операции получены";
        public const string InsufficientBalance = "Недостаточно средств для списания";
        public const string InsufficientBalanceDetails = "Недостаточно средств. Текущий баланс: {0} сомони";
        public const string AlreadyPaid = "Платеж за этот месяц уже зарегистрирован";
        public const string GroupInactive = "Группа неактивна или не найдена";
        public const string ZeroPayable = "Сумма к оплате равна 0 (после скидки)";
        public const string ChargeSuccess = "Ежемесячный платеж для группы выполнен";
        public const string MonthlyChargeSummary = "Списание успешно выполнено для {0} студентов";
        public const string GroupChargeSummary = "Списание для группы выполнено, успешных операций: {0}";
        public const string PaymentStatusRecalculated = "Статус оплаты студента обновлен по всем группам";
        public const string ChargeFailed = "Внутренняя ошибка при списании";
    }

    public static class Schedule
    {
        public const string NotFound = "Расписание не найдено";
        public const string Created = "Расписание успешно создано";
        public const string Updated = "Расписание успешно обновлено";
        public const string Deleted = "Расписание успешно удалено";
        public const string ConflictDetected = "Обнаружен конфликт расписания";
        public const string TimeSlotOccupied = "Временной слот занят";
        public const string AccessDeniedClassroom = "Доступ к выбранной аудитории запрещен";
        public const string AccessDeniedGroup = "Доступ к выбранной группе запрещен";
        public const string ClassroomGroupMismatch = "Аудитория и группа должны принадлежать одному учебному центру";
        public const string CreateError = "Ошибка при создании расписания: {0}";
        public const string UpdateError = "Ошибка при обновлении расписания: {0}";
        public const string DeleteError = "Ошибка при удалении расписания: {0}";
        public const string FetchError = "Ошибка при получении расписания: {0}";
        public const string ConflictCheckError = "Ошибка при проверке конфликтов: {0}";
        public const string AvailableSlotsError = "Ошибка при получении доступных временных промежутков: {0}";
        public const string WeeklyError = "Ошибка при получении недельного расписания: {0}";
        public const string ConflictMessage = "Время занятия пересекается с {0} в аудитории {1}";
    }

    public static class Classroom
    {
        public const string NotFound = "Аудитория не найдена";
        public const string AlreadyExists = "Аудитория с таким названием уже существует в этом центре";
        public const string CreationError = "Ошибка при создании аудитории: {0}";
        public const string GetListError = "Ошибка при получении списка аудиторий: {0}";
        public const string GetError = "Ошибка при получении аудитории: {0}";
        public const string GetByCenterError = "Ошибка при получении аудиторий центра: {0}";
        public const string UpdateError = "Ошибка при обновлении аудитории: {0}";
        public const string CannotDeleteWithActiveSchedules = "Невозможно удалить аудиторию, так как в ней есть активные занятия";
        public const string Deleted = "Аудитория успешно удалена";
        public const string DeleteError = "Ошибка при удалении аудитории: {0}";
        public const string GetScheduleError = "Ошибка при получении расписания аудитории: {0}";
        public const string GetAvailableError = "Ошибка при получении свободных аудиторий: {0}";
    }

    public static class File
    {
        public const string UploadSuccess = "Файл успешно загружен";
        public const string UploadError = "Ошибка при загрузке файла: {0}";
        public const string DeleteSuccess = "Файл успешно удален";
        public const string DeleteError = "Ошибка при удалении файла: {0}";
        public const string FileRequired = "Требуется файл";
        public const string InvalidFileFormat = "Недопустимый формат файла";
        public const string FileTooLarge = "Файл слишком большой";
    }

    public static class Validation
    {
        public const string RequiredField = "Поле '{0}' обязательно для заполнения";
        public const string InvalidEmail = "Некорректный email адрес";
        public const string InvalidPhoneNumber = "Некорректный номер телефона";
        public const string InvalidDateRange = "Некорректный диапазон дат";
        public const string MinLength = "Минимальная длина поля '{0}': {1} символов";
        public const string MaxLength = "Максимальная длина поля '{0}': {1} символов";
    }

    public static class Sms
    {
        public const string WelcomeStudent = "Здравствуйте, {0}!\nUsername: {1}\nPassword: {2}\nПожалуйста, перейдите по ссылке для входа в систему: {3}\nKavsar Academy";
        public const string WalletCode = "Здравствуйте, {0}!\nКод вашего кошелька: {1}.\nОбязательно предоставьте этот код администратору при пополнении счета. Пожалуйста, сохраните код и не теряйте его.";
        public const string TopUpNotification = "Здравствуйте, {0}! Ваш счет пополнен на {1:0.##} сомони. Текущий баланс: {2:0.##} сомони.";
        public const string ChargeNotification = "Здравствуйте, {0}! С вашего счета списано {1:0.##} сомони за группу {2}. Остаток: {3:0.##} сомони.";
        public const string InsufficientFunds = "Здравствуйте, {0}! Для оплаты группы {1} за {2:MM.yyyy} не хватает {3:0.##} сомони. Пожалуйста, пополните кошелек с кодом {4}.";
        public const string WelcomeMentor = "Здравствуйте, {0}!\nUsername: {1}\nPassword: {2}\nПожалуйста, перейдите по ссылке для входа в систему: {3}\nKavsar Academy";
        public const string WelcomeEmployee = "Здравствуйте, {0}!\nUsername: {1},\nPassword: {2}\nПожалуйста, для входа в систему перейдите по этому адресу: {3}\nKavsar Academy";
    }

    public static class Email
    {
        public const string InsufficientFunds = "<p>Здравствуйте, {0}.</p><p>Для оплаты группы <b>{1}</b> за {2:MM.yyyy} не хватает средств на счете.</p><p>Недостающая сумма: <b>{3:0.##}</b> сомони. Код кошелька: <b>{4}</b>.</p><p>Пожалуйста, пополните баланс и свяжитесь с администрацией при необходимости.</p>";
    }
}
