namespace server.Dto;

public class MoveTaskRequest
{
    public Guid TaskId { get; set; }
    public Guid NewStatusId { get; set; }
    public Guid ChangedByUserId { get; set; }
}