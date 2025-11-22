using Domain.DTOs.Student;
using Domain.Responses;
using MediatR;

namespace Infrastructure.Features.Students.Commands.UpdateStudent;

public record UpdateStudentCommand(int Id, UpdateStudentDto Dto) : IRequest<Response<string>>;
