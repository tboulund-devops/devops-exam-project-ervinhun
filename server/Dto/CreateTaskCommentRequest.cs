namespace server.Dto;

public class CreateTaskCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
}