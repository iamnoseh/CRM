using Domain.DTOs.Journal;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IJournalService
{
    Task<Response<string>> GenerateWeeklyJournalAsync(int groupId, int weekNumber);
    Task<Response<GetJournalDto>> GetJournalAsync(int groupId, int weekNumber);
    Task<Response<string>> UpdateEntryAsync(int entryId, UpdateJournalEntryDto request);
}


