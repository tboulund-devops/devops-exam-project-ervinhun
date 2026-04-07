namespace server.Dto;

public class TaskQueryParameters 
{
    public string? Status { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
    public Guid? AssigneeId { get; set; }
    
    public bool? HasAssignee { get; set; }
}