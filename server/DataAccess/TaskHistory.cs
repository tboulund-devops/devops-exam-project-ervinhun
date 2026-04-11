using System;
using System.Collections.Generic;

namespace server.DataAccess;

public partial class TaskHistory
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public Guid? FromStatusId { get; set; }

    public Guid? ToStatusId { get; set; }

    public Guid? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual User? ChangedByNavigation { get; set; }

    public virtual TodoTaskStatus? FromStatus { get; set; }

    public virtual TaskItem Task { get; set; } = null!;

    public virtual TodoTaskStatus? ToStatus { get; set; }
}
