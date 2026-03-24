using System;
using System.Collections.Generic;

namespace server.DataAccess;

public partial class TodoTaskStatus
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<TaskHistory> TaskHistoryFromStatuses { get; set; } = new List<TaskHistory>();

    public virtual ICollection<TaskHistory> TaskHistoryToStatuses { get; set; } = new List<TaskHistory>();

    public virtual ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
}
