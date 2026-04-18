namespace server.Dto;

public class UpdateTaskRequest
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public Guid? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
}