using System.Net;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GroupActivationService(DataContext context, IHttpContextAccessor httpContextAccessor, IJournalService journalService) : IGroupActivationService
{
    public async Task<Response<string>> ActivateGroupAsync(int groupId)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<string>(HttpStatusCode.BadRequest, "CenterId not found in token");

            var group = await context.Groups
                .Include(g => g.Course)
                .ThenInclude(c => c!.Center)
                .FirstOrDefaultAsync(g => g.Id == groupId && g.Course != null && g.Course.CenterId == centerId);
                
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Group not found or doesn't belong to your center");
            
            if (group.Status == ActiveStatus.Active)
                return new Response<string>(HttpStatusCode.BadRequest, "Group is already active");

            if (group.Status == ActiveStatus.Completed)
                return new Response<string>(HttpStatusCode.BadRequest, "Completed groups cannot be activated again");

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
                return new Response<string>(HttpStatusCode.OK, "Group activated successfully");
            }
            
            return new Response<string>(HttpStatusCode.InternalServerError, "Failed to activate group");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
