using System.Net;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Features.Students.Queries.GetStudentDocument;

public class GetStudentDocumentHandler(DataContext context, IHttpContextAccessor httpContextAccessor, string uploadPath)
    : IRequestHandler<GetStudentDocumentQuery, Response<byte[]>>
{
    public async Task<Response<byte[]>> Handle(GetStudentDocumentQuery request, CancellationToken cancellationToken)
    {
        var studentsQuery = context.Students
            .Where(s => !s.IsDeleted && s.Id == request.StudentId);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            studentsQuery, httpContextAccessor, s => s.CenterId);
        var student = await studentsQuery.FirstOrDefaultAsync(cancellationToken);

        if (student == null)
            return new Response<byte[]>(HttpStatusCode.NotFound, "Донишҷӯ ёфт нашуд");

        return await FileUploadHelper.GetFileAsync(student.Document, uploadPath);
    }
}
