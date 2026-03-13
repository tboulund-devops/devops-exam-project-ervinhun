using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Dto;
using server.Utils;

namespace server.Controller;

[ApiController]
[Route("api/[controller]")]
public class TaskController(MyDbContext ctx) : ControllerBase
{
    [HttpGet("Users")]
    public async Task<IActionResult> GetUsers()
    {
        return Ok(await ctx.Users.ToListAsync());
    }


    [HttpGet(nameof(GetTasks))]
    public async Task<List<TaskDto>> GetTasks()
    {
        var tasks = await ctx.TaskItems
            .Include(t => t.Assignee)
            .Include(t => t.Status)
            .Where(t => t.DeletedAt == null)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                DeletedAt = t.DeletedAt,
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
            return NotFound($"Task not found with id: '{id}'");
        }

        return task;
    }

    [HttpPost(nameof(MoveTask))]
    public async Task<ActionResult<TaskDto>> MoveTask([FromBody] MoveTaskRequest request)
    {
        var task = await ctx.TaskItems
            .Include(t => t.Assignee)
            .Include(t => t.Status)
            .Where(t => t.Id == request.TaskId && t.DeletedAt == null)
            .FirstOrDefaultAsync();

        if (task == null)
        {
            throw new KeyNotFoundException("Task not found.");
        }

        var newStatus = await ctx.TodoTaskStatuses
            .FirstOrDefaultAsync(s => s.Id == request.NewStatusId);

        if (newStatus == null)
        {
            throw new KeyNotFoundException("New status not found.");
        }

        var oldStatus = task.Status;

        // Update task
        task.StatusId = newStatus.Id;
        task.Status = newStatus;
        await ctx.SaveChangesAsync();

        // Save history
        var saveHistory = new SaveTaskToHistory(ctx);
        await saveHistory.OnStatusChange(task, oldStatus.Id, newStatus.Id, request.ChangedByUserId);

        return Ok(MapToTaskDto(task));
    }

    [HttpPost(nameof(CreateTask))]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskRequest request)
    {
        var defaultStatus = await ctx.TodoTaskStatuses.Where(s => s.Name == "Backlog")
            .FirstOrDefaultAsync();
        if (defaultStatus == null)
        {
            var backlogStatus = new TodoTaskStatus()
            {
                Name = "Backlog",
                CreatedAt = DateTime.UtcNow,
                DeletedAt = null
            };
            await ctx.TodoTaskStatuses.AddAsync(backlogStatus);
            await ctx.SaveChangesAsync();
            defaultStatus = backlogStatus;
        }

        User? user = null;
        if (request.AssigneeId != null)
        {
            user = await ctx.Users.Where(u => u.Id == request.AssigneeId && u.DeletedAt == null).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound($"Assignee not found with id: '{request.AssigneeId}'");
            }
        }

        var newTask = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            AssigneeId = request.AssigneeId,
            StatusId = defaultStatus.Id,
            Status = defaultStatus,
            Assignee = user
        };

        //For the history set the uploading user for 'system' at the moment, as we don't have auth yet

        var systemUser = await GetSystemUserBeforeWeImplementAuthentication();
        await ctx.TaskItems.AddAsync(newTask);
        await ctx.SaveChangesAsync();
        var saveHistory = new SaveTaskToHistory(ctx);
        await saveHistory.OnCreate(newTask, systemUser.Id);
        return CreatedAtAction(nameof(GetTaskById), new { id = newTask.Id }, MapToTaskDto(newTask));
    }

    [HttpPut(nameof(UpdateTask))]
    public async Task<ActionResult<TaskDto>> UpdateTask([FromQuery] string id, [FromBody] UpdateTaskRequest request)
    {
        if (!Guid.TryParse(id, out var taskId))
        {
            return BadRequest("Invalid task id.");
        }

        var task = await ctx.TaskItems
            .Include(t => t.Assignee)
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.DeletedAt == null);

        if (task == null)
        {
            return NotFound();
        }

        var oldTask = new TaskItem()
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            StatusId = task.StatusId,
            AssigneeId = task.AssigneeId,
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DeletedAt = task.DeletedAt,
        };
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Title is required.");
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();

        if (request.AssigneeId.HasValue)
        {
            // Only change the assignee if the client explicitly provided a value.
            // Guid.Empty is treated as an explicit "unassign" request.
            if (request.AssigneeId == task.AssigneeId)
            {
                // No change in assignee requested.
            }
            else if (request.AssigneeId == Guid.Empty)
            {
                // Explicitly unassign the task.
                task.AssigneeId = null;
                task.Assignee = null;
            }
            else
            {
                var user = await ctx.Users
                    .FirstOrDefaultAsync(u => u.Id == request.AssigneeId && u.DeletedAt == null);
                if (user == null)
                {
                    return NotFound("User not found with id: " + request.AssigneeId);
                }
                task.AssigneeId = request.AssigneeId;
                task.Assignee = user;
            }
        }

        await ctx.SaveChangesAsync();
        var saveHistory = new SaveTaskToHistory(ctx);
        var systemUser = await GetSystemUserBeforeWeImplementAuthentication();
        await saveHistory.OnUpdate(oldTask, request, systemUser.Id);
        return Ok(MapToTaskDto(task));
    }

    [HttpDelete(nameof(DeleteTask))]
    public async Task<IActionResult> DeleteTask([FromQuery] string id)
    {
        if (!Guid.TryParse(id, out var taskId))
            return BadRequest("Invalid task id.");

        var task = await ctx.TaskItems
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound();

        if (task.DeletedAt != null)
            return BadRequest("Task is already deleted.");

        task.DeletedAt = DateTime.UtcNow;

        await ctx.SaveChangesAsync();
        var saveHistory = new SaveTaskToHistory(ctx);
        var systemUser = await GetSystemUserBeforeWeImplementAuthentication();
        await saveHistory.OnDelete(task.Id, systemUser.Id, task.DeletedAt.Value);
        return NoContent(); // 204
    }

    private TaskDto MapToTaskDto(TaskItem task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DeletedAt = task.DeletedAt,
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

    private async Task<User> GetSystemUserBeforeWeImplementAuthentication()
    {
        var systemUser = await ctx.Users.FirstOrDefaultAsync(u => u.Username == "system" && u.DeletedAt == null);
        return systemUser ?? throw new KeyNotFoundException("System user not found.");
    }
}