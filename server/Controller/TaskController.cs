using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Dto;
using server.Utils;

namespace server.Controller;

[ApiController]
public class TaskController(MyDbContext ctx) : ControllerBase
{
    [HttpGet(nameof(GetTasks))]
    public async Task<List<TaskDto>> GetTasks()
    {
        var tasks = await ctx.TaskItems
            .Include(t => t.Assignee)
            .Include(t => t.Status)
            .Where(t => t.DeletedAt == null)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                Status = t.Status.Name,
                Assignee = t.Assignee == null
                    ? null
                    : new UserDto
                    {
                        Id = t.Assignee.Id,
                        Username = t.Assignee.Username
                    }
            })
            .ToListAsync();
        return tasks;
    }

    [HttpGet(nameof(GetTaskById))]
    public async Task<ActionResult<TaskDto>> GetTaskById([FromQuery] string id)
    {
        if (!Guid.TryParse(id, out var taskId))
        {
            return BadRequest("Invalid task id.");
        }
        var task = await ctx.TaskItems
            .Include(t => t.Assignee)
            .Include(t => t.Status)
            .Where(t => t.Id == taskId && t.DeletedAt == null)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                Status = t.Status.Name,
                Assignee = t.Assignee == null
                    ? null
                    : new UserDto
                    {
                        Id = t.Assignee.Id,
                        Username = t.Assignee.Username
                    }
            })
            .FirstOrDefaultAsync();
        if (task == null)
        {
            return NotFound();
        }

        return task;
    }

[HttpPost(nameof(MoveTask))]
public async Task<ActionResult<TaskDto>> MoveTask([FromBody] MoveTaskRequest request)
{
    var task = await ctx.TaskItems
        .Include(t => t.Status)
        .Where(t => t.Id == request.TaskId && t.DeletedAt == null)
        .FirstOrDefaultAsync();

    if (task == null)
        return NotFound("Task not found.");

    var newStatus = await ctx.TodoTaskStatuses
        .FirstOrDefaultAsync(s => s.Id == request.NewStatusId);

    if (newStatus == null)
        return NotFound("New status not found.");

    var user = await ctx.Users
        .FirstOrDefaultAsync(u => u.Id == request.ChangedByUserId);

    if (user == null)
        return NotFound("User who changes the task not found.");

    var oldStatus = task.Status;

    if (oldStatus.Id == newStatus.Id)
        return BadRequest("Task is already in the requested status.");

    // Update task
    task.StatusId = newStatus.Id;
    task.Status = newStatus;
    await ctx.SaveChangesAsync();

    // Save history
    var saveHistory = new SaveTaskToHistory(ctx);
    await saveHistory.OnStatusChange(task, oldStatus.Id, newStatus.Id, user.Id);

    return Ok(MapToTaskDto(task));
}

    [HttpPost(nameof(CreateTask))]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskRequest request)
    {
        var toDoStatus = await ctx.TodoTaskStatuses.Where(s => s.Name == "To-do")
            .FirstOrDefaultAsync();
        if (toDoStatus == null)
        {
            throw new KeyNotFoundException("Status not found with name: To-do");
        }

        var user = await ctx.Users.Where(u => u.Id == request.AssigneeId).FirstOrDefaultAsync();
        if (user == null)
        {
            throw new KeyNotFoundException("User not found with id: " + request.AssigneeId);
        }

        var newTask = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            AssigneeId = request.AssigneeId,
            StatusId = toDoStatus.Id,
            Status = toDoStatus,
            Assignee = user
        };
        await ctx.TaskItems.AddAsync(newTask);
        await ctx.SaveChangesAsync();
        var saveHistory = new SaveTaskToHistory(ctx);
        await saveHistory.OnCreate(newTask, user.Id);
        return CreatedAtAction(nameof(GetTaskById), MapToTaskDto(newTask));
    }

    private TaskDto MapToTaskDto(TaskItem task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            Status = task.Status.Name,
            Assignee = task.Assignee == null
                ? null
                : new UserDto
                {
                    Id = task.Assignee.Id,
                    Username = task.Assignee.Username
                }
        };
    }
}