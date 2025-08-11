using Domain.DTOs.Journal;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IJournalService
{
    Task<Response<string>> GenerateWeeklyJournalAsync(int groupId, int weekNumber);
    Task<Response<GetJournalDto>> GetJournalAsync(int groupId, int weekNumber);
    Task<Response<GetJournalDto>> GetJournalByDateAsync(int groupId, DateTime dateLocal);
    Task<Response<GetJournalDto>> GetLatestJournalAsync(int groupId);
    Task<Response<string>> UpdateEntryAsync(int entryId, UpdateJournalEntryDto request);

    // Backfill current week's journal entries for newly added students so they don't lag behind
    Task<Response<string>> BackfillCurrentWeekForStudentAsync(int groupId, int studentId);
    Task<Response<string>> BackfillCurrentWeekForStudentsAsync(int groupId, IEnumerable<int> studentIds);

    // Cleanup: when a student is removed/deactivated, ensure no future entries remain for them
    Task<Response<string>> RemoveFutureEntriesForStudentAsync(int groupId, int studentId);
}


