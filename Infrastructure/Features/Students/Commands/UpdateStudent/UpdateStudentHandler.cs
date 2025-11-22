using Domain.DTOs.Student;
using Domain.Entities;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Infrastructure.Features.Students.Commands.UpdateStudent;

public class UpdateStudentHandler(
    DataContext context,
    UserManager<User> userManager,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<UpdateStudentCommand, Response<string>>
{
    public async Task<Response<string>> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var id = request.Id;
            var studentDto = request.Dto;

            var student = await context.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (student == null) 
                return new Response<string>(HttpStatusCode.NotFound, "Студент не найден");

            var user = await userManager.FindByIdAsync(student.UserId.ToString());
            if (user == null) 
                return new Response<string>(HttpStatusCode.NotFound, "Пользователь не найден");

            // Check for duplicate email/phone if changed
            if (student.Email != studentDto.Email)
            {
                if (await userManager.FindByEmailAsync(studentDto.Email) != null)
                    return new Response<string>(HttpStatusCode.Conflict, "Email уже существует");
            }

            // Update User
            user.FullName = studentDto.FullName;
            user.Email = studentDto.Email;
            user.UserName = studentDto.Email;
            user.PhoneNumber = studentDto.PhoneNumber;
            user.Birthday = studentDto.Birthday;
            user.Gender = studentDto.Gender;
            user.Address = studentDto.Address;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return new Response<string>(HttpStatusCode.InternalServerError, string.Join(", ", updateResult.Errors.Select(e => e.Description)));

            // Update Student
            student.FullName = studentDto.FullName;
            student.Email = studentDto.Email;
            student.PhoneNumber = studentDto.PhoneNumber;
            student.Birthday = studentDto.Birthday;
            student.Age = DateUtils.CalculateAge(studentDto.Birthday);
            student.Gender = studentDto.Gender;
            student.Address = studentDto.Address;

            await context.SaveChangesAsync(cancellationToken);

            return new Response<string>(HttpStatusCode.OK, "Студент успешно обновлен");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
