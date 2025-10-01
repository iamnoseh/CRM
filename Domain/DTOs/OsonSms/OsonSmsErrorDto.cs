using Newtonsoft.Json;

namespace Domain.DTOs.OsonSms;

public class OsonSmsErrorDto
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("msg")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }
}
