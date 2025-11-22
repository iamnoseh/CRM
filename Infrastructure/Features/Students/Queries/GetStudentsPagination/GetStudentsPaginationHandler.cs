using Domain.DTOs.Student;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Infrastructure.Features.Students.Queries.GetStudentsPagination;

public class GetStudentsPaginationHandler(DataContext context, IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetStudentsPaginationQuery, PaginationResponse<List<GetStudentDto>>>
{
    public async Task<PaginationResponse<List<GetStudentDto>>> Handle(GetStudentsPaginationQuery request,
        CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        
        Log.Information("Получение студентов с пагинацией: Страница {PageNumber}, Размер {PageSize}", 
            filter.PageNumber, filter.PageSize);
        
        var studentsQuery = context.Students
            .Where(s => !s.IsDeleted)
            .Include(s => s.User)  
            .Include(s => s.StudentGroups)
            .ThenInclude(sg => sg.Group)
            .AsQueryable();
            
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            studentsQuery, httpContextAccessor, s => s.CenterId);

        if (!string.IsNullOrEmpty(filter.FullName))
            studentsQuery = studentsQuery.Where(s => s.FullName.ToLower().Contains(filter.FullName.ToLower()));

        if (!string.IsNullOrEmpty(filter.Email))
            studentsQuery = studentsQuery.Where(s => s.Email.ToLower().Contains(filter.Email.ToLower()));

        if (!string.IsNullOrEmpty(filter.PhoneNumber))
            studentsQuery = studentsQuery.Where(s => s.PhoneNumber.ToLower().Contains(filter.PhoneNumber.ToLower()));

        if (filter.CenterId.HasValue)
            studentsQuery = studentsQuery.Where(s => s.CenterId == filter.CenterId.Value);

        var currentMentorId = UserContextHelper.GetCurrentUserMentorId(httpContextAccessor);

        if (filter.MentorId.HasValue)
        {
            studentsQuery = studentsQuery.Where(s =>
                s.StudentGroups.Any(sg => sg.Group.MentorId == filter.MentorId.Value && !sg.IsDeleted));
        }
        else if (currentMentorId.HasValue)
        {
            studentsQuery = studentsQuery.Where(s =>
                s.StudentGroups.Any(sg => sg.Group.MentorId == currentMentorId.Value && !sg.IsDeleted));
        }

        if (filter.Active.HasValue)
            studentsQuery = studentsQuery.Where(s => s.ActiveStatus == filter.Active.Value);

        if (filter.PaymentStatus.HasValue)
            studentsQuery = studentsQuery.Where(s => s.PaymentStatus == filter.PaymentStatus.Value);

        var totalRecords = await studentsQuery.CountAsync(cancellationToken);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        
        var students = await studentsQuery
            .OrderBy(s => s.Id)
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(s => new GetStudentDto
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                Address = s.Address,
                Phone = s.PhoneNumber,
                Birthday = s.Birthday,
                Age = s.Age,
                Gender = s.Gender,
                ActiveStatus = s.ActiveStatus,
                PaymentStatus = s.PaymentStatus,
                ImagePath = s.User != null ? s.User.ProfileImagePath : s.ProfileImage,  
                UserId = s.UserId,
                CenterId = s.CenterId
            })
            .ToListAsync(cancellationToken);

        Log.Information("Получено {Count} студентов из {Total}", students.Count, totalRecords);

        return new PaginationResponse<List<GetStudentDto>>(
            students,
            totalRecords,
            filter.PageNumber,
            filter.PageSize);
    }
}
