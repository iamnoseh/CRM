using Domain.DTOs.MentorGroup;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IMentorGroupService
{
    Task<Response<string>> CreateMentorGroupAsync(CreateMentorGroupDto request);
    Task<Response<string>> UpdateMentorGroupAsync(int id, UpdateMentorGroupDto request);
    Task<Response<string>> DeleteMentorGroupAsync(int id);
    Task<Response<GetMentorGroupDto>> GetMentorGroupByIdAsync(int id);
    Task<Response<List<GetMentorGroupDto>>> GetAllMentorGroupsAsync();
    Task<PaginationResponse<List<GetMentorGroupDto>>> GetMentorGroupsPaginated(BaseFilter filter);
    Task<Response<List<GetMentorGroupDto>>> GetMentorGroupsByMentorAsync(int mentorId);
    Task<Response<List<GetMentorGroupDto>>> GetMentorGroupsByGroupAsync(int groupId);
    Task<Response<string>> AddMultipleMentorsToGroupAsync(int groupId, List<int> mentorIds);
    Task<Response<string>> RemoveMentorFromAllGroupsAsync(int mentorId);
}
