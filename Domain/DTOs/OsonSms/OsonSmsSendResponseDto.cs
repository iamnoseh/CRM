using Newtonsoft.Json;

namespace Domain.DTOs.OsonSms;

public class OsonSmsSendResponseDto
{
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonProperty("txn_id")]
    public string TxnId { get; set; } = string.Empty;

    [JsonProperty("msg_id")]
    public string MsgId { get; set; } = string.Empty;

    [JsonProperty("smsc_msg_id")]
    public string SmscMsgId { get; set; } = string.Empty;

    [JsonProperty("smsc_msg_status")]
    public string SmscMsgStatus { get; set; } = string.Empty;

    [JsonProperty("smsc_msg_parts")]
    public int SmscMsgParts { get; set; }

    [JsonProperty("error")]
    public OsonSmsErrorDto? Error { get; set; }
}
