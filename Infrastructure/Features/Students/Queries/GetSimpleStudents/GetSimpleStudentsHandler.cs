using System.Net;
using Domain.DTOs.Student;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Features.Students.Queries.GetSimpleStudents;

public class GetSimpleStudentsHandler(DataContext context, IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetSimpleStudentsQuery, PaginationResponse<List<GetSimpleDto>>>
{
    public async Task<PaginationResponse<List<GetSimpleDto>>> Handle(GetSimpleStudentsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var filter = request.Filter;
            var query = context.Students.Where(s => !s.IsDeleted);
            query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(query, httpContextAccessor, s => s.CenterId);

            if (!string.IsNullOrEmpty(filter.FullName))
                query = query.Where(s => s.FullName.ToLower().Contains(filter.FullName.ToLower()));

            if (!string.IsNullOrEmpty(filter.PhoneNumber))
                query = query.Where(s => s.PhoneNumber.ToLower().Contains(filter.PhoneNumber.ToLower()));

            if (!string.IsNullOrEmpty(filter.Email))
                query = query.Where(s => s.Email.ToLower().Contains(filter.Email.ToLower()));

            if (filter.Active.HasValue)
                query = query.Where(s => s.ActiveStatus == filter.Active.Value);

            if (filter.PaymentStatus.HasValue)
                query = query.Where(s => s.PaymentStatus == filter.PaymentStatus.Value);

            if (filter.Gender.HasValue)
                query = query.Where(s => s.Gender == filter.Gender.Value);

            if (filter.MinAge.HasValue)
                query = query.Where(s => s.Age >= filter.MinAge.Value);

            if (filter.MaxAge.HasValue)
                query = query.Where(s => s.Age <= filter.MaxAge.Value);

            if (filter.CenterId.HasValue)
                query = query.Where(s => s.CenterId == filter.CenterId.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(s =>
                    s.ActiveStatus == (filter.IsActive.Value ? ActiveStatus.Active : ActiveStatus.Inactive));

            if (filter.JoinedDateFrom.HasValue)
                query = query.Where(s => s.CreatedAt >= filter.JoinedDateFrom.Value);

            if (filter.JoinedDateTo.HasValue)
                query = query.Where(s => s.CreatedAt <= filter.JoinedDateTo.Value);

            if (filter.GroupId.HasValue)
            {
                query = query.Where(s =>
                    s.StudentGroups.Any(sg => sg.GroupId == filter.GroupId.Value && !sg.IsDeleted));
            }

            if (filter.MentorId.HasValue)
            {
                query = query.Where(s =>
                    s.StudentGroups.Any(sg => sg.Group.MentorId == filter.MentorId.Value && !sg.IsDeleted));
            }

            if (filter.CourseId.HasValue)
            {
                query = query.Where(s =>
                    s.StudentGroups.Any(sg => sg.Group!.CourseId == filter.CourseId.Value && !sg.IsDeleted));
            }

            var totalRecords = await query.CountAsync(cancellationToken);
            var skip = (filter.PageNumber - 1) * filter.PageSize;

            var students = await query
                .OrderBy(s => s.FullName)
                .Skip(skip)
                .Take(filter.PageSize)
                .Select(s => new GetSimpleDto
                {
                    Id = s.Id,
                    FullName = s.FullName
                })
                .ToListAsync(cancellationToken);

            return new PaginationResponse<List<GetSimpleDto>>(
                students,
                totalRecords,
                filter.PageNumber,
                filter.PageSize);
        }
        catch
        {
            return new PaginationResponse<List<GetSimpleDto>>(HttpStatusCode.InternalServerError,
                "Хатогӣ рух дод");
        }
    }
}
