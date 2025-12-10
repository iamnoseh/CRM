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

public class PayrollContractService(
    DataContext context,
    IHttpContextAccessor httpContextAccessor) : IPayrollContractService
{
    #region CreateAsync

    public async Task<Response<GetPayrollContractDto>> CreateAsync(CreatePayrollContractDto dto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<GetPayrollContractDto>(HttpStatusCode.BadRequest, Messages.Group.CenterIdNotFound);

            if (dto.MentorId == null && dto.EmployeeUserId == null)
                return new Response<GetPayrollContractDto>(HttpStatusCode.BadRequest, Messages.Payroll.MustSpecifyMentorOrEmployee);

            if (dto.MentorId != null && dto.EmployeeUserId != null)
                return new Response<GetPayrollContractDto>(HttpStatusCode.BadRequest, Messages.Payroll.CannotSpecifyBoth);

            if (dto.MentorId != null)
            {
                var mentorExists = await context.Mentors.AnyAsync(m => m.Id == dto.MentorId && !m.IsDeleted);
                if (!mentorExists)
                    return new Response<GetPayrollContractDto>(HttpStatusCode.NotFound, Messages.Mentor.NotFound);

                var existingContract = await context.PayrollContracts
                    .AnyAsync(c => c.MentorId == dto.MentorId && c.IsActive && !c.IsDeleted);
                if (existingContract)
                    return new Response<GetPayrollContractDto>(HttpStatusCode.BadRequest, Messages.Payroll.ContractAlreadyExists);
            }

            if (dto.EmployeeUserId != null)
            {
                var userExists = await context.Users.AnyAsync(u => u.Id == dto.EmployeeUserId);
                if (!userExists)
                    return new Response<GetPayrollContractDto>(HttpStatusCode.NotFound, Messages.User.NotFound);

                var existingContract = await context.PayrollContracts
                    .AnyAsync(c => c.EmployeeUserId == dto.EmployeeUserId && c.IsActive && !c.IsDeleted);
                if (existingContract)
                    return new Response<GetPayrollContractDto>(HttpStatusCode.BadRequest, Messages.Payroll.ContractAlreadyExists);
            }

            var contract = new PayrollContract
            {
                MentorId = dto.MentorId,
                EmployeeUserId = dto.EmployeeUserId,
                CenterId = centerId.Value,
                SalaryType = dto.SalaryType,
                FixedAmount = dto.FixedAmount,
                HourlyRate = dto.HourlyRate,
                StudentPercentage = dto.StudentPercentage,
                Description = dto.Description,
                EffectiveFrom = dto.EffectiveFrom,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await context.PayrollContracts.AddAsync(contract);
            await context.SaveChangesAsync();

            var result = await GetByIdAsync(contract.Id);
            return new Response<GetPayrollContractDto>(result.Data!) { Message = Messages.Payroll.ContractCreated };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating payroll contract");
            return new Response<GetPayrollContractDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region UpdateAsync

    public async Task<Response<GetPayrollContractDto>> UpdateAsync(int id, UpdatePayrollContractDto dto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<GetPayrollContractDto>(HttpStatusCode.BadRequest, Messages.Group.CenterIdNotFound);

            var contract = await context.PayrollContracts
                .FirstOrDefaultAsync(c => c.Id == id && c.CenterId == centerId && !c.IsDeleted);

            if (contract == null)
                return new Response<GetPayrollContractDto>(HttpStatusCode.NotFound, Messages.Payroll.ContractNotFound);

            if (dto.SalaryType.HasValue) contract.SalaryType = dto.SalaryType.Value;
            if (dto.FixedAmount.HasValue) contract.FixedAmount = dto.FixedAmount.Value;
            if (dto.HourlyRate.HasValue) contract.HourlyRate = dto.HourlyRate.Value;
            if (dto.StudentPercentage.HasValue) contract.StudentPercentage = dto.StudentPercentage.Value;
            if (dto.Description != null) contract.Description = dto.Description;
            if (dto.EffectiveTo.HasValue) contract.EffectiveTo = dto.EffectiveTo.Value;
            if (dto.IsActive.HasValue) contract.IsActive = dto.IsActive.Value;

            contract.UpdatedAt = DateTimeOffset.UtcNow;

            context.PayrollContracts.Update(contract);
            await context.SaveChangesAsync();

            var result = await GetByIdAsync(contract.Id);
            return new Response<GetPayrollContractDto>(result.Data!) { Message = Messages.Payroll.ContractUpdated };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating payroll contract {Id}", id);
            return new Response<GetPayrollContractDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region DeactivateAsync

    public async Task<Response<string>> DeactivateAsync(int id)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Group.CenterIdNotFound);

            var contract = await context.PayrollContracts
                .FirstOrDefaultAsync(c => c.Id == id && c.CenterId == centerId && !c.IsDeleted);

            if (contract == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Payroll.ContractNotFound);

            contract.IsActive = false;
            contract.EffectiveTo = DateTimeOffset.UtcNow;
            contract.UpdatedAt = DateTimeOffset.UtcNow;

            context.PayrollContracts.Update(contract);
            await context.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, Messages.Payroll.ContractDeactivated);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deactivating payroll contract {Id}", id);
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetByIdAsync

    public async Task<Response<GetPayrollContractDto>> GetByIdAsync(int id)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var contract = await context.PayrollContracts
                .Include(c => c.Mentor)
                .Include(c => c.EmployeeUser)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && 
                    (centerId == null || c.CenterId == centerId));

            if (contract == null)
                return new Response<GetPayrollContractDto>(HttpStatusCode.NotFound, Messages.Payroll.ContractNotFound);

            return new Response<GetPayrollContractDto>(MapToDto(contract));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting payroll contract {Id}", id);
            return new Response<GetPayrollContractDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetActiveByMentorAsync

    public async Task<Response<GetPayrollContractDto>> GetActiveByMentorAsync(int mentorId)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var contract = await context.PayrollContracts
                .Include(c => c.Mentor)
                .FirstOrDefaultAsync(c => c.MentorId == mentorId && c.IsActive && !c.IsDeleted &&
                    (centerId == null || c.CenterId == centerId));

            if (contract == null)
                return new Response<GetPayrollContractDto>(HttpStatusCode.NotFound, Messages.Payroll.ContractNotFound);

            return new Response<GetPayrollContractDto>(MapToDto(contract));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting active contract for mentor {MentorId}", mentorId);
            return new Response<GetPayrollContractDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetActiveByEmployeeAsync

    public async Task<Response<GetPayrollContractDto>> GetActiveByEmployeeAsync(int employeeUserId)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var contract = await context.PayrollContracts
                .Include(c => c.EmployeeUser)
                .FirstOrDefaultAsync(c => c.EmployeeUserId == employeeUserId && c.IsActive && !c.IsDeleted &&
                    (centerId == null || c.CenterId == centerId));

            if (contract == null)
                return new Response<GetPayrollContractDto>(HttpStatusCode.NotFound, Messages.Payroll.ContractNotFound);

            return new Response<GetPayrollContractDto>(MapToDto(contract));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting active contract for employee {EmployeeUserId}", employeeUserId);
            return new Response<GetPayrollContractDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetAllAsync

    public async Task<Response<List<GetPayrollContractDto>>> GetAllAsync()
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var contracts = await context.PayrollContracts
                .Include(c => c.Mentor)
                .Include(c => c.EmployeeUser)
                .Where(c => !c.IsDeleted && (centerId == null || c.CenterId == centerId))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var dtos = contracts.Select(MapToDto).ToList();
            return new Response<List<GetPayrollContractDto>>(dtos);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all payroll contracts");
            return new Response<List<GetPayrollContractDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetPaginatedAsync

    public async Task<PaginationResponse<List<GetPayrollContractDto>>> GetPaginatedAsync(PayrollContractFilter filter)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var query = context.PayrollContracts
                .Include(c => c.Mentor)
                .Include(c => c.EmployeeUser)
                .Where(c => !c.IsDeleted && (centerId == null || c.CenterId == centerId));

            if (filter.MentorId.HasValue)
                query = query.Where(c => c.MentorId == filter.MentorId);

            if (filter.EmployeeUserId.HasValue)
                query = query.Where(c => c.EmployeeUserId == filter.EmployeeUserId);

            if (filter.SalaryType.HasValue)
                query = query.Where(c => c.SalaryType == filter.SalaryType);

            if (filter.IsActive.HasValue)
                query = query.Where(c => c.IsActive == filter.IsActive);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                query = query.Where(c =>
                    (c.Mentor != null && c.Mentor.FullName.Contains(filter.Search)) ||
                    (c.EmployeeUser != null && c.EmployeeUser.FullName.Contains(filter.Search)));
            }

            var totalCount = await query.CountAsync();
            var contracts = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var dtos = contracts.Select(MapToDto).ToList();

            return new PaginationResponse<List<GetPayrollContractDto>>(dtos, totalCount, filter.PageNumber, filter.PageSize);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting paginated payroll contracts");
            return new PaginationResponse<List<GetPayrollContractDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region MapToDto

    private static GetPayrollContractDto MapToDto(PayrollContract c)
    {
        return new GetPayrollContractDto
        {
            Id = c.Id,
            MentorId = c.MentorId,
            MentorName = c.Mentor?.FullName,
            EmployeeUserId = c.EmployeeUserId,
            EmployeeName = c.EmployeeUser?.FullName,
            SalaryType = c.SalaryType,
            SalaryTypeDisplay = GetSalaryTypeDisplay(c.SalaryType),
            FixedAmount = c.FixedAmount,
            HourlyRate = c.HourlyRate,
            StudentPercentage = c.StudentPercentage,
            Description = c.Description,
            IsActive = c.IsActive,
            EffectiveFrom = c.EffectiveFrom,
            EffectiveTo = c.EffectiveTo,
            CreatedAt = c.CreatedAt
        };
    }

    #endregion

    #region GetSalaryTypeDisplay

    private static string GetSalaryTypeDisplay(SalaryType type)
    {
        return type switch
        {
            SalaryType.Fixed => Messages.Payroll.SalaryTypeFixed,
            SalaryType.Hourly => Messages.Payroll.SalaryTypeHourly,
            SalaryType.Percentage => Messages.Payroll.SalaryTypePercentage,
            SalaryType.Mixed => Messages.Payroll.SalaryTypeMixed,
            _ => Messages.Common.Unknown
        };
    }

    #endregion
}
