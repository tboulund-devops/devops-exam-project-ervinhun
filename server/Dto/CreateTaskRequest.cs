using System.ComponentModel.DataAnnotations;

namespace server.Dto;

public class CreateTaskRequest
{
    [Required]
    [Length(1, 512)]
    public required string Title { get; set; }
    public string? Description { get; set; }
    public Guid? AssigneeId { get; set; }
}