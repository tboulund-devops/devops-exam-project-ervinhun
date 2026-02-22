using System;
using System.Collections.Generic;

namespace server.DataAccess;

public partial class TaskComment
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public Guid? UserId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual TaskItem Task { get; set; } = null!;

    public virtual User? User { get; set; }
}
