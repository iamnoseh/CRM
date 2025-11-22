using MediatR;
using Domain.Responses;
using Domain.DTOs.Student;

namespace Infrastructure.Features.Students.Commands.UpdateStudentPaymentStatus;

public record UpdateStudentPaymentStatusCommand(UpdateStudentPaymentStatusDto Dto) : IRequest<Response<string>>;
