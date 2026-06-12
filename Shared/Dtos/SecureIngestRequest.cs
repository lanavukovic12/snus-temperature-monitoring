namespace Shared.Dtos;

public class SecureIngestRequest
{
    public string SensorId { get; set; } = string.Empty;
    public long MessageId { get; set; }
    public DateTime SentAt { get; set; }
    public string Iv { get; set; } = string.Empty;
    public string Ciphertext { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}
