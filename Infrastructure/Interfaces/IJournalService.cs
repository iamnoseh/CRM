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
}


