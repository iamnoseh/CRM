using System.Net;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Features.Students.Commands.UpdateUserProfileImage;

public class UpdateUserProfileImageHandler(
    DataContext context,
    UserManager<User> userManager,
    string uploadPath) : IRequestHandler<UpdateUserProfileImageCommand, Response<string>>
{
    public async Task<Response<string>> Handle(UpdateUserProfileImageCommand request, CancellationToken cancellationToken)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId && !s.IsDeleted, cancellationToken);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Студент не найден");

        if (request.ProfileImage == null)
            return new Response<string>(HttpStatusCode.BadRequest, "Изображение профиля обязательно");

        if (!string.IsNullOrEmpty(student.ProfileImage))
        {
            FileDeleteHelper.DeleteFile(student.ProfileImage, uploadPath);
        }

        var uploadResult = await FileUploadHelper.UploadFileAsync(request.ProfileImage, uploadPath, "profiles", "profile");
        if (uploadResult.StatusCode != 200)
            return new Response<string>((HttpStatusCode)uploadResult.StatusCode, uploadResult.Message);

        student.ProfileImage = uploadResult.Data;
        student.UpdatedAt = DateTime.UtcNow;

        if (student.UserId != null)
        {
            var user = await userManager.FindByIdAsync(student.UserId.ToString());
            if (user != null)
            {
                user.ProfileImagePath = student.ProfileImage;
                await userManager.UpdateAsync(user);
            }
        }

        context.Students.Update(student);
        var res = await context.SaveChangesAsync(cancellationToken);

        return res > 0
            ? new Response<string>(HttpStatusCode.OK, "Фото профиля успешно обновлено")
            : new Response<string>(HttpStatusCode.BadRequest, "Не удалось обновить фото профиля");
    }
}
