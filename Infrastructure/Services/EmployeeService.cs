using Domain.DTOs.User.Employee;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Constants;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Domain.Enums;
using Infrastructure.Services.EmailService;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class EmployeeService(
    DataContext context,
    UserManager<User> userManager,
    string uploadPath,
    IEmailService emailService,
    IHttpContextAccessor httpContextAccessor,
    IOsonSmsService osonSmsService,
    IConfiguration configuration)
    : IEmployeeService
{
    #region GetEmployeesAsync

    public async Task<PaginationResponse<List<GetEmployeeDto>>> GetEmployeesAsync(EmployeeFilter filter)
    {
        try
        {
            var usersQuery = context.Users.Where(u => !u.IsDeleted);
            usersQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
                usersQuery, httpContextAccessor, u => u.CenterId);
            var users = await usersQuery.ToListAsync();
            var employees = new List<GetEmployeeDto>();
            foreach (var u in users)
            {
                var roles = await userManager.GetRolesAsync(u);
                if (!roles.Any(r => r == "Admin" || r == "Manager" || r == "SuperAdmin" || r == "User"))
                    continue;
                if (filter.Id.HasValue && u.Id != filter.Id.Value) continue;
                if (!string.IsNullOrEmpty(filter.FullName) && (!u.FullName.Contains(filter.FullName))) continue;
                if (!string.IsNullOrEmpty(filter.PhoneNumber) && (u.PhoneNumber == null || !u.PhoneNumber.Contains(filter.PhoneNumber))) continue;
                if (filter.Age.HasValue && u.Age != filter.Age.Value) continue;
                if (filter.Gender.HasValue && u.Gender != filter.Gender.Value) continue;
                if (filter.Salary.HasValue && u.Salary != filter.Salary.Value) continue;
                if (filter.CenterId.HasValue && u.CenterId != filter.CenterId.Value) continue;
                
                employees.Add(DtoMappingHelper.MapToGetEmployeeDto(u, roles.FirstOrDefault()));
            }
            return new PaginationResponse<List<GetEmployeeDto>>(employees, employees.Count, 1, employees.Count);
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetEmployeeDto>>(HttpStatusCode.InternalServerError, string.Format(Messages.Employee.GetListError, ex.Message));
        }
    }

    #endregion

    #region GetEmployeeAsync

    public async Task<Response<GetEmployeeDto>> GetEmployeeAsync(int employeeId)
    {
        try
        {
            var usersQuery = context.Users.Where(x => x.Id == employeeId && !x.IsDeleted);
            usersQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
                usersQuery, httpContextAccessor, u => u.CenterId);
            var u = await usersQuery.FirstOrDefaultAsync();
            if (u == null)
                return new Response<GetEmployeeDto>(HttpStatusCode.NotFound, Messages.Employee.NotFound);
            var roles = await userManager.GetRolesAsync(u);
            
            var dto = DtoMappingHelper.MapToGetEmployeeDto(u, roles.FirstOrDefault());
            return new Response<GetEmployeeDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetEmployeeDto>
            {
                Message = string.Format(Messages.Employee.GetError, ex.Message)
            };
        }
    }

    #endregion

    #region CreateEmployeeAsync

    public async Task<Response<string>> CreateEmployeeAsync(CreateEmployeeDto request)
    {
        try
        {
            string imagePath = string.Empty;
            if (request.Image != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(request.Image, uploadPath, "profiles", "profile");
                if (imageResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message!);
                imagePath = imageResult.Data;
            }
            string documentPath = string.Empty;
            if (request.Document != null)
            {
                var docResult = await FileUploadHelper.UploadFileAsync(request.Document, uploadPath, "employee", "document");
                if (docResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)docResult.StatusCode, docResult.Message!);
                documentPath = docResult.Data;
            }
            int? safeCenterId = null;
            if (request.CenterId.HasValue)
            {
                var centerExists = await context.Centers.AnyAsync(c => c.Id == request.CenterId.Value);
                if (centerExists)
                    safeCenterId = request.CenterId;
            }
            var userResult = await UserManagementHelper.CreateUserAsync(
                request,
                userManager,
                request.Role.ToString(),
                dto => dto.PhoneNumber,
                dto => dto.Email,
                dto => dto.FullName,
                dto => dto.Birthday,
                dto => dto.Gender,
                dto => dto.Address,
                _ => safeCenterId,
                _ => imagePath);
            if (userResult.StatusCode != 200)
                return new Response<string>((HttpStatusCode)userResult.StatusCode, userResult.Message!);
            var (user, password, username) = userResult.Data;
            user.Salary = request.Salary;
            user.Experience = request.Experience;
            user.Age = DateUtils.CalculateAge(request.Birthday);
            user.ActiveStatus = ActiveStatus.Active;
            user.PaymentStatus = PaymentStatus.Completed;
            user.DocumentPath = documentPath;
            user.UpdatedAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            if (request.Role == Role.Manager && safeCenterId.HasValue)
            {
                var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == safeCenterId.Value && !c.IsDeleted);
                if (center != null)
                {
                    center.ManagerId = user.Id;
                    center.UpdatedAt = DateTime.UtcNow;
                    context.Centers.Update(center);
                    await context.SaveChangesAsync();
                }
            }
            if (!string.IsNullOrEmpty(request.Email))
            {
                await EmailHelper.SendLoginDetailsEmailAsync(
                    emailService,
                    request.Email,
                    username,
                    password,
                    "Employee",
                    "#4776E6",
                    "#8E54E9");
            }

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                var loginUrl = configuration["AppSettings:LoginUrl"];
                var smsMessage = string.Format(Messages.Sms.WelcomeEmployee, user.FullName, username, password, loginUrl);
                await osonSmsService.SendSmsAsync(user.PhoneNumber, smsMessage);
            }

            return new Response<string>(HttpStatusCode.Created, string.Format(Messages.Employee.CreatedWithAuth, username));
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Employee.CreationError, ex.Message));
        }
    }

    #endregion

    #region UpdateEmployeeAsync

    public async Task<Response<string>> UpdateEmployeeAsync(UpdateEmployeeDto request)
    {
        try
        {
            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted);
            if (user == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Employee.NotFound);
            if (request.Image != null)
            {
                if (!string.IsNullOrEmpty(user.ProfileImagePath))
                    FileDeleteHelper.DeleteFile(user.ProfileImagePath, uploadPath);
                var imageResult = await FileUploadHelper.UploadFileAsync(request.Image, uploadPath, "profiles", "profile");
                if (imageResult.StatusCode == 200)
                    user.ProfileImagePath = imageResult.Data;
            }
            if (request.Document != null)
            {
                if (!string.IsNullOrEmpty(user.DocumentPath))
                    FileDeleteHelper.DeleteFile(user.DocumentPath, uploadPath);
                var docResult = await FileUploadHelper.UploadFileAsync(request.Document, uploadPath, "employee", "document");
                if (docResult.StatusCode == 200)
                    user.DocumentPath = docResult.Data;
            }
            user.FullName = request.FullName ?? user.FullName;
            user.Email = request.Email ?? user.Email;
            user.Address = request.Address ?? user.Address;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.Salary = request.Salary ?? user.Salary;
            user.Birthday = request.Birthday ?? user.Birthday;
            user.Age = request.Age ?? user.Age;
            user.Experience = request.Experience ?? user.Experience;
            user.Gender = request.Gender ?? user.Gender;
            user.ActiveStatus = request.ActiveStatus ?? user.ActiveStatus;
            user.PaymentStatus = request.PaymentStatus ?? user.PaymentStatus;
            user.CenterId = request.CenterId ?? user.CenterId;
            user.UpdatedAt = DateTime.UtcNow;
            var result = await userManager.UpdateAsync(user);
            return result.Succeeded
                ? new Response<string>(HttpStatusCode.OK, Messages.Employee.Updated)
                : new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(result));
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Employee.UpdateError, ex.Message));
        }
    }

    #endregion

    #region DeleteEmployeeAsync

    public async Task<Response<string>> DeleteEmployeeAsync(int employeeId)
    {
        try
        {
            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == employeeId && !x.IsDeleted);
            if (user == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Employee.NotFound);
            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;
            var result = await userManager.UpdateAsync(user);
            return result.Succeeded
                ? new Response<string>(HttpStatusCode.OK, Messages.Employee.Deleted)
                : new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(result));
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Employee.DeleteError, ex.Message));
        }
    }

    #endregion

    #region GetManagersForSelectAsync

    public async Task<Response<List<ManagerSelectDto>>> GetManagersForSelectAsync()
    {
        try
        {
            var usersQuery = context.Users.Where(u => !u.IsDeleted);
            usersQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(usersQuery, httpContextAccessor, u => u.CenterId);
            var users = await usersQuery.ToListAsync();
            var managers = new List<ManagerSelectDto>();
            foreach (var u in users)
            {
                var roles = await userManager.GetRolesAsync(u);
                if (roles.Contains("Manager"))
                {
                    managers.Add(DtoMappingHelper.MapToManagerSelectDto(u));
                }
            }
            return new Response<List<ManagerSelectDto>>(managers);
        }
        catch (Exception ex)
        {
            return new Response<List<ManagerSelectDto>>(HttpStatusCode.InternalServerError, string.Format(Messages.Employee.GetListError, ex.Message));
        }
    }

    #endregion

    #region UpdateEmployeePaymentStatusAsync

    public async Task<Response<string>> UpdateEmployeePaymentStatusAsync(int employeeId, PaymentStatus status)
    {
        try
        {
            var usersQuery = context.Users.Where(u => u.Id == employeeId && !u.IsDeleted);
            usersQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(usersQuery, httpContextAccessor, u => u.CenterId);
            var user = await usersQuery.FirstOrDefaultAsync();
            if (user == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Employee.NotFound);

            user.PaymentStatus = status;
            user.UpdatedAt = DateTime.UtcNow;
            var res = await userManager.UpdateAsync(user);
            return res.Succeeded
                ? new Response<string>(HttpStatusCode.OK, Messages.Common.Success)
                : new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(res));
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Employee.UpdateError, ex.Message));
        }
    }

    #endregion
}
