using System.Net;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GroupActivationService(DataContext context) : IGroupActivationService
{

    public async Task<Response<string>> ActivateGroupAsync(int groupId)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Group not found");
            
            if (group.Status == ActiveStatus.Active)
                return new Response<string>(HttpStatusCode.BadRequest, "Group is already active");

            if (group.Status == ActiveStatus.Completed)
                return new Response<string>(HttpStatusCode.BadRequest, "Completed groups cannot be activated again");

            var currentDate = DateTimeOffset.Now;

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
                return new Response<string>(HttpStatusCode.OK, "Group activated successfully");
            
            return new Response<string>(HttpStatusCode.InternalServerError, "Failed to activate group");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
