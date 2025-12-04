using Domain.DTOs.Finance;
using Domain.Entities;
using Domain.Enums;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Constants;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Infrastructure.Services;

public class ExpenseService(DataContext dbContext, IHttpContextAccessor httpContextAccessor) : IExpenseService
{
    private readonly DataContext _dbContext = dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    #region CreateAsync

    public async Task<Response<GetExpenseDto>> CreateAsync(CreateExpenseDto dto)
    {
        try
        {
            if (dto.Amount <= 0)
                return new Response<GetExpenseDto>(HttpStatusCode.BadRequest, Messages.Finance.InvalidAmount);

            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            var effectiveCenterId = userCenterId ?? dto.CenterId;

            var centerExists = await _dbContext.Centers.AnyAsync(c => c.Id == effectiveCenterId);
            if (!centerExists)
                return new Response<GetExpenseDto>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            if (dto.Category == ExpenseCategory.Salary && dto.MentorId is not null)
            {
                var mentorExists = await _dbContext.Mentors.AnyAsync(m => m.Id == dto.MentorId && m.CenterId == effectiveCenterId);
                if (!mentorExists)
                    return new Response<GetExpenseDto>(HttpStatusCode.NotFound, Messages.Mentor.NotFound);
            }

            var month = dto.Month ?? dto.ExpenseDate.Month;
            var year = dto.Year ?? dto.ExpenseDate.Year;

            var financeService = new FinanceService(_dbContext, _httpContextAccessor);
            if (await financeService.IsMonthClosedAsync(effectiveCenterId, year, month))
                return new Response<GetExpenseDto>(HttpStatusCode.BadRequest, "Этот месяц закрыт, создание расходов запрещено");

            var entity = new Expense
            {
                CenterId = effectiveCenterId,
                Amount = dto.Amount,
                ExpenseDate = dto.ExpenseDate,
                Category = dto.Category,
                PaymentMethod = dto.PaymentMethod,
                Description = dto.Description,
                MentorId = dto.MentorId,
                Month = month,
                Year = year,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Expenses.Add(entity);
            await _dbContext.SaveChangesAsync();

            var user = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "anonymous";
            Log.Information("Пользователь {User} создал расход: {@Expense}", user, entity);

            return new Response<GetExpenseDto>(MapToGetDto(entity));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при создании расхода");
            return new Response<GetExpenseDto>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region UpdateAsync

    public async Task<Response<GetExpenseDto>> UpdateAsync(int id, UpdateExpenseDto dto)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            var entity = await _dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted && (userCenterId == null || e.CenterId == userCenterId.Value));
            if (entity is null)
                return new Response<GetExpenseDto>(HttpStatusCode.NotFound, Messages.Common.NotFound);

            if (dto.Amount <= 0)
                return new Response<GetExpenseDto>(HttpStatusCode.BadRequest, Messages.Finance.InvalidAmount);

            if (dto.Category == ExpenseCategory.Salary && dto.MentorId is not null)
            {
                var mentorExists = await _dbContext.Mentors.AnyAsync(m => m.Id == dto.MentorId && m.CenterId == entity.CenterId);
                if (!mentorExists)
                    return new Response<GetExpenseDto>(HttpStatusCode.NotFound, Messages.Mentor.NotFound);
            }

            var isClosed = await new FinanceService(_dbContext, _httpContextAccessor).IsMonthClosedAsync(entity.CenterId, entity.Year, entity.Month);
            if (isClosed)
                return new Response<GetExpenseDto>(HttpStatusCode.BadRequest, "Этот месяц закрыт, изменение запрещено");

            entity.Amount = dto.Amount;
            entity.ExpenseDate = dto.ExpenseDate;
            entity.Category = dto.Category;
            entity.PaymentMethod = dto.PaymentMethod;
            entity.Description = dto.Description;
            entity.MentorId = dto.MentorId;
            entity.Month = dto.Month ?? dto.ExpenseDate.Month;
            entity.Year = dto.Year ?? dto.ExpenseDate.Year;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync();

            var user = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "anonymous";
            Log.Information("Пользователь {User} обновил расход: {@Expense}", user, entity);

            return new Response<GetExpenseDto>(MapToGetDto(entity));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при обновлении расхода {ExpenseId}", id);
            return new Response<GetExpenseDto>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region DeleteAsync

    public async Task<Response<bool>> DeleteAsync(int id)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            var entity = await _dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted && (userCenterId == null || e.CenterId == userCenterId.Value));
            if (entity is null)
                return new Response<bool>(HttpStatusCode.NotFound, Messages.Common.NotFound);

            var isClosed = await new FinanceService(_dbContext, _httpContextAccessor).IsMonthClosedAsync(entity.CenterId, entity.Year, entity.Month);
            if (isClosed)
                return new Response<bool>(HttpStatusCode.BadRequest, "Этот месяц закрыт, удаление запрещено");

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync();

            var user = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "anonymous";
            Log.Information("Пользователь {User} удалил (soft) расход: {ExpenseId}", user, id);
            return new Response<bool>(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при удалении расхода {ExpenseId}", id);
            return new Response<bool>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region GetByIdAsync

    public async Task<Response<GetExpenseDto>> GetByIdAsync(int id)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            var entity = await _dbContext.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted && (userCenterId == null || e.CenterId == userCenterId.Value));
            if (entity is null)
                return new Response<GetExpenseDto>(HttpStatusCode.NotFound, Messages.Common.NotFound);

            return new Response<GetExpenseDto>(MapToGetDto(entity));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при получении расхода {ExpenseId}", id);
            return new Response<GetExpenseDto>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region GetAsync

    public async Task<Response<List<GetExpenseDto>>> GetAsync(ExpenseFilter filter)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            var effectiveCenterId = userCenterId ?? filter.CenterId;
            var query = _dbContext.Expenses.AsNoTracking().Where(e => !e.IsDeleted && e.CenterId == effectiveCenterId);

            if (filter.Category.HasValue)
                query = query.Where(e => e.Category == filter.Category);

            if (filter.MentorId.HasValue)
                query = query.Where(e => e.MentorId == filter.MentorId);

            if (filter.StartDate.HasValue)
                query = query.Where(e => e.ExpenseDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(e => e.ExpenseDate <= filter.EndDate.Value);

            if (filter.MinAmount.HasValue)
                query = query.Where(e => e.Amount >= filter.MinAmount.Value);

            if (filter.MaxAmount.HasValue)
                query = query.Where(e => e.Amount <= filter.MaxAmount.Value);

            if (filter.Month.HasValue)
                query = query.Where(e => e.Month == filter.Month.Value);

            if (filter.Year.HasValue)
                query = query.Where(e => e.Year == filter.Year.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(e => e.Description != null && EF.Functions.ILike(e.Description, $"%{filter.Search}%"));

            query = query.OrderByDescending(e => e.ExpenseDate)
                         .Skip((filter.PageNumber - 1) * filter.PageSize)
                         .Take(filter.PageSize);

            var list = await query.Select(e => MapToGetDto(e)).ToListAsync();
            return new Response<List<GetExpenseDto>>(list);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при запросе списка расходов");
            return new Response<List<GetExpenseDto>>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region MapToGetDto

    private static GetExpenseDto MapToGetDto(Expense e)
    {
        return new GetExpenseDto
        {
            Id = e.Id,
            CenterId = e.CenterId,
            Amount = e.Amount,
            ExpenseDate = e.ExpenseDate,
            Category = e.Category,
            PaymentMethod = e.PaymentMethod,
            Description = e.Description,
            MentorId = e.MentorId,
            Month = e.Month,
            Year = e.Year,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }

    #endregion
}
