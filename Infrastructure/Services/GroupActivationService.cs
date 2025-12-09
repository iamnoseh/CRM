using System.Net;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Constants;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GroupActivationService(DataContext context, IHttpContextAccessor httpContextAccessor, IJournalService journalService) : IGroupActivationService
{
    #region ActivateGroupAsync

    public async Task<Response<string>> ActivateGroupAsync(int groupId)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Group.CenterIdNotFound);

            var group = await context.Groups
                .Include(g => g.Course)
                .ThenInclude(c => c!.Center)
                .FirstOrDefaultAsync(g => g.Id == groupId && g.Course != null && g.Course.CenterId == centerId);
                
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Group.NotFound);
            
            if (group.Status == ActiveStatus.Active)
                return new Response<string>(HttpStatusCode.BadRequest, "Группа уже активна");

            if (group.Status == ActiveStatus.Completed)
                return new Response<string>(HttpStatusCode.BadRequest, "Завершенные группы нельзя активировать повторно");

            var currentDate = DateTimeOffset.UtcNow;

            var endDate = currentDate.AddMonths(group.DurationMonth);

            var totalDays = (endDate - currentDate).TotalDays;
            var totalWeeks = (int)Math.Ceiling(totalDays / 7);

            group.Status = ActiveStatus.Active;
            group.StartDate = currentDate;
            group.EndDate = endDate;
            group.TotalWeeks = totalWeeks;
            group.CurrentWeek = 1;
            group.Started = true;
            
            context.Groups.Update(group);
            var result = await context.SaveChangesAsync();

            if (result > 0)
            {
                await journalService.GenerateWeeklyJournalAsync(group.Id, 1);
                return new Response<string>(HttpStatusCode.OK, "Группа успешно активирована");
            }
            
            return new Response<string>(HttpStatusCode.InternalServerError, "Не удалось активировать группу");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion
}
