using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Dto;
using server.Services;
using server.Utils;

namespace server.Controller;

[ApiController]
[Route("api/[controller]")]
public class TaskController(MyDbContext ctx, ITaskService taskService ) : ControllerBase
{
    private const string InvalidTaskIdMessage = "Invalid task id.";

    [HttpGet("Users")]
    public async Task<IActionResult> GetUsers()
    {
        return Ok(await ctx.Users.ToListAsync());
    }

    [HttpGet("Statuses")]
    public async Task<IActionResult> GetStatuses()
    {
        var statuses = await ctx.TodoTaskStatuses
            .Where(s => s.DeletedAt == null)
            .OrderBy(s => s.Name)
            .Select(s => new 
            {
                s.Id,
                s.Name,
                s.CreatedAt
            })
            .ToListAsync();
        
        return Ok(statuses);
    }

    [HttpGet(nameof(GetTaskById))]
    public async Task<ActionResult<TaskDto>> GetTaskById([FromQuery] string id)
    {
        if (!Guid.TryParse(id, out var taskId))
        {
            return BadRequest(InvalidTaskIdMessage);
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

        task.StatusId = newStatus.Id;
        task.Status = newStatus;
        await ctx.SaveChangesAsync();

        var saveHistory = new SaveTaskToHistory(ctx);
        await saveHistory.OnStatusChange(task, oldStatus.Id, newStatus.Id, request.ChangedByUserId);

        return Ok(MapToTaskDto(task));
    }

    [HttpPost(nameof(CreateTask))]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskRequest request)
    {
        var defaultStatus = await ctx.TodoTaskStatuses
            .Where(s => s.Name == "To-do")
            .FirstOrDefaultAsync();

        if (defaultStatus == null)
        {
            var todoStatus = new TodoTaskStatus()
            {
                Name = "To-do",
                CreatedAt = DateTime.UtcNow,
                DeletedAt = null
            };
            await ctx.TodoTaskStatuses.AddAsync(todoStatus);
            await ctx.SaveChangesAsync();
            defaultStatus = todoStatus;
        }

        User? user = null;
        if (request.AssigneeId != null)
        {
            user = await ctx.Users
                .Where(u => u.Id == request.AssigneeId && u.DeletedAt == null)
                .FirstOrDefaultAsync();
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
            return BadRequest(InvalidTaskIdMessage);
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

    [HttpGet(nameof(GetArchivedTasks))]
    public async Task<List<TaskDto>> GetArchivedTasks()
    {
        return await ctx.TaskItems
            .Include(t => t.Assignee)
            .Include(t => t.Status)
            .Where(t => t.DeletedAt != null)
            .OrderByDescending(t => t.DeletedAt)
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
    }

    [HttpPatch(nameof(ArchiveTask))]
    public async Task<IActionResult> ArchiveTask([FromQuery] string id)
    {
        if (!Guid.TryParse(id, out var taskId))
            return BadRequest(InvalidTaskIdMessage);

        var task = await ctx.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound();

        if (task.DeletedAt != null)
            return BadRequest("Task is already archived.");

        task.DeletedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        var saveHistory = new SaveTaskToHistory(ctx);
        var systemUser = await GetSystemUserBeforeWeImplementAuthentication();
        await saveHistory.OnDelete(task.Id, systemUser.Id, task.DeletedAt.Value);

        return NoContent();
    }

    [HttpPatch(nameof(UnarchiveTask))]
    public async Task<IActionResult> UnarchiveTask([FromQuery] string id)
    {
        if (!Guid.TryParse(id, out var taskId))
            return BadRequest(InvalidTaskIdMessage);

        var task = await ctx.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound();

        if (task.DeletedAt == null)
            return BadRequest("Task is not archived.");

        var archivedAt = task.DeletedAt.Value;
        task.DeletedAt = null;
        await ctx.SaveChangesAsync();

        var systemUser = await GetSystemUserBeforeWeImplementAuthentication();
        ctx.TaskDetailHistories.Add(new TaskDetailHistory
        {
            TaskId = task.Id,
            FieldName = "DeletedAt",
            OldValue = archivedAt.ToString("o"),
            NewValue = null,
            ChangedBy = systemUser.Id,
            ChangedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        return NoContent();
    }
        
    [HttpPatch(nameof(AssignTask))]
    public async Task<ActionResult<TaskDto>> AssignTask([FromQuery] string taskId, [FromQuery] string assigneeId)
    {
        if (!Guid.TryParse(taskId, out var parsedTaskId))
            return BadRequest("Invalid task id.");

        if (!Guid.TryParse(assigneeId, out var parsedAssigneeId))
            return BadRequest("Invalid assignee id.");

        var task = await ctx.TaskItems
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == parsedTaskId && t.DeletedAt == null);

        if (task == null)
            return NotFound($"Task not found with id: '{taskId}'");

        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == parsedAssigneeId && u.DeletedAt == null);
        if (user == null)
            return NotFound($"Assignee not found with id: '{assigneeId}'");

        task.AssigneeId = user.Id;
        task.Assignee = user;
        await ctx.SaveChangesAsync();

        return Ok(MapToTaskDto(task));
    }

    [HttpDelete(nameof(DeleteTask))]
    public async Task<IActionResult> DeleteTask([FromQuery] string id)
    {
        if (!Guid.TryParse(id, out var taskId))
            return BadRequest(InvalidTaskIdMessage);

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

    [HttpPost(nameof(ReopenTask))]
    public async Task<ActionResult<TaskDto>> ReopenTask([FromQuery] string id)
    {
        if (!Guid.TryParse(id, out var taskId))
            return BadRequest(InvalidTaskIdMessage);

        var task = await ctx.TaskItems
            .Include(t => t.Assignee)
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.DeletedAt == null);

        if (task == null)
            return NotFound();

        if (task.Status.Name != "Done")
            return BadRequest("Only tasks with status 'Done' can be reopened.");

        var todoStatus = await ctx.TodoTaskStatuses
            .FirstOrDefaultAsync(s => s.Name == "To-do" && s.DeletedAt == null);

        if (todoStatus == null)
            return StatusCode(500, "Status 'To-do' not found.");

        var oldStatus = task.Status;
        task.StatusId = todoStatus.Id;
        task.Status = todoStatus;
        await ctx.SaveChangesAsync();

        var saveHistory = new SaveTaskToHistory(ctx);
        var systemUser = await GetSystemUserBeforeWeImplementAuthentication();
        await saveHistory.OnStatusChange(task, oldStatus.Id, todoStatus.Id, systemUser.Id);

        return Ok(MapToTaskDto(task));
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
    
    [HttpGet(nameof(GetTasks))]
    public async Task<ActionResult<List<TaskDto>>> GetTasks([FromQuery] TaskQueryParameters query)
    {
        try
        {
            var tasks = await taskService.GetTasksByQueryAsync(query);
            return Ok(tasks);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}