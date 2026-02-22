using System.Runtime.InteropServices.JavaScript;

namespace server.Dto;

public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = default!;
    public UserDto? Assignee { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = default!;
}