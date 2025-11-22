using Domain.Responses;
using MediatR;

namespace Infrastructure.Features.Students.Queries.GetStudentDocument;

public record GetStudentDocumentQuery(int StudentId) : IRequest<Response<byte[]>>;
