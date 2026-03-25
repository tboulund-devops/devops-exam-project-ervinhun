using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using System;
using System.Linq;

namespace server.Controller;

public sealed class TaskHistoryDto
{
    public DateTime ChangedAt { get; set; }
}

public sealed class TaskDetailHistoryDto
{
    public DateTime ChangedAt { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class HistoryController(MyDbContext ctx) : ControllerBase
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 500;

    [HttpGet(nameof(GetTaskHistory))]
    public async Task<ActionResult<List<TaskHistory>>> GetTaskHistory([FromQuery] int? pageNumber = null, [FromQuery] int? pageSize = null)
    {
        var query = ctx.TaskHistories
            .AsNoTracking()
            .OrderBy(t => t.ChangedAt)
            .AsQueryable();

        var page = pageNumber is > 0 ? pageNumber.Value : 1;
        var size = Math.Min(pageSize is > 0 ? pageSize.Value : DefaultPageSize, MaxPageSize);
        query = query.Skip((page - 1) * size).Take(size);

        var result = await query
            .ToListAsync();
        return Ok(result);
    }

    [HttpGet(nameof(GetTaskDetailHistory))]
    public async Task<ActionResult<List<TaskDetailHistory>>> GetTaskDetailHistory([FromQuery] int? pageNumber = null, [FromQuery] int? pageSize = null)
    {
        var query = ctx.TaskDetailHistories
            .AsNoTracking()
            .OrderBy(t => t.ChangedAt)
            .AsQueryable();

        var page = pageNumber is > 0 ? pageNumber.Value : 1;
        var size = Math.Min(pageSize is > 0 ? pageSize.Value : DefaultPageSize, MaxPageSize);
        query = query.Skip((page - 1) * size).Take(size);

        var result = await query
            .ToListAsync();
        return Ok(result);
    }
}