using MediatR;
using Domain.Responses;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Features.Students.Commands.UpdateStudentDocument;

public record UpdateStudentDocumentCommand(int StudentId, IFormFile DocumentFile) : IRequest<Response<string>>;
