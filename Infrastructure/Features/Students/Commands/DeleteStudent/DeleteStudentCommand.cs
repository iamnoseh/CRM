using Domain.Responses;
using MediatR;

namespace Infrastructure.Features.Students.Commands.DeleteStudent;

public record DeleteStudentCommand(int Id) : IRequest<Response<string>>;
