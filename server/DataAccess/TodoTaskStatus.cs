using System.ComponentModel.DataAnnotations;

namespace server.DataAccess;

public class TodoTaskStatus
{
    public Guid Id { get; set; }

    [Length(1, 255)]
    public required string Name { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<TaskHistory> TaskHistoryFromStatuses { get; set; } = new List<TaskHistory>();

    public virtual ICollection<TaskHistory> TaskHistoryToStatuses { get; set; } = new List<TaskHistory>();

    public virtual ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
}