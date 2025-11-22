using Domain.Responses;
using Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Infrastructure.Features.Students.Commands.DeleteStudent;

public class DeleteStudentHandler(DataContext context) : IRequestHandler<DeleteStudentCommand, Response<string>>
{
    public async Task<Response<string>> Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var student = await context.Students.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
            if (student == null) 
                return new Response<string>(HttpStatusCode.NotFound, "Студент не найден");

            student.IsDeleted = true;

            await context.SaveChangesAsync(cancellationToken);

            return new Response<string>(HttpStatusCode.OK, "Студент успешно удален");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
