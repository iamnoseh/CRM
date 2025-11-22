using System.Net;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Features.Students.Commands.UpdateStudentDocument;

public class UpdateStudentDocumentHandler(
    DataContext context,
    string uploadPath) : IRequestHandler<UpdateStudentDocumentCommand, Response<string>>
{
    public async Task<Response<string>> Handle(UpdateStudentDocumentCommand request, CancellationToken cancellationToken)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId && !s.IsDeleted, cancellationToken);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Студент не найден");

        if (request.DocumentFile == null)
            return new Response<string>(HttpStatusCode.BadRequest, "Файл документа обязателен");

        if (!string.IsNullOrEmpty(student.Document))
        {
            FileDeleteHelper.DeleteFile(student.Document, uploadPath);
        }

        var uploadResult = await FileUploadHelper.UploadFileAsync(request.DocumentFile, uploadPath, "student", "document");
        if (uploadResult.StatusCode != 200)
            return new Response<string>((HttpStatusCode)uploadResult.StatusCode, uploadResult.Message);

        student.Document = uploadResult.Data;
        student.UpdatedAt = DateTime.UtcNow;
        context.Students.Update(student);
        var res = await context.SaveChangesAsync(cancellationToken);

        return res > 0
            ? new Response<string>(HttpStatusCode.OK, "Документ студента успешно обновлен")
            : new Response<string>(HttpStatusCode.BadRequest, "Не удалось обновить документ студента");
    }
}
