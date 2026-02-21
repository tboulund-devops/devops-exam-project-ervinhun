namespace server.Dto;

public class TasksResponseDto
{
    Guid id { get; set; }
    string title { get; set; }
    string description { get; set; }
    Guid assigneeId { get; set; }
    DateTime createdAt { get; set; }
    DateTime updatedAt { get; set; }
    DateTime? deletedAt { get; set; }
}