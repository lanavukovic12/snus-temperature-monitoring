namespace IngestionService.Options;

public class NotificationOptions
{
    public const string SectionName = "NotificationService";

    public string BaseUrl { get; set; } = "http://localhost:5117";
    public bool Enabled { get; set; } = true;
}
