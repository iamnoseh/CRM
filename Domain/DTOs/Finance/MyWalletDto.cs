namespace Domain.DTOs.Finance;

public class MyWalletDto
{
    public GetStudentAccountDto Account { get; set; } = new();
    public List<GetAccountLogDto> LastLogs { get; set; } = new();
}


