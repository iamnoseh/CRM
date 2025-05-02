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
    Task<PaginationResponse<List<GetStudentGroupDto>>> GetStudentGroupsPaginated(BaseFilter filter);
    Task<Response<List<GetStudentGroupDto>>> GetStudentGroupsByStudentAsync(int studentId);
    Task<Response<List<GetStudentGroupDto>>> GetStudentGroupsByGroupAsync(int groupId);
    
    // Операции с множеством записей
    Task<Response<string>> AddMultipleStudentsToGroupAsync(int groupId, List<int> studentIds);
    Task<Response<string>> RemoveStudentFromAllGroupsAsync(int studentId);
}
