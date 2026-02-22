using System;
using System.Collections.Generic;
using NpgsqlTypes;

namespace server.DataAccess;

public partial class TaskItem
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public Guid StatusId { get; set; }

    public Guid? AssigneeId { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public NpgsqlTsVector? SearchVector { get; set; }

    public virtual User? Assignee { get; set; }

    public virtual TodoTaskStatus Status { get; set; } = null!;

    public virtual ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();

    public virtual ICollection<TaskHistory> TaskHistories { get; set; } = new List<TaskHistory>();
}
