using Domain.DTOs.Student;
using Domain.Filters;
using Domain.Responses;
using MediatR;

namespace Infrastructure.Features.Students.Queries.GetStudentsPagination;

public record GetStudentsPaginationQuery(StudentFilter Filter) : IRequest<PaginationResponse<List<GetStudentDto>>>;
