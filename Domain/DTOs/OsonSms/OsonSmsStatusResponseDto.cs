using Newtonsoft.Json;

namespace Domain.DTOs.OsonSms;

public class OsonSmsStatusResponseDto
{
    [JsonProperty("message_id")]
    public string MessageId { get; set; } = string.Empty;

    [JsonProperty("final_date")]
    public OsonSmsFinalDateDto? FinalDate { get; set; }

    [JsonProperty("message_state_code")]
    public int MessageStateCode { get; set; }

    [JsonProperty("error_code")]
    public int ErrorCode { get; set; }

    [JsonProperty("message_state")]
    public string MessageState { get; set; } = string.Empty;

    [JsonProperty("error")]
    public OsonSmsErrorDto? Error { get; set; }
}

public class OsonSmsFinalDateDto
{
    [JsonProperty("date")]
    public DateTime Date { get; set; }

    [JsonProperty("timezone_type")]
    public int TimezoneType { get; set; }

    [JsonProperty("timezone")]
    public string Timezone { get; set; } = string.Empty;
}
