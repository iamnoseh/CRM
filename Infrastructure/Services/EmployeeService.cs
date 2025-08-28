using Domain.DTOs.User.Employee;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Domain.Enums;
using Infrastructure.Services.EmailService;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly DataContext _context;
    private readonly UserManager<User> _userManager;
    private readonly string _uploadPath;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmployeeService(DataContext context, UserManager<User> userManager, string uploadPath, IEmailService emailService, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _uploadPath = uploadPath;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PaginationResponse<List<GetEmployeeDto>>> GetEmployeesAsync(EmployeeFilter filter)
    {
        var usersQuery = _context.Users.Where(u => !u.IsDeleted);
        usersQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            usersQuery, _httpContextAccessor, u => u.CenterId);
        var users = await usersQuery.ToListAsync();
        var employees = new List<GetEmployeeDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            if (!roles.Any(r => r == "Admin" || r == "Manager" || r == "SuperAdmin" || r == "User"))
                continue;
            if (filter.Id.HasValue && u.Id != filter.Id.Value) continue;
            if (!string.IsNullOrEmpty(filter.FullName) && (!u.FullName.Contains(filter.FullName))) continue;
            if (!string.IsNullOrEmpty(filter.PhoneNumber) && (u.PhoneNumber == null || !u.PhoneNumber.Contains(filter.PhoneNumber))) continue;
            if (filter.Age.HasValue && u.Age != filter.Age.Value) continue;
            if (filter.Gender.HasValue && u.Gender != filter.Gender.Value) continue;
            if (filter.Salary.HasValue && u.Salary != filter.Salary.Value) continue;
            if (filter.CenterId.HasValue && u.CenterId != filter.CenterId.Value) continue;
            employees.Add(new GetEmployeeDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Address = u.Address,
                PhoneNumber = u.PhoneNumber,
                Role = roles.FirstOrDefault(),
                Salary = u.Salary,
                Birthday = u.Birthday,
                Age = u.Age,
                Experience = u.Experience,
                Gender = u.Gender,
                ActiveStatus = u.ActiveStatus,
                PaymentStatus = u.PaymentStatus,
                ImagePath = u.ProfileImagePath,
                DocumentPath = u.DocumentPath,
                CenterId = u.CenterId
            });
        }
        return new PaginationResponse<List<GetEmployeeDto>>(employees, employees.Count, 1, employees.Count);
    }

    public async Task<Response<GetEmployeeDto>> GetEmployeeAsync(int employeeId)
    {
        var usersQuery = _context.Users.Where(x => x.Id == employeeId && !x.IsDeleted);
        usersQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            usersQuery, _httpContextAccessor, u => u.CenterId);
        var u = await usersQuery.FirstOrDefaultAsync();
        if (u == null)
            return new Response<GetEmployeeDto>(HttpStatusCode.NotFound, "Корманд ёфт нашуд");
        var roles = await _userManager.GetRolesAsync(u);
        var dto = new GetEmployeeDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Address = u.Address,
            PhoneNumber = u.PhoneNumber,
            Role = roles.FirstOrDefault(),
            Salary = u.Salary,
            Birthday = u.Birthday,
            Age = u.Age,
            Experience = u.Experience,
            Gender = u.Gender,
            ActiveStatus = u.ActiveStatus,
            PaymentStatus = u.PaymentStatus,
            ImagePath = u.ProfileImagePath,
            DocumentPath = u.DocumentPath,
            CenterId = u.CenterId
        };
        return new Response<GetEmployeeDto>(dto);
    }

    public async Task<Response<string>> CreateEmployeeAsync(CreateEmployeeDto request)
    {
        try
        {
            string imagePath = string.Empty;
            if (request.Image != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(request.Image, _uploadPath, "employee", "profile");
                if (imageResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);
                imagePath = imageResult.Data;
            }
            string documentPath = string.Empty;
            if (request.Document != null)
            {
                var docResult = await FileUploadHelper.UploadFileAsync(request.Document, _uploadPath, "employee", "document");
                if (docResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)docResult.StatusCode, docResult.Message);
                documentPath = docResult.Data;
            }
            int? safeCenterId = null;
            if (request.CenterId.HasValue)
            {
                var centerExists = await _context.Centers.AnyAsync(c => c.Id == request.CenterId.Value);
                if (centerExists)
                    safeCenterId = request.CenterId;
            }
            var userResult = await UserManagementHelper.CreateUserAsync(
                request,
                _userManager,
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
                return new Response<string>((HttpStatusCode)userResult.StatusCode, userResult.Message);
            var (user, password, username) = userResult.Data;
            user.Salary = request.Salary;
            user.Experience = request.Experience;
            user.Age = DateUtils.CalculateAge(request.Birthday);
            user.ActiveStatus = ActiveStatus.Active;
            user.PaymentStatus = PaymentStatus.Completed;
            user.DocumentPath = documentPath;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            if (!string.IsNullOrEmpty(request.Email))
            {
                await EmailHelper.SendLoginDetailsEmailAsync(
                    _emailService,
                    request.Email,
                    username,
                    password,
                    "Employee",
                    "#4776E6",
                    "#8E54E9");
            }
            return new Response<string>(HttpStatusCode.Created, $"Корманд бомуваффақият илова шуд. Маълумоти воридшавӣ ба email фиристода шуд. Username: {username}");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<string>> UpdateEmployeeAsync(UpdateEmployeeDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted);
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, "Корманд ёфт нашуд");
        if (request.Image != null)
        {
            if (!string.IsNullOrEmpty(user.ProfileImagePath))
                FileDeleteHelper.DeleteFile(user.ProfileImagePath, _uploadPath);
            var imageResult = await FileUploadHelper.UploadFileAsync(request.Image, _uploadPath, "employee", "profile");
            if (imageResult.StatusCode == 200)
                user.ProfileImagePath = imageResult.Data;
        }
        if (request.Document != null)
        {
            if (!string.IsNullOrEmpty(user.DocumentPath))
                FileDeleteHelper.DeleteFile(user.DocumentPath, _uploadPath);
            var docResult = await FileUploadHelper.UploadFileAsync(request.Document, _uploadPath, "employee", "document");
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
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? new Response<string>(HttpStatusCode.OK, "Маълумоти корманд тағйир ёфт")
            : new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(result));
    }

    public async Task<Response<string>> DeleteEmployeeAsync(int employeeId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == employeeId && !x.IsDeleted);
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, "Корманд ёфт нашуд");
        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? new Response<string>(HttpStatusCode.OK, "Корманд бо муваффақият нест шуд")
            : new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(result));
    }

    public async Task<Response<List<ManagerSelectDto>>> GetManagersForSelectAsync()
    {
        var usersQuery = _context.Users.Where(u => !u.IsDeleted);
        usersQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(usersQuery, _httpContextAccessor, u => u.CenterId);
        var users = await usersQuery.ToListAsync();
        var managers = new List<ManagerSelectDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            if (roles.Contains("Manager"))
            {
                managers.Add(new ManagerSelectDto
                {
                    Id = u.Id,
                    FullName = u.FullName 
                });
            }
        }
        return new Response<List<ManagerSelectDto>>(managers);
    }

    public async Task<Response<string>> UpdateEmployeePaymentStatusAsync(int employeeId, PaymentStatus status)
    {
        var usersQuery = _context.Users.Where(u => u.Id == employeeId && !u.IsDeleted);
        usersQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(usersQuery, _httpContextAccessor, u => u.CenterId);
        var user = await usersQuery.FirstOrDefaultAsync();
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, "Корманд ёфт нашуд");

        user.PaymentStatus = status;
        user.UpdatedAt = DateTime.UtcNow;
        var res = await _userManager.UpdateAsync(user);
        return res.Succeeded
            ? new Response<string>(HttpStatusCode.OK, "Ҳолати пардохти корманд навсозӣ шуд")
            : new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(res));
    }
}