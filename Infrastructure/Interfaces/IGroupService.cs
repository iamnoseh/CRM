using Domain.DTOs.Group;
using Domain.DTOs.Statistics;
using Domain.Filters;
using Domain.Responses;
using GroupAttendanceStatisticsDto = Domain.DTOs.Group.GroupAttendanceStatisticsDto;

namespace Infrastructure.Interfaces;

public interface IGroupService
{
    Task<Response<string>> CreateGroupAsync(CreateGroupDto request);
    Task<Response<string>> UpdateGroupAsync(int id, UpdateGroupDto request);
    Task<Response<string>> DeleteGroupAsync(int id);
    Task<Response<GetGroupDto>> GetGroupByIdAsync(int id);
    Task<Response<List<GetGroupDto>>> GetGroups();
    Task<PaginationResponse<List<GetGroupDto>>> GetGroupPaginated(GroupFilter filter);
    Task<Response<GroupAttendanceStatisticsDto>> GetGroupAttendanceStatisticsAsync(int groupId);
}