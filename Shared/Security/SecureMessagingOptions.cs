namespace Shared.Security;

public class SecureMessagingOptions
{
    public const string SectionName = "SecureMessaging";

    public bool Enabled { get; set; } = true;
    public int MaxClockSkewSeconds { get; set; } = 120;
    public string AesKeyBase64 { get; set; } = string.Empty;
    public RsaKeyOptions Rsa { get; set; } = new();
}
