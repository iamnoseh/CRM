using Domain.DTOs.Student;
using Domain.Filters;
using Domain.Responses;
using MediatR;

namespace Infrastructure.Features.Students.Queries.GetSimpleStudents;

public record GetSimpleStudentsQuery(StudentFilter Filter) : IRequest<PaginationResponse<List<GetSimpleDto>>>;
