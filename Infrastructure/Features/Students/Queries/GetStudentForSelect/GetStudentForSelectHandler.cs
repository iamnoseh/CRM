using System.Net;
using Domain.DTOs.Student;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Features.Students.Queries.GetStudentForSelect;

public class GetStudentForSelectHandler(DataContext context, IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetStudentForSelectQuery, PaginationResponse<List<GetStudentForSelectDto>>>
{
    public async Task<PaginationResponse<List<GetStudentForSelectDto>>> Handle(GetStudentForSelectQuery request,
        CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        var studentsQuery = context.Students.Where(s => !s.IsDeleted);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            studentsQuery, httpContextAccessor, s => s.CenterId);

        if (!string.IsNullOrWhiteSpace(filter.FullName))
        {
            studentsQuery = studentsQuery
                .Where(s => EF.Functions.ILike(s.FullName, $"%{filter.FullName}%"));
        }

        var totalRecords = await studentsQuery.CountAsync(cancellationToken);
        var skip = (filter.PageNumber - 1) * filter.PageSize;

        var students = await studentsQuery
            .OrderBy(s => s.Id)
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(s => new GetStudentForSelectDto
            {
                Id = s.Id,
                FullName = s.FullName,
            })
            .ToListAsync(cancellationToken);

        if (students.Count == 0)
        {
            return new PaginationResponse<List<GetStudentForSelectDto>>(HttpStatusCode.NotFound, "Донишҷӯён ёфт нашуданд");
        }

        return new PaginationResponse<List<GetStudentForSelectDto>>(
            students,
            totalRecords,
            filter.PageNumber,
            filter.PageSize
        );
    }
}
