using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IGroupActivationService
{
    Task<Response<string>> ActivateGroupAsync(int groupId);
}
