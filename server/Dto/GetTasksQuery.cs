namespace server.Dto;

public class GetTasksQuery
{
    public string? Status { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
}