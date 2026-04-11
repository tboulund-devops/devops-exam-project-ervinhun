namespace server.Dto;

public class NotificationDto
{
    public Guid id { get; set; }
    public Guid userId { get; set; }
    public string message { get; set; }
    public bool isRead { get; set; }
    public DateTime createdAt { get; set; }
}

public class CreateNotificationDto
{
    public Guid userId { get; set; }
    public string message { get; set; }
}

