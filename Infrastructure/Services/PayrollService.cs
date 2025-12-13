using System.Net;
using Domain.DTOs.Payroll;
using Domain.Entities;
using Domain.Enums;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Constants;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Infrastructure.Services;

public class PayrollService(
    DataContext context,
    IHttpContextAccessor httpContextAccessor,
    IPayrollContractService contractService) : IPayrollService
{
    #region CreateWorkLogAsync

    public async Task<Response<GetWorkLogDto>> CreateWorkLogAsync(CreateWorkLogDto dto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<GetWorkLogDto>(HttpStatusCode.BadRequest, Messages.Group.CenterIdNotFound);

            if (dto.MentorId == null && dto.EmployeeUserId == null)
                return new Response<GetWorkLogDto>(HttpStatusCode.BadRequest, Messages.Payroll.MustSpecifyMentorOrEmployee);

            if (dto.MentorId != null && dto.EmployeeUserId != null)
                return new Response<GetWorkLogDto>(HttpStatusCode.BadRequest, Messages.Payroll.CannotSpecifyBoth);

            var workLog = new WorkLog
            {
                MentorId = dto.MentorId,
                EmployeeUserId = dto.EmployeeUserId,
                CenterId = centerId.Value,
                WorkDate = dto.WorkDate,
                Hours = dto.Hours,
                Description = dto.Description,
                GroupId = dto.GroupId,
                Month = dto.WorkDate.Month,
                Year = dto.WorkDate.Year,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await context.WorkLogs.AddAsync(workLog);
            await context.SaveChangesAsync();

            var result = await context.WorkLogs
                .Include(w => w.Mentor)
                .Include(w => w.EmployeeUser)
                .Include(w => w.Group)
                .FirstOrDefaultAsync(w => w.Id == workLog.Id);

            return new Response<GetWorkLogDto>(MapWorkLogToDto(result!)) { Message = Messages.Payroll.WorkLogCreated };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating work log");
            return new Response<GetWorkLogDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region DeleteWorkLogAsync

    public async Task<Response<string>> DeleteWorkLogAsync(int id)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Group.CenterIdNotFound);

            var workLog = await context.WorkLogs
                .FirstOrDefaultAsync(w => w.Id == id && w.CenterId == centerId && !w.IsDeleted);

            if (workLog == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Payroll.WorkLogNotFound);

            workLog.IsDeleted = true;
            workLog.UpdatedAt = DateTimeOffset.UtcNow;

            context.WorkLogs.Update(workLog);
            await context.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, Messages.Payroll.WorkLogDeleted);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting work log {Id}", id);
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetWorkLogsAsync

    public async Task<Response<List<GetWorkLogDto>>> GetWorkLogsAsync(int? mentorId, int? employeeUserId, int month, int year)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var query = context.WorkLogs
                .Include(w => w.Mentor)
                .Include(w => w.EmployeeUser)
                .Include(w => w.Group)
                .Where(w => !w.IsDeleted && w.Month == month && w.Year == year &&
                    (centerId == null || w.CenterId == centerId));

            if (mentorId.HasValue)
                query = query.Where(w => w.MentorId == mentorId);

            if (employeeUserId.HasValue)
                query = query.Where(w => w.EmployeeUserId == employeeUserId);

            var workLogs = await query.OrderBy(w => w.WorkDate).ToListAsync();
            var dtos = workLogs.Select(MapWorkLogToDto).ToList();

            return new Response<List<GetWorkLogDto>>(dtos);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting work logs");
            return new Response<List<GetWorkLogDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetTotalHoursAsync

    public async Task<Response<decimal>> GetTotalHoursAsync(int? mentorId, int? employeeUserId, int month, int year)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var query = context.WorkLogs
                .Where(w => !w.IsDeleted && w.Month == month && w.Year == year &&
                    (centerId == null || w.CenterId == centerId));

            if (mentorId.HasValue)
                query = query.Where(w => w.MentorId == mentorId);

            if (employeeUserId.HasValue)
                query = query.Where(w => w.EmployeeUserId == employeeUserId);

            var totalHours = await query.SumAsync(w => w.Hours);

            return new Response<decimal>(totalHours);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting total hours");
            return new Response<decimal>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region CalculatePayrollAsync

    public async Task<Response<GetPayrollRecordDto>> CalculatePayrollAsync(CalculatePayrollDto dto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<GetPayrollRecordDto>(HttpStatusCode.BadRequest, Messages.Group.CenterIdNotFound);

            if (dto.MentorId == null && dto.EmployeeUserId == null)
                return new Response<GetPayrollRecordDto>(HttpStatusCode.BadRequest, Messages.Payroll.MustSpecifyMentorOrEmployee);

            Response<GetPayrollContractDto> contractResponse;
            if (dto.MentorId.HasValue)
                contractResponse = await contractService.GetActiveByMentorAsync(dto.MentorId.Value);
            else
                contractResponse = await contractService.GetActiveByEmployeeAsync(dto.EmployeeUserId!.Value);

            if (contractResponse.Data == null)
                return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, Messages.Payroll.NoActiveContract);

            var contract = contractResponse.Data;

            var existingRecord = await context.PayrollRecords
                .FirstOrDefaultAsync(p => p.Month == dto.Month && p.Year == dto.Year &&
                    ((dto.MentorId.HasValue && p.MentorId == dto.MentorId) ||
                     (dto.EmployeeUserId.HasValue && p.EmployeeUserId == dto.EmployeeUserId)) &&
                    !p.IsDeleted);

            PayrollRecord record;
            bool isNewRecord = false;
            if (existingRecord != null)
            {
                record = existingRecord;
            }
            else
            {
                record = new PayrollRecord
                {
                    MentorId = dto.MentorId,
                    EmployeeUserId = dto.EmployeeUserId,
                    CenterId = centerId.Value,
                    Month = dto.Month,
                    Year = dto.Year,
                    Status = PayrollStatus.Draft,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await context.PayrollRecords.AddAsync(record);
                isNewRecord = true;
            }

            record.FixedAmount = contract.SalaryType == SalaryType.Fixed || contract.SalaryType == SalaryType.Mixed
                ? contract.FixedAmount
                : 0;

            if (contract.SalaryType == SalaryType.Hourly || contract.SalaryType == SalaryType.Mixed)
            {
                decimal totalHours = 0;

                if (dto.MentorId.HasValue)
                {
                    totalHours = await GetHoursFromJournalAsync(dto.MentorId.Value, dto.Month, dto.Year);
                }
                else if (dto.EmployeeUserId.HasValue)
                {
                    var totalHoursResponse = await GetTotalHoursAsync(null, dto.EmployeeUserId, dto.Month, dto.Year);
                    totalHours = totalHoursResponse.Data;
                }

                record.TotalHours = totalHours;
                record.HourlyAmount = record.TotalHours * contract.HourlyRate;
            }
            else
            {
                record.TotalHours = 0;
                record.HourlyAmount = 0;
            }

            if ((contract.SalaryType == SalaryType.Percentage || contract.SalaryType == SalaryType.Mixed) && dto.MentorId.HasValue)
            {
                var mentorGroups = await context.Groups
                    .Where(g => g.MentorId == dto.MentorId && !g.IsDeleted)
                    .Select(g => g.Id)
                    .ToListAsync();

                Log.Information("Calculating percentage for Mentor {MentorId}: Found {GroupCount} groups: {GroupIds}", 
                    dto.MentorId, mentorGroups.Count, string.Join(", ", mentorGroups));

                var payments = await context.Payments
                    .Where(p => mentorGroups.Contains(p.GroupId!.Value) &&
                        p.Month == dto.Month && p.Year == dto.Year &&
                        p.Status == PaymentStatus.Completed &&
                        !p.IsDeleted)
                    .ToListAsync();

                Log.Information("Found {PaymentCount} completed payments for month {Month}/{Year}. Payment details: {Payments}", 
                    payments.Count, dto.Month, dto.Year, 
                    payments.Select(p => new { p.Id, p.GroupId, p.Amount, p.Status, p.Month, p.Year }));

                var totalPayments = payments.Sum(p => p.Amount);

                record.TotalStudentPayments = totalPayments;
                record.PercentageRate = contract.StudentPercentage;
                record.PercentageAmount = totalPayments * (contract.StudentPercentage / 100);

                Log.Information("Percentage calculation: Total={Total}, Rate={Rate}%, Amount={Amount}", 
                    totalPayments, contract.StudentPercentage, record.PercentageAmount);
            }
            else
            {
                record.TotalStudentPayments = 0;
                record.PercentageRate = 0;
                record.PercentageAmount = 0;
            }

            var pendingAdvances = await GetPendingAdvancesAmountAsync(dto.MentorId, dto.EmployeeUserId, dto.Month, dto.Year);
            record.AdvanceDeduction = pendingAdvances.Data;

            record.GrossAmount = record.FixedAmount + record.HourlyAmount + record.PercentageAmount + record.BonusAmount - record.FineAmount;
            record.NetAmount = record.GrossAmount - record.AdvanceDeduction;

            record.Status = PayrollStatus.Calculated;
            record.UpdatedAt = DateTimeOffset.UtcNow;

            if (!isNewRecord)
            {
                context.PayrollRecords.Update(record);
            }
            await context.SaveChangesAsync();

            await MarkAdvancesAsDeducted(dto.MentorId, dto.EmployeeUserId, dto.Month, dto.Year, record.Id);

            var result = await GetPayrollRecordByIdAsync(record.Id);
            return new Response<GetPayrollRecordDto>(result.Data!) { Message = Messages.Payroll.PayrollCalculated };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating payroll");
            return new Response<GetPayrollRecordDto>(HttpStatusCode.InternalServerError, string.Format(Messages.Payroll.CalculationError, ex.Message));
        }
    }

    #endregion

    #region CalculateAllForMonthAsync

    public async Task<Response<List<GetPayrollRecordDto>>> CalculateAllForMonthAsync(int month, int year)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<List<GetPayrollRecordDto>>(HttpStatusCode.BadRequest, Messages.Group.CenterIdNotFound);

            var activeContracts = await context.PayrollContracts
                .Where(c => c.IsActive && !c.IsDeleted && c.CenterId == centerId)
                .ToListAsync();

            var results = new List<GetPayrollRecordDto>();

            foreach (var contract in activeContracts)
            {
                var dto = new CalculatePayrollDto
                {
                    MentorId = contract.MentorId,
                    EmployeeUserId = contract.EmployeeUserId,
                    Month = month,
                    Year = year
                };

                var result = await CalculatePayrollAsync(dto);
                if (result.Data != null)
                    results.Add(result.Data);
            }

            return new Response<List<GetPayrollRecordDto>>(results);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating all payrolls for {Month}/{Year}", month, year);
            return new Response<List<GetPayrollRecordDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region AddBonusFineAsync

    public async Task<Response<GetPayrollRecordDto>> AddBonusFineAsync(AddBonusFineDto dto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var record = await context.PayrollRecords
                .FirstOrDefaultAsync(p => p.Id == dto.PayrollRecordId && !p.IsDeleted &&
                    (centerId == null || p.CenterId == centerId));

            if (record == null)
                return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, Messages.Payroll.PayrollRecordNotFound);

            record.BonusAmount = dto.BonusAmount;
            record.BonusReason = dto.BonusReason;
            record.FineAmount = dto.FineAmount;
            record.FineReason = dto.FineReason;

            record.GrossAmount = record.FixedAmount + record.HourlyAmount + record.PercentageAmount + record.BonusAmount - record.FineAmount;
            record.NetAmount = record.GrossAmount - record.AdvanceDeduction;

            record.UpdatedAt = DateTimeOffset.UtcNow;

            context.PayrollRecords.Update(record);
            await context.SaveChangesAsync();

            var result = await GetPayrollRecordByIdAsync(record.Id);
            return new Response<GetPayrollRecordDto>(result.Data!) { Message = Messages.Payroll.BonusFineAdded };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error adding bonus/fine to payroll record {Id}", dto.PayrollRecordId);
            return new Response<GetPayrollRecordDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region ApproveAsync

    public async Task<Response<GetPayrollRecordDto>> ApproveAsync(int payrollRecordId)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var record = await context.PayrollRecords
                .FirstOrDefaultAsync(p => p.Id == payrollRecordId && !p.IsDeleted &&
                    (centerId == null || p.CenterId == centerId));

            if (record == null)
                return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, Messages.Payroll.PayrollRecordNotFound);

            if (record.Status == PayrollStatus.Approved || record.Status == PayrollStatus.Paid)
                return new Response<GetPayrollRecordDto>(HttpStatusCode.BadRequest, Messages.Payroll.PayrollAlreadyApproved);

            record.Status = PayrollStatus.Approved;
            record.ApprovedDate = DateTime.UtcNow;
            record.ApprovedByUserId = UserContextHelper.GetCurrentUserMentorId(httpContextAccessor);
            record.UpdatedAt = DateTimeOffset.UtcNow;

            context.PayrollRecords.Update(record);
            await context.SaveChangesAsync();

            var result = await GetPayrollRecordByIdAsync(record.Id);
            return new Response<GetPayrollRecordDto>(result.Data!) { Message = Messages.Payroll.PayrollApproved };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error approving payroll record {Id}", payrollRecordId);
            return new Response<GetPayrollRecordDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region MarkAsPaidAsync

    public async Task<Response<GetPayrollRecordDto>> MarkAsPaidAsync(int payrollRecordId, MarkAsPaidDto dto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var record = await context.PayrollRecords
                .FirstOrDefaultAsync(p => p.Id == payrollRecordId && !p.IsDeleted &&
                    (centerId == null || p.CenterId == centerId));

            if (record == null)
                return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, Messages.Payroll.PayrollRecordNotFound);

            if (record.Status != PayrollStatus.Approved)
                return new Response<GetPayrollRecordDto>(HttpStatusCode.BadRequest, Messages.Payroll.PayrollMustBeApproved);

            if (record.Status == PayrollStatus.Paid)
                return new Response<GetPayrollRecordDto>(HttpStatusCode.BadRequest, Messages.Payroll.PayrollAlreadyPaid);

            record.Status = PayrollStatus.Paid;
            record.PaidDate = DateTime.UtcNow;
            record.PaymentMethod = dto.PaymentMethod;
            if (!string.IsNullOrWhiteSpace(dto.Notes))
                record.Notes = dto.Notes;
            record.UpdatedAt = DateTimeOffset.UtcNow;

            context.PayrollRecords.Update(record);
            await context.SaveChangesAsync();

            var result = await GetPayrollRecordByIdAsync(record.Id);
            return new Response<GetPayrollRecordDto>(result.Data!) { Message = Messages.Payroll.PayrollPaid };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error marking payroll record {Id} as paid", payrollRecordId);
            return new Response<GetPayrollRecordDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetPayrollRecordByIdAsync

    public async Task<Response<GetPayrollRecordDto>> GetPayrollRecordByIdAsync(int id)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var record = await context.PayrollRecords
                .Include(p => p.Mentor)
                .Include(p => p.EmployeeUser)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted &&
                    (centerId == null || p.CenterId == centerId));

            if (record == null)
                return new Response<GetPayrollRecordDto>(HttpStatusCode.NotFound, Messages.Payroll.PayrollRecordNotFound);

            return new Response<GetPayrollRecordDto>(MapPayrollRecordToDto(record));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting payroll record {Id}", id);
            return new Response<GetPayrollRecordDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetPayrollRecordsAsync

    public async Task<Response<List<GetPayrollRecordDto>>> GetPayrollRecordsAsync(int month, int year)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var records = await context.PayrollRecords
                .Include(p => p.Mentor)
                .Include(p => p.EmployeeUser)
                .Where(p => p.Month == month && p.Year == year && !p.IsDeleted &&
                    (centerId == null || p.CenterId == centerId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var dtos = records.Select(MapPayrollRecordToDto).ToList();
            return new Response<List<GetPayrollRecordDto>>(dtos);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting payroll records for {Month}/{Year}", month, year);
            return new Response<List<GetPayrollRecordDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetPayrollRecordsPaginatedAsync

    public async Task<PaginationResponse<List<GetPayrollRecordDto>>> GetPayrollRecordsPaginatedAsync(PayrollFilter filter)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var query = context.PayrollRecords
                .Include(p => p.Mentor)
                .Include(p => p.EmployeeUser)
                .Where(p => !p.IsDeleted && (centerId == null || p.CenterId == centerId));

            if (filter.MentorId.HasValue)
                query = query.Where(p => p.MentorId == filter.MentorId);

            if (filter.EmployeeUserId.HasValue)
                query = query.Where(p => p.EmployeeUserId == filter.EmployeeUserId);

            if (filter.Month.HasValue)
                query = query.Where(p => p.Month == filter.Month);

            if (filter.Year.HasValue)
                query = query.Where(p => p.Year == filter.Year);

            if (filter.Status.HasValue)
                query = query.Where(p => p.Status == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                query = query.Where(p =>
                    (p.Mentor != null && p.Mentor.FullName.Contains(filter.Search)) ||
                    (p.EmployeeUser != null && p.EmployeeUser.FullName.Contains(filter.Search)));
            }

            var totalCount = await query.CountAsync();
            var records = await query
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ThenByDescending(p => p.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var dtos = records.Select(MapPayrollRecordToDto).ToList();

            return new PaginationResponse<List<GetPayrollRecordDto>>(dtos, totalCount, filter.PageNumber, filter.PageSize);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting paginated payroll records");
            return new PaginationResponse<List<GetPayrollRecordDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region CreateAdvanceAsync

    public async Task<Response<GetAdvanceDto>> CreateAdvanceAsync(CreateAdvanceDto dto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<GetAdvanceDto>(HttpStatusCode.BadRequest, Messages.Group.CenterIdNotFound);

            if (dto.MentorId == null && dto.EmployeeUserId == null)
                return new Response<GetAdvanceDto>(HttpStatusCode.BadRequest, Messages.Payroll.MustSpecifyMentorOrEmployee);

            var currentUserId = UserContextHelper.GetCurrentUserMentorId(httpContextAccessor);
            if (currentUserId == null)
                return new Response<GetAdvanceDto>(HttpStatusCode.Unauthorized, "User ID not found");

            var currentUserName = await context.Users
                .Where(u => u.Id == currentUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();

            var advance = new Advance
            {
                MentorId = dto.MentorId,
                EmployeeUserId = dto.EmployeeUserId,
                CenterId = centerId.Value,
                Amount = dto.Amount,
                GivenDate = DateTime.UtcNow,
                Reason = dto.Reason,
                TargetMonth = dto.TargetMonth,
                TargetYear = dto.TargetYear,
                Status = AdvanceStatus.Pending,
                GivenByUserId = currentUserId.Value,
                GivenByName = currentUserName,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await context.Advances.AddAsync(advance);
            await context.SaveChangesAsync();

            var result = await context.Advances
                .Include(a => a.Mentor)
                .Include(a => a.EmployeeUser)
                .FirstOrDefaultAsync(a => a.Id == advance.Id);

            return new Response<GetAdvanceDto>(MapAdvanceToDto(result!)) { Message = Messages.Payroll.AdvanceCreated };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating advance");
            return new Response<GetAdvanceDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region UpdateAdvanceAsync

    public async Task<Response<GetAdvanceDto>> UpdateAdvanceAsync(int advanceId, UpdateAdvanceDto dto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var advance = await context.Advances
                .FirstOrDefaultAsync(a => a.Id == advanceId && !a.IsDeleted &&
                    (centerId == null || a.CenterId == centerId));

            if (advance == null)
                return new Response<GetAdvanceDto>(HttpStatusCode.NotFound, Messages.Payroll.AdvanceNotFound);

            if (advance.Status == AdvanceStatus.Deducted)
                return new Response<GetAdvanceDto>(HttpStatusCode.BadRequest, Messages.Payroll.AdvanceAlreadyDeducted);

            if (advance.Status == AdvanceStatus.Cancelled)
                return new Response<GetAdvanceDto>(HttpStatusCode.BadRequest, "Cannot update cancelled advance");

            advance.Amount = dto.Amount;
            advance.Reason = dto.Reason;
            advance.TargetMonth = dto.TargetMonth;
            advance.TargetYear = dto.TargetYear;
            advance.UpdatedAt = DateTimeOffset.UtcNow;

            context.Advances.Update(advance);
            await context.SaveChangesAsync();

            var result = await context.Advances
                .Include(a => a.Mentor)
                .Include(a => a.EmployeeUser)
                .FirstOrDefaultAsync(a => a.Id == advance.Id);

            return new Response<GetAdvanceDto>(MapAdvanceToDto(result!)) { Message = "Advance updated successfully" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating advance {Id}", advanceId);
            return new Response<GetAdvanceDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region CancelAdvanceAsync

    public async Task<Response<string>> CancelAdvanceAsync(int id)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var advance = await context.Advances
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted &&
                    (centerId == null || a.CenterId == centerId));

            if (advance == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Payroll.AdvanceNotFound);

            if (advance.Status == AdvanceStatus.Deducted)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Payroll.AdvanceAlreadyDeducted);

            if (advance.Status == AdvanceStatus.Cancelled)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Payroll.AdvanceAlreadyCancelled);

            advance.Status = AdvanceStatus.Cancelled;
            advance.UpdatedAt = DateTimeOffset.UtcNow;

            context.Advances.Update(advance);
            await context.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, Messages.Payroll.AdvanceCancelled);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error cancelling advance {Id}", id);
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetAdvancesAsync

    public async Task<Response<List<GetAdvanceDto>>> GetAdvancesAsync(int? mentorId, int? employeeUserId, int? month, int? year)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var query = context.Advances
                .Include(a => a.Mentor)
                .Include(a => a.EmployeeUser)
                .Where(a => !a.IsDeleted && (centerId == null || a.CenterId == centerId));

            if (mentorId.HasValue)
                query = query.Where(a => a.MentorId == mentorId);

            if (employeeUserId.HasValue)
                query = query.Where(a => a.EmployeeUserId == employeeUserId);

            if (month.HasValue)
                query = query.Where(a => a.TargetMonth == month);

            if (year.HasValue)
                query = query.Where(a => a.TargetYear == year);

            var advances = await query.OrderByDescending(a => a.GivenDate).ToListAsync();
            var dtos = advances.Select(MapAdvanceToDto).ToList();

            return new Response<List<GetAdvanceDto>>(dtos);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting advances");
            return new Response<List<GetAdvanceDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetPendingAdvancesAmountAsync

    public async Task<Response<decimal>> GetPendingAdvancesAmountAsync(int? mentorId, int? employeeUserId, int month, int year)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var query = context.Advances
                .Where(a => !a.IsDeleted && a.Status == AdvanceStatus.Pending &&
                    a.TargetMonth == month && a.TargetYear == year &&
                    (centerId == null || a.CenterId == centerId));

            if (mentorId.HasValue)
                query = query.Where(a => a.MentorId == mentorId);

            if (employeeUserId.HasValue)
                query = query.Where(a => a.EmployeeUserId == employeeUserId);

            var totalAdvances = await query.SumAsync(a => a.Amount);

            return new Response<decimal>(totalAdvances);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting pending advances amount");
            return new Response<decimal>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetPaymentHistoryAsync

    public async Task<Response<List<PaymentHistoryDto>>> GetPaymentHistoryAsync(int? mentorId, int? employeeUserId, int? month, int? year)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var query = context.PayrollRecords
                .Include(p => p.Mentor)
                .Include(p => p.EmployeeUser)
                .Where(p => !p.IsDeleted && p.Status == PayrollStatus.Paid &&
                    (centerId == null || p.CenterId == centerId));

            if (mentorId.HasValue)
                query = query.Where(p => p.MentorId == mentorId);

            if (employeeUserId.HasValue)
                query = query.Where(p => p.EmployeeUserId == employeeUserId);

            if (month.HasValue)
                query = query.Where(p => p.Month == month);

            if (year.HasValue)
                query = query.Where(p => p.Year == year);

            var records = await query
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ToListAsync();

            var dtos = records.Select(r => new PaymentHistoryDto
            {
                Id = r.Id,
                MentorId = r.MentorId,
                MentorName = r.Mentor?.FullName,
                EmployeeUserId = r.EmployeeUserId,
                EmployeeName = r.EmployeeUser?.FullName,
                Month = r.Month,
                Year = r.Year,
                NetAmount = r.NetAmount,
                PaidDate = r.PaidDate,
                PaymentMethod = r.PaymentMethod,
                PaymentMethodDisplay = r.PaymentMethod?.ToString(),
                Notes = r.Notes
            }).ToList();

            return new Response<List<PaymentHistoryDto>>(dtos);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting payment history for MentorId={MentorId}, EmployeeUserId={EmployeeUserId}, Month={Month}, Year={Year}", 
                mentorId, employeeUserId, month, year);
            return new Response<List<PaymentHistoryDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetMonthlySummaryAsync

    public async Task<Response<PayrollSummaryDto>> GetMonthlySummaryAsync(int month, int year)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var records = await context.PayrollRecords
                .Where(p => p.Month == month && p.Year == year && !p.IsDeleted &&
                    (centerId == null || p.CenterId == centerId))
                .ToListAsync();

            var summary = new PayrollSummaryDto
            {
                Month = month,
                Year = year,
                TotalMentors = records.Count(r => r.MentorId.HasValue),
                TotalEmployees = records.Count(r => r.EmployeeUserId.HasValue),
                TotalFixedAmount = records.Sum(r => r.FixedAmount),
                TotalHourlyAmount = records.Sum(r => r.HourlyAmount),
                TotalPercentageAmount = records.Sum(r => r.PercentageAmount),
                TotalBonusAmount = records.Sum(r => r.BonusAmount),
                TotalFineAmount = records.Sum(r => r.FineAmount),
                TotalAdvanceDeduction = records.Sum(r => r.AdvanceDeduction),
                TotalGrossAmount = records.Sum(r => r.GrossAmount),
                TotalNetAmount = records.Sum(r => r.NetAmount),
                DraftCount = records.Count(r => r.Status == PayrollStatus.Draft),
                CalculatedCount = records.Count(r => r.Status == PayrollStatus.Calculated),
                ApprovedCount = records.Count(r => r.Status == PayrollStatus.Approved),
                PaidCount = records.Count(r => r.Status == PayrollStatus.Paid)
            };

            return new Response<PayrollSummaryDto>(summary);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting monthly summary for {Month}/{Year}", month, year);
            return new Response<PayrollSummaryDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region Helper Methods

    private async Task MarkAdvancesAsDeducted(int? mentorId, int? employeeUserId, int month, int year, int payrollRecordId)
    {
        try
        {
            var query = context.Advances.Where(a => !a.IsDeleted &&
                a.Status == AdvanceStatus.Pending &&
                a.TargetMonth == month &&
                a.TargetYear == year);

            if (mentorId.HasValue)
                query = query.Where(a => a.MentorId == mentorId);

            if (employeeUserId.HasValue)
                query = query.Where(a => a.EmployeeUserId == employeeUserId);

            var advances = await query.ToListAsync();

            foreach (var advance in advances)
            {
                advance.Status = AdvanceStatus.Deducted;
                advance.PayrollRecordId = payrollRecordId;
                advance.UpdatedAt = DateTimeOffset.UtcNow;
            }

            context.Advances.UpdateRange(advances);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error marking advances as deducted");
        }
    }

    private async Task<decimal> GetHoursFromJournalAsync(int mentorId, int month, int year)
    {
        try
        {
            var firstDayOfMonth = new DateTimeOffset(new DateTime(year, month, 1), TimeSpan.Zero);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var mentorGroupsWithDates = await context.Groups
                .Where(g => g.MentorId == mentorId && !g.IsDeleted)
                .Select(g => new
                {
                    g.Id,
                    g.StartDate,
                    g.EndDate
                })
                .ToListAsync();

            if (!mentorGroupsWithDates.Any())
                return 0;

            decimal totalHours = 0;

            foreach (var group in mentorGroupsWithDates)
            {
                var effectiveStartDate = group.StartDate > firstDayOfMonth ? group.StartDate : firstDayOfMonth;
                var effectiveEndDate = group.EndDate < lastDayOfMonth ? group.EndDate : lastDayOfMonth;

                if (effectiveStartDate > lastDayOfMonth || effectiveEndDate < firstDayOfMonth)
                    continue;

                var groupJournals = await context.Journals
                    .Where(j => j.GroupId == group.Id &&
                        !j.IsDeleted &&
                        j.WeekStartDate <= effectiveEndDate &&
                        j.WeekEndDate >= effectiveStartDate)
                    .Select(j => new
                    {
                        j.Id,
                        j.WeekStartDate,
                        j.WeekEndDate
                    })
                    .ToListAsync();

                if (!groupJournals.Any())
                    continue;

                foreach (var journal in groupJournals)
                {
                    var journalEffectiveStart = journal.WeekStartDate > effectiveStartDate ? journal.WeekStartDate : effectiveStartDate;
                    var journalEffectiveEnd = journal.WeekEndDate < effectiveEndDate ? journal.WeekEndDate : effectiveEndDate;

                    var journalEntriesGrouped = await context.JournalEntries
                        .Where(je => je.JournalId == journal.Id && !je.IsDeleted)
                        .GroupBy(je => new { je.DayOfWeek, je.LessonNumber })
                        .Select(g => g.FirstOrDefault())
                        .ToListAsync();

                    foreach (var entry in journalEntriesGrouped)
                    {
                        if (entry != null && entry.StartTime.HasValue && entry.EndTime.HasValue)
                        {
                            var start = entry.StartTime.Value;
                            var end = entry.EndTime.Value;

                            var durationMinutes = (decimal)((end.Hour * 60 + end.Minute) - (start.Hour * 60 + start.Minute));
                            var lessonHours = durationMinutes / 60;

                            var weekStart = journalEffectiveStart.Date;
                            var weekEnd = journalEffectiveEnd.Date;
                            var daysInWeek = (weekEnd - weekStart).Days + 1;

                            var targetDayOfWeek = entry.DayOfWeek;
                            var currentDate = weekStart;
                            var lessonOccurrences = 0;

                            while (currentDate <= weekEnd)
                            {
                                var dotNetDayOfWeek = (int)currentDate.DayOfWeek;
                                var adjustedDayOfWeek = dotNetDayOfWeek == 0 ? 7 : dotNetDayOfWeek;

                                if (adjustedDayOfWeek == targetDayOfWeek &&
                                    currentDate >= effectiveStartDate.Date &&
                                    currentDate <= effectiveEndDate.Date)
                                {
                                    lessonOccurrences++;
                                }

                                currentDate = currentDate.AddDays(1);
                            }

                            totalHours += lessonHours * lessonOccurrences;
                        }
                    }
                }
            }

            return totalHours;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating hours from journal for mentor {MentorId}", mentorId);
            return 0;
        }
    }

    private static GetWorkLogDto MapWorkLogToDto(WorkLog w)
    {
        return new GetWorkLogDto
        {
            Id = w.Id,
            MentorId = w.MentorId,
            MentorName = w.Mentor?.FullName,
            EmployeeUserId = w.EmployeeUserId,
            EmployeeName = w.EmployeeUser?.FullName,
            WorkDate = w.WorkDate,
            Hours = w.Hours,
            Description = w.Description,
            GroupId = w.GroupId,
            GroupName = w.Group?.Name,
            Month = w.Month,
            Year = w.Year
        };
    }

    private static GetPayrollRecordDto MapPayrollRecordToDto(PayrollRecord p)
    {
        return new GetPayrollRecordDto
        {
            Id = p.Id,
            MentorId = p.MentorId,
            MentorName = p.Mentor?.FullName,
            EmployeeUserId = p.EmployeeUserId,
            EmployeeName = p.EmployeeUser?.FullName,
            Month = p.Month,
            Year = p.Year,
            FixedAmount = p.FixedAmount,
            HourlyAmount = p.HourlyAmount,
            TotalHours = p.TotalHours,
            PercentageAmount = p.PercentageAmount,
            TotalStudentPayments = p.TotalStudentPayments,
            PercentageRate = p.PercentageRate,
            BonusAmount = p.BonusAmount,
            BonusReason = p.BonusReason,
            FineAmount = p.FineAmount,
            FineReason = p.FineReason,
            AdvanceDeduction = p.AdvanceDeduction,
            GrossAmount = p.GrossAmount,
            NetAmount = p.NetAmount,
            Status = p.Status,
            StatusDisplay = GetPayrollStatusDisplay(p.Status),
            ApprovedDate = p.ApprovedDate,
            PaidDate = p.PaidDate,
            PaymentMethod = p.PaymentMethod,
            Notes = p.Notes
        };
    }

    private static GetAdvanceDto MapAdvanceToDto(Advance a)
    {
        return new GetAdvanceDto
        {
            Id = a.Id,
            MentorId = a.MentorId,
            MentorName = a.Mentor?.FullName,
            EmployeeUserId = a.EmployeeUserId,
            EmployeeName = a.EmployeeUser?.FullName,
            Amount = a.Amount,
            GivenDate = a.GivenDate,
            Reason = a.Reason,
            TargetMonth = a.TargetMonth,
            TargetYear = a.TargetYear,
            Status = a.Status,
            StatusDisplay = GetAdvanceStatusDisplay(a.Status),
            GivenByName = a.GivenByName,
            CreatedAt = a.CreatedAt
        };
    }

    private static string GetPayrollStatusDisplay(PayrollStatus status)
    {
        return status switch
        {
            PayrollStatus.Draft => Messages.Payroll.StatusDraft,
            PayrollStatus.Calculated => Messages.Payroll.StatusCalculated,
            PayrollStatus.Approved => Messages.Payroll.StatusApproved,
            PayrollStatus.Paid => Messages.Payroll.StatusPaid,
            PayrollStatus.Cancelled => Messages.Payroll.StatusCancelled,
            _ => Messages.Common.Unknown
        };
    }

    private static string GetAdvanceStatusDisplay(AdvanceStatus status)
    {
        return status switch
        {
            AdvanceStatus.Pending => Messages.Payroll.AdvanceStatusPending,
            AdvanceStatus.Deducted => Messages.Payroll.AdvanceStatusDeducted,
            AdvanceStatus.Cancelled => Messages.Payroll.AdvanceStatusCancelled,
            _ => Messages.Common.Unknown
        };
    }

    #endregion
}
