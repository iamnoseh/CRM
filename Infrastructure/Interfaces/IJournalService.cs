using Domain.DTOs.Journal;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IJournalService
{
    Task<Response<string>> GenerateWeeklyJournalAsync(int groupId, int weekNumber);
    Task<Response<string>> GenerateWeeklyJournalFromCustomDateAsync(int groupId, DateTime startDate);
    Task<Response<GetJournalDto>> GetJournalAsync(int groupId, int weekNumber);
    Task<Response<GetJournalDto>> GetJournalByDateAsync(int groupId, DateTime dateLocal);
    Task<Response<GetJournalDto>> GetLatestJournalAsync(int groupId);
    Task<Response<string>> UpdateEntryAsync(int entryId, UpdateJournalEntryDto request);
    Task<Response<string>> BackfillCurrentWeekForStudentAsync(int groupId, int studentId);
    Task<Response<string>> BackfillCurrentWeekForStudentsAsync(int groupId, IEnumerable<int> studentIds);
    Task<Response<string>> RemoveFutureEntriesForStudentAsync(int groupId, int studentId);
    Task<Response<List<StudentWeekTotalsDto>>> GetStudentWeekTotalsAsync(int groupId, int weekNumber);
    Task<Response<GroupWeeklyTotalsDto>> GetGroupWeeklyTotalsAsync(int groupId, int? weekId = null);
    Task<Response<GroupPassStatsDto>> GetGroupPassStatsAsync(int groupId, decimal threshold);
    Task<Response<List<int>>> GetGroupWeekNumbersAsync(int groupId);
    Task<Response<string>> DeleteJournalAsync(int groupId, int weekNumber);
    Task<Response<string>> DeleteAllJournalsAsync(int groupId);
    Task<Response<List<StudentCommentDto>>> GetStudentCommentsAsync(int studentId);
}


