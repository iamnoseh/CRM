using Domain.DTOs.Group;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IGroupService
{
    Task<Response<string>> CreateGroupAsync(CreateGroupDto request);
    Task<Response<string>> DeleteGroupAsync(int id);
    Task<Response<string>> UpdateGroupAsync(int id, UpdateGroupDto request);
    Task<Response<GetGroupDto>> GetGroupByIdAsync(int id);
    Task<Response<List<GetGroupDto>>> GetGroups();
    Task<PaginationResponse<List<GetGroupDto>>> GetGroupPaginated(GroupFilter filter);
}