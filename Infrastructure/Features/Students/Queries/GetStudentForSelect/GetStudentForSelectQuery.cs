using Domain.DTOs.Student;
using Domain.Filters;
using Domain.Responses;
using MediatR;

namespace Infrastructure.Features.Students.Queries.GetStudentForSelect;

public record GetStudentForSelectQuery(StudentFilterForSelect Filter) : IRequest<PaginationResponse<List<GetStudentForSelectDto>>>;
