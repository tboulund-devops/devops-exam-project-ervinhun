namespace server.Dto;

public class CreateTaskRequest
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required Guid AssigneeId { get; set; }
}