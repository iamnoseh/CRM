using System.Net;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Helpers;

public static class UserManagementHelper
{
    public static async Task<Response<(User User, string Password, string Username)>> CreateUserAsync<T>(
        T createDto,
        UserManager<User> userManager,
        string role,
        Func<T, string> getUserNameOrPhoneNumber,
        Func<T, string> getEmail,
        Func<T, string> getFullName,
        Func<T, DateTime> getBirthday,
        Func<T, Gender> getGender,
        Func<T, string> getAddress,
        Func<T, int> getCenterId,
        Func<T, string> getProfileImagePath,
        bool usePhoneNumberAsUsername = true)
    {
        // Формирование имени пользователя
        string username = getUserNameOrPhoneNumber(createDto);
        if (usePhoneNumberAsUsername)
        {
            username = username.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");
            if (username.StartsWith("+"))
                username = username.Substring(1);
        }

        // Обеспечение уникальности имени пользователя
        var existingUser = await userManager.FindByNameAsync(username);
        int counter = 0;
        string originalUsername = username;
        while (existingUser != null)
        {
            counter++;
            username = originalUsername + counter;
            existingUser = await userManager.FindByNameAsync(username);
        }

        // Создание пользователя
        var user = new User
        {
            UserName = username,
            Email = getEmail(createDto),
            PhoneNumber = usePhoneNumberAsUsername ? getUserNameOrPhoneNumber(createDto) : null,
            FullName = getFullName(createDto),
            Birthday = getBirthday(createDto),
            Age = DateUtils.CalculateAge(getBirthday(createDto)),
            Gender = getGender(createDto),
            Address = getAddress(createDto),
            CenterId = getCenterId(createDto),
            ProfileImagePath = getProfileImagePath(createDto),
            ActiveStatus = ActiveStatus.Active
        };

        var password = PasswordUtils.GenerateRandomPassword();
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return new Response<(User, string, string)>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(result));

        // Назначение роли
        await userManager.AddToRoleAsync(user, role);

        return new Response<(User, string, string)>((user, password, username));
    }

    public static async Task<Response<string>> UpdateUserAsync<T>(
        User user,
        T updateDto,
        UserManager<User> userManager,
        Func<T, string> getEmail,
        Func<T, string> getFullName,
        Func<T, string> getPhoneNumber,
        Func<T, DateTime> getBirthday,
        Func<T, Gender> getGender,
        Func<T, string> getAddress,
        Func<T, ActiveStatus> getActiveStatus,
        Func<T, int> getCenterId,
        Func<T, PaymentStatus?> getPaymentStatus = null,
        Func<T, string> getProfileImagePath = null)
    {
        user.Email = getEmail(updateDto);
        user.FullName = getFullName(updateDto);
        user.PhoneNumber = getPhoneNumber(updateDto);
        user.Birthday = getBirthday(updateDto);
        user.Age = DateUtils.CalculateAge(getBirthday(updateDto));
        user.Gender = getGender(updateDto);
        user.Address = getAddress(updateDto);

        var activeStatus = getActiveStatus(updateDto);
        if (Enum.IsDefined(typeof(ActiveStatus), activeStatus))
        {
            user.ActiveStatus = activeStatus;
        }

        if (getPaymentStatus != null)
        {
            var paymentStatus = getPaymentStatus(updateDto);
            if (paymentStatus.HasValue && Enum.IsDefined(typeof(PaymentStatus), paymentStatus.Value))
            {
                user.PaymentStatus = paymentStatus.Value;
            }
        }
        
        if (getProfileImagePath != null)
            user.ProfileImagePath = getProfileImagePath(updateDto);
        user.UpdatedAt = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? new Response<string>(HttpStatusCode.OK, "User updated successfully")
            : new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(result));
    }
}