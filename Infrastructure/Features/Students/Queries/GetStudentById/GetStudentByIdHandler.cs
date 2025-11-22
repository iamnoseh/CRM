using Domain.DTOs.Student;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Infrastructure.Features.Students.Queries.GetStudentById;

public class GetStudentByIdHandler(DataContext context, IHttpContextAccessor httpContextAccessor) 
    : IRequestHandler<GetStudentByIdQuery, Response<GetStudentDto>>
{
    public async Task<Response<GetStudentDto>> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
    {
        var studentsQuery = context.Students
            .Where(s => !s.IsDeleted && s.Id == request.Id);
            
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            studentsQuery, httpContextAccessor, s => s.CenterId);
            
        var student = await studentsQuery.FirstOrDefaultAsync(cancellationToken);

        if (student == null)
            return new Response<GetStudentDto>(HttpStatusCode.NotFound, "Студент не найден");

        var dto = new GetStudentDto
        {
            Id = student.Id,
            FullName = student.FullName,
            Email = student.Email,
            Address = student.Address,
            Phone = student.PhoneNumber,
            Birthday = student.Birthday,
            Age = student.Age,
            Gender = student.Gender,
            ActiveStatus = student.ActiveStatus,
            PaymentStatus = student.PaymentStatus,
            ImagePath = context.Users.Where(u => u.Id == student.UserId).Select(u => u.ProfileImagePath).FirstOrDefault() ?? student.ProfileImage,
            Document = student.Document,
            UserId = student.UserId,
            CenterId = student.CenterId
        };

        return new Response<GetStudentDto>(dto);
    }
}
