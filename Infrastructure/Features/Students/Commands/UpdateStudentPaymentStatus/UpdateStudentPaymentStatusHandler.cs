using System.Net;
using Domain.Responses;
using Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Features.Students.Commands.UpdateStudentPaymentStatus;

public class UpdateStudentPaymentStatusHandler(
    DataContext context,
    UserManager<User> userManager,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<UpdateStudentPaymentStatusCommand, Response<string>>
{
    public async Task<Response<string>> Handle(UpdateStudentPaymentStatusCommand request, CancellationToken cancellationToken)
    {
        var studentsQuery = context.Students
            .Where(s => !s.IsDeleted && s.Id == request.Dto.StudentId);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            studentsQuery, httpContextAccessor, s => s.CenterId);
        var student = await studentsQuery.FirstOrDefaultAsync(cancellationToken);

        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Студент не найден");

        student.PaymentStatus = request.Dto.Status;
        student.UpdatedAt = DateTime.UtcNow;
        context.Students.Update(student);

        if (student.UserId != null)
        {
            var user = await userManager.FindByIdAsync(student.UserId.ToString());
            if (user != null)
            {
                user.PaymentStatus = request.Dto.Status;
                await userManager.UpdateAsync(user);
            }
        }

        var res = await context.SaveChangesAsync(cancellationToken);
        return res > 0
            ? new Response<string>(HttpStatusCode.OK, "Статус оплаты успешно обновлен")
            : new Response<string>(HttpStatusCode.BadRequest, "Не удалось обновить статус оплаты");
    }
}
