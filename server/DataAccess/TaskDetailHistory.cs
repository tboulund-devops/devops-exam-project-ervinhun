using System;
using System.Collections.Generic;

namespace server.DataAccess;

public partial class TaskDetailHistory
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public string FieldName { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public Guid? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual User? ChangedByNavigation { get; set; }

    public virtual TaskItem Task { get; set; } = null!;
}
