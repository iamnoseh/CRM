using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IGroupActivationService
{
    /// <summary>
    /// Activates a group with the specified ID. This sets the status to Active,
    /// sets the StartDate to the current date, calculates EndDate based on DurationMonth,
    /// and ensures other group properties are properly initialized.
    /// </summary>
    /// <param name="groupId">The ID of the group to activate</param>
    /// <returns>A response indicating success or failure</returns>
    Task<Response<string>> ActivateGroupAsync(int groupId);
}
