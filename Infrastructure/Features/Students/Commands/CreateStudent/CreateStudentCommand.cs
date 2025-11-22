using Domain.DTOs.Student;
using Domain.Responses;
using MediatR;

namespace Infrastructure.Features.Students.Commands.CreateStudent;

public record CreateStudentCommand(CreateStudentDto Dto) : IRequest<Response<string>>;
