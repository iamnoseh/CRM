using Domain.DTOs.StudentGroup;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IStudentGroupService
{
    Task<Response<string>> CreateStudentGroupAsync(CreateStudentGroup request);
    Task<Response<string>> UpdateStudentGroupAsync(int id, UpdateStudentGroupDto request);
    Task<Response<string>> DeleteStudentGroupAsync(int id);
    Task<Response<GetStudentGroupDto>> GetStudentGroupByIdAsync(int id);
    Task<Response<List<GetStudentGroupDto>>> GetAllStudentGroupsAsync();
    Task<PaginationResponse<List<GetStudentGroupDto>>> GetStudentGroupsPaginated(StudentGroupFilter filter);
    Task<Response<List<GetStudentGroupDto>>> GetStudentGroupsByStudentAsync(int studentId);
    Task<Response<List<GetStudentGroupDto>>> GetStudentGroupsByGroupAsync(int groupId);
    
    Task<Response<string>> AddMultipleStudentsToGroupAsync(int groupId, List<int> studentIds);
    Task<Response<string>> RemoveStudentFromAllGroupsAsync(int studentId);

    Task<Response<string>> RemoveStudentFromGroup(int studentId, int groupId);
    Task<Response<List<GetStudentGroupDto>>> GetActiveStudentsInGroupAsync(int groupId);
    Task<Response<List<GetStudentGroupDto>>> GetInactiveStudentsInGroupAsync(int groupId);
    Task<Response<string>> ActivateStudentInGroupAsync(int studentId, int groupId);
    Task<Response<string>> DeactivateStudentInGroupAsync(int studentId, int groupId);
    Task<Response<string>> LeftStudentFromGroup(int studentId, int groupId, string leftReason);
    Task<Response<string>> ReverseLeftStudentFromGroup(int studentId, int groupId);
    Task<Response<List<LeftStudentDto>>> GetLeftStudentsInGroupAsync(int groupId);
    Task<Response<int>> GetStudentGroupCountAsync(int groupId);
    Task<Response<int>> GetStudentGroupsCountAsync(int studentId);
    Task<Response<string>> TransferStudentsGroupBulk(int sourceGroupId, int targetGroupId, List<int> studentIds);
}
