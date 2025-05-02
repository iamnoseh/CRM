using Domain.DTOs.Group;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;

namespace Infrastructure.Services;

public class GroupService(DataContext context, string uploadPath) : IGroupService
{
    public Task<Response<string>> CreateGroupAsync(CreateGroupDto request)
    {
        throw new NotImplementedException();
    }

    public Task<Response<string>> UpdateGroupAsync(int id, UpdateGroupDto request)
    {
        throw new NotImplementedException();
    }

    public Task<Response<string>> DeleteGroupAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<GetGroupDto>> GetGroupByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<GetGroupDto>>> GetGroups()
    {
        throw new NotImplementedException();
    }

    public Task<PaginationResponse<List<GetGroupDto>>> GetGroupPaginated(GroupFilter filter)
    {
        throw new NotImplementedException();
    }
}