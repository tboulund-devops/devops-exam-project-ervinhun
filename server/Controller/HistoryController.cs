using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;

namespace server.Controller;

[ApiController]
[Route("api/[controller]")]
public class HistoryController(MyDbContext ctx) : ControllerBase
{
    [HttpGet(nameof(GetTaskHistory))]
    public async Task<ActionResult<List<TaskHistory>>> GetTaskHistory()
    {
        return Ok(await ctx.TaskHistories.OrderBy(t => t.ChangedAt).ToListAsync());
    }

    [HttpGet(nameof(GetTaskDetailHistory))]
    public async Task<ActionResult<List<TaskDetailHistory>>> GetTaskDetailHistory()
    {
        return Ok(await ctx.TaskDetailHistories.OrderBy(t => t.ChangedAt).ToListAsync());
    }
}