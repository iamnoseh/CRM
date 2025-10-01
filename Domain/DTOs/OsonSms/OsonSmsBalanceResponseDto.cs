using Newtonsoft.Json;

namespace Domain.DTOs.OsonSms;

public class OsonSmsBalanceResponseDto
{
    [JsonProperty("balance")]
    public decimal Balance { get; set; }

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonProperty("error")]
    public OsonSmsErrorDto? Error { get; set; }
}
