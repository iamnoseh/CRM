using Domain.DTOs.Student;
using Domain.Responses;
using MediatR;

namespace Infrastructure.Features.Students.Queries.GetStudentById;

public record GetStudentByIdQuery(int Id) : IRequest<Response<GetStudentDto>>;
