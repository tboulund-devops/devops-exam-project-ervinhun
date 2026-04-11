using System.ComponentModel;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using server.DataAccess;
using server.Dto;
using Xunit.Abstractions;

namespace test;

public class TaskControllerTests(CustomWebApplicationFactory factory, ITestOutputHelper testOutputHelper)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    [DisplayName("GetAllTasks returns a Success status code")]
    public async Task GetAllTasks()
    {
        // Act
        var response = await _client.GetAsync("/api/Task/GetTasks");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    [DisplayName("GetTaskById with invalid id returns BadRequest")]
    public async Task GetTaskById_InvalidId()
    {
        // Act
        var response = await _client.GetAsync("/api/Task/GetTaskById?id=invalid-id");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("GetTaskById with non-existing id returns NotFound")]
    public async Task GetTaskById_NonExistingId()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.GetAsync($"/api/Task/GetTaskById?id={nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    [DisplayName("GetTaskById with valid id returns Success")]
    public async Task GetTaskById_ValidId()
    {
        // Arrange
        var newTask = new CreateTaskRequest
        {
            Title = "Test Task",
            Description = "This is a test task"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/Task/CreateTask", newTask);
        var error = await createResponse.Content.ReadAsStringAsync();
        testOutputHelper.WriteLine($"Status: {createResponse.StatusCode}, Error: {error}");

        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskDto>();

        if (createdTask == null)
        {
            throw new Exception($"Failed to create task for testing GetTaskById. Error: {error}");
        }

        // Act
        var response = await _client.GetAsync($"/api/Task/GetTaskById?id={createdTask.Id}");

        // Assert
        createResponse.IsSuccessStatusCode.Should().BeTrue($"because create should succeed, but got: {error}");
        createdTask.Should().NotBeNull("because the created task response should be deserializable");
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();
        var task = await response.Content.ReadFromJsonAsync<TaskDto>();
        task.Should().NotBeNull();
        task.Id.Should().Be(createdTask.Id);
    }

    [Fact]
    [DisplayName("CreateTask with valid data returns Success")]
    public async Task CreateTask_ValidData()
    {
        // Arrange
        var newTask = new CreateTaskRequest
        {
            Title = "Test Task2",
            Description = "This is a test task2"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Task/CreateTask", newTask);
        var error = await response.Content.ReadAsStringAsync();
        testOutputHelper.WriteLine($"Status: {response.StatusCode}, Error: {error}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue($"because create should succeed, but got: {error}");
        var createdTask = await response.Content.ReadFromJsonAsync<TaskDto>();
        createdTask.Should().NotBeNull();
        createdTask.Title.Should().Be(newTask.Title);
        createdTask.Description.Should().Be(newTask.Description);
        createdTask.Id.Should().NotBeEmpty();
    }

    [Fact]
    [DisplayName("CreateTask with missing title returns BadRequest")]
    public async Task CreateTask_MissingTitle()
    {
        // Arrange
        var newTask = new CreateTaskRequest
        {
            Title = "",
            Description = "This task has no title"
        };
        // Act
        var response = await _client.PostAsJsonAsync("/api/Task/CreateTask", newTask);
        var error = await response.Content.ReadAsStringAsync();
        testOutputHelper.WriteLine($"Status: {response.StatusCode}, Error: {error}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("CreateTask with invalid AssigneeId returns NotFound")]
    public async Task CreateTask_InvalidAssigneeId()
    {
        // Arrange
        var newTask = new CreateTaskRequest
        {
            Title = "Test Task with Invalid Assignee",
            Description = "This task has an invalid assignee",
            AssigneeId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Task/CreateTask", newTask);
        var error = await response.Content.ReadAsStringAsync();
        testOutputHelper.WriteLine($"Status: {response.StatusCode}, Error: {error}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        error.Should().Be(
            $"Assignee not found with id: '{newTask.AssigneeId}'",
            "because the error message should indicate the assignee was not found, but got: {0}",
            error);
    }

    [Fact]
    [DisplayName("AssignTask with invalid taskId returns BadRequest")]
    public async Task AssignTask_InvalidTaskId()
    {
        // Act
        var response = await _client.PatchAsync($"/api/Task/AssignTask?taskId=invalid-id&assigneeId={Guid.NewGuid()}", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("AssignTask with invalid assigneeId returns BadRequest")]
    public async Task AssignTask_InvalidAssigneeId()
    {
        // Act
        var response = await _client.PatchAsync($"/api/Task/AssignTask?taskId={Guid.NewGuid()}&assigneeId=invalid-id", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("AssignTask with non-existing task returns NotFound")]
    public async Task AssignTask_NonExistingTask()
    {
        // Act
        var response = await _client.PatchAsync($"/api/Task/AssignTask?taskId={Guid.NewGuid()}&assigneeId={Guid.NewGuid()}", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    [DisplayName("AssignTask with non-existing user returns NotFound")]
    public async Task AssignTask_NonExistingUser()
    {
        // Arrange - create a task first
        var createResponse = await _client.PostAsJsonAsync("/api/Task/CreateTask", new CreateTaskRequest
        {
            Title = "Task for AssignTask NonExistingUser test"
        });
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
        createdTask.Should().NotBeNull();

        // Act
        var response = await _client.PatchAsync($"/api/Task/AssignTask?taskId={createdTask!.Id}&assigneeId={Guid.NewGuid()}", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    [DisplayName("AssignTask with valid taskId and assigneeId returns Success with assignee set")]
    public async Task AssignTask_ValidData()
    {
        // Arrange - create a task (this also seeds the "system" user)
        var createResponse = await _client.PostAsJsonAsync("/api/Task/CreateTask", new CreateTaskRequest
        {
            Title = "Task for AssignTask test"
        });
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
        createdTask.Should().NotBeNull();

        // Get the seeded system user
        var usersResponse = await _client.GetAsync("/api/Task/Users");
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNullOrEmpty();
        var systemUser = users!.First(u => u.Username == "system");
        // Act
        var response = await _client.PatchAsync(
            $"/api/Task/AssignTask?taskId={createdTask!.Id}&assigneeId={systemUser.Id}", null);
        var error = await response.Content.ReadAsStringAsync();
        testOutputHelper.WriteLine($"Status: {response.StatusCode}, Body: {error}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue($"because assign should succeed, but got: {error}");
        var assignedTask = await response.Content.ReadFromJsonAsync<TaskDto>();
        assignedTask.Should().NotBeNull();
        assignedTask!.Assignee.Should().NotBeNull();
        assignedTask.Assignee!.Id.Should().Be(systemUser.Id);
        }

    [Fact]    
    [DisplayName("UpdateTask with valid data writes title and description history")]
    public async Task UpdateTask_ValidData_WritesHistory()
    {
        var createdTask = await CreateTaskOrThrow("Original Title", "Original Description");

        var updateRequest = new UpdateTaskRequest
        {
            Title = "  Updated Title  ",
            Description = "  Updated Description  "
        };

        var response = await _client.PutAsJsonAsync($"/api/Task/UpdateTask?id={createdTask.Id}", updateRequest);
        var error = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue($"because update should succeed, but got: {error}");

        var updatedTask = await response.Content.ReadFromJsonAsync<TaskDto>();
        updatedTask.Should().NotBeNull();
        updatedTask.Title.Should().Be("Updated Title");
        updatedTask.Description.Should().Be("Updated Description");

        var systemUserId = await GetSystemUserId();
        var detailHistory = await GetTaskDetailHistory(createdTask.Id);

        detailHistory.Should().ContainSingle(h =>
            h.FieldName == "Title" &&
            h.OldValue == "Original Title" &&
            h.NewValue == "Updated Title" &&
            h.ChangedBy == systemUserId);

        detailHistory.Should().ContainSingle(h =>
            h.FieldName == "Description" &&
            h.OldValue == "Original Description" &&
            h.NewValue == "Updated Description" &&
            h.ChangedBy == systemUserId);
    }

    [Fact]
    [DisplayName("MoveTask with valid data writes status history")]
    public async Task MoveTask_ValidData_WritesStatusHistory()
    {
        var createdTask = await CreateTaskOrThrow("Move me", "Move test");
        var fromStatusId = await GetTaskStatusId(createdTask.Id);
        var toStatusId = await EnsureStatus("In Progress");
        var changedBy = await GetSystemUserId();

        var moveRequest = new MoveTaskRequest
        {
            TaskId = createdTask.Id,
            NewStatusId = toStatusId,
            ChangedByUserId = changedBy
        };

        var response = await _client.PostAsJsonAsync("/api/Task/MoveTask", moveRequest);
        var error = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue($"because move should succeed, but got: {error}");

        var movedTask = await response.Content.ReadFromJsonAsync<TaskDto>();
        movedTask.Should().NotBeNull();
        movedTask.Status.Should().Be("In Progress");

        var statusHistory = await GetTaskHistory(createdTask.Id);
        statusHistory.Should().ContainSingle(h =>
            h.FromStatusId == fromStatusId &&
            h.ToStatusId == toStatusId &&
            h.ChangedBy == changedBy);
    }

    [Fact]
    [DisplayName("DeleteTask soft-deletes task and hides it from queries")]
    public async Task DeleteTask_ValidId_SoftDeletesTask()
    {
        var createdTask = await CreateTaskOrThrow("Delete me", "Delete test");

        var deleteResponse = await _client.DeleteAsync($"/api/Task/DeleteTask?id={createdTask.Id}");
        deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var byIdResponse = await _client.GetAsync($"/api/Task/GetTaskById?id={createdTask.Id}");
        byIdResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

        var tasksResponse = await _client.GetAsync("/api/Task/GetTasks");
        tasksResponse.IsSuccessStatusCode.Should().BeTrue();
        var tasks = await tasksResponse.Content.ReadFromJsonAsync<List<TaskDto>>();
        tasks.Should().NotBeNull();
        tasks.Should().NotContain(t => t.Id == createdTask.Id);
    }

    [Fact]
    [DisplayName("DeleteTask with valid id writes DeletedAt detail history entry")]
    public async Task DeleteTask_ValidId_WritesDeleteHistory()
    {
        var createdTask = await CreateTaskOrThrow("Delete history", "Delete history test");
        var systemUserId = await GetSystemUserId();

        var deleteResponse = await _client.DeleteAsync($"/api/Task/DeleteTask?id={createdTask.Id}");
        deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var detailHistory = await GetTaskDetailHistory(createdTask.Id);
        detailHistory.Should().ContainSingle(h =>
            h.FieldName == "DeletedAt" &&
            h.OldValue == null &&
            h.ChangedBy == systemUserId);

        var deleteEntry = detailHistory.Single(h => h.FieldName == "DeletedAt");
        deleteEntry.NewValue.Should().NotBeNullOrWhiteSpace();
        DateTimeOffset.TryParse(deleteEntry.NewValue, out _)
            .Should().BeTrue("because delete history NewValue should be an ISO timestamp");
    }

    [Fact]
    [DisplayName("DeleteTask with invalid id returns BadRequest")]
    public async Task DeleteTask_InvalidId_ReturnsBadRequest()
    {
        var deleteResponse = await _client.DeleteAsync("/api/Task/DeleteTask?id=invalid-id");

        deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var body = await deleteResponse.Content.ReadAsStringAsync();
        body.Should().Be("Invalid task id.");
    }

    [Fact]
    [DisplayName("DeleteTask with unknown id returns NotFound")]
    public async Task DeleteTask_UnknownId_ReturnsNotFound()
    {
        var deleteResponse = await _client.DeleteAsync($"/api/Task/DeleteTask?id={Guid.NewGuid()}");

        deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    [DisplayName("DeleteTask called twice returns BadRequest second time and does not duplicate delete history")]
    public async Task DeleteTask_Twice_NoDuplicateDeleteHistory()
    {
        var createdTask = await CreateTaskOrThrow("Delete twice", "Delete twice test");

        var firstDeleteResponse = await _client.DeleteAsync($"/api/Task/DeleteTask?id={createdTask.Id}");
        firstDeleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var secondDeleteResponse = await _client.DeleteAsync($"/api/Task/DeleteTask?id={createdTask.Id}");
        secondDeleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var detailHistory = await GetTaskDetailHistory(createdTask.Id);
        detailHistory.Count(h => h.FieldName == "DeletedAt").Should().Be(1);
    }

    [Fact]
    [DisplayName("ReopenTask moves task from Done to To-do")]
    public async Task ReopenTask_ValidId_MovesTaskToTodo()
    {
        var createdTask = await CreateTaskOrThrow("Reopen me", "Reopen test");
        var doneStatusId = await EnsureStatus("Done");
        var changedBy = await GetSystemUserId();

        await _client.PostAsJsonAsync("/api/Task/MoveTask", new MoveTaskRequest
        {
            TaskId = createdTask.Id,
            NewStatusId = doneStatusId,
            ChangedByUserId = changedBy
        });

        var response = await _client.PostAsync($"/api/Task/ReopenTask?id={createdTask.Id}", null);
        var error = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue($"because reopen should succeed, but got: {error}");

        var reopenedTask = await response.Content.ReadFromJsonAsync<TaskDto>();
        reopenedTask.Should().NotBeNull();
        reopenedTask.Status.Should().Be("To-do");
    }

    [Fact]
    [DisplayName("ReopenTask writes status change to history")]
    public async Task ReopenTask_ValidId_WritesStatusHistory()
    {
        var createdTask = await CreateTaskOrThrow("Reopen history", "Reopen history test");
        var doneStatusId = await EnsureStatus("Done");
        var todoStatusId = await EnsureStatus("To-do");
        var changedBy = await GetSystemUserId();

        await _client.PostAsJsonAsync("/api/Task/MoveTask", new MoveTaskRequest
        {
            TaskId = createdTask.Id,
            NewStatusId = doneStatusId,
            ChangedByUserId = changedBy
        });

        var response = await _client.PostAsync($"/api/Task/ReopenTask?id={createdTask.Id}", null);
        response.IsSuccessStatusCode.Should().BeTrue();

        var statusHistory = await GetTaskHistory(createdTask.Id);
        statusHistory.Should().Contain(h =>
            h.FromStatusId == doneStatusId &&
            h.ToStatusId == todoStatusId,
            "because reopen should record Done -> To-do in history");
    }

    [Fact]
    [DisplayName("ReopenTask on non-Done task returns BadRequest")]
    public async Task ReopenTask_NotDoneStatus_ReturnsBadRequest()
    {
        var createdTask = await CreateTaskOrThrow("Not done", "Not done test");

        var response = await _client.PostAsync($"/api/Task/ReopenTask?id={createdTask.Id}", null);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Only tasks with status 'Done' can be reopened.");
    }

[Fact]
    [DisplayName("ReopenTask with invalid id returns BadRequest")]
    public async Task ReopenTask_InvalidId_ReturnsBadRequest()
    {
        var response = await _client.PostAsync("/api/Task/ReopenTask?id=invalid-id", null);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Invalid task id.");
    }

    [Fact]
    [DisplayName("GetArchivedTasks returns empty list when no tasks are archived")]
    public async Task GetArchivedTasks_NoArchivedTasks_ReturnsEmpty()
    {
        var response = await _client.GetAsync("/api/Task/GetArchivedTasks");
        response.IsSuccessStatusCode.Should().BeTrue();
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>();
        tasks.Should().NotBeNull();
        tasks.Should().NotContain(t => t.DeletedAt == null, "because all returned tasks must be archived");
    }

    [Fact]
    [DisplayName("GetArchivedTasks returns task after it is archived")]
    public async Task GetArchivedTasks_AfterArchiving_ReturnsArchivedTask()
    {
        var createdTask = await CreateTaskOrThrow("Archive me", "Archive test");

        var archiveResponse = await _client.PatchAsync($"/api/Task/ArchiveTask?id={createdTask.Id}", null);
        archiveResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var response = await _client.GetAsync("/api/Task/GetArchivedTasks");
        response.IsSuccessStatusCode.Should().BeTrue();
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>();
        tasks.Should().NotBeNull();
        tasks.Should().Contain(t => t.Id == createdTask.Id);
        tasks.Should().OnlyContain(t => t.DeletedAt != null, "because all returned tasks must have DeletedAt set");
    }

    [Fact]
    [DisplayName("ArchiveTask with valid id returns NoContent and hides task from GetTasks")]
    public async Task ArchiveTask_ValidId_HidesTaskFromGetTasks()
    {
        var createdTask = await CreateTaskOrThrow("Archive hide me", "Archive hide test");

        var archiveResponse = await _client.PatchAsync($"/api/Task/ArchiveTask?id={createdTask.Id}", null);
        archiveResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var tasksResponse = await _client.GetAsync("/api/Task/GetTasks");
        tasksResponse.IsSuccessStatusCode.Should().BeTrue();
        var tasks = await tasksResponse.Content.ReadFromJsonAsync<List<TaskDto>>();
        tasks.Should().NotBeNull();
        tasks.Should().NotContain(t => t.Id == createdTask.Id);
    }

    [Fact]
    [DisplayName("ArchiveTask with valid id writes DeletedAt history entry")]
    public async Task ArchiveTask_ValidId_WritesDeletedAtHistory()
    {
        var createdTask = await CreateTaskOrThrow("Archive history", "Archive history test");
        var systemUserId = await GetSystemUserId();

        var archiveResponse = await _client.PatchAsync($"/api/Task/ArchiveTask?id={createdTask.Id}", null);
        archiveResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var detailHistory = await GetTaskDetailHistory(createdTask.Id);
        detailHistory.Should().ContainSingle(h =>
            h.FieldName == "DeletedAt" &&
            h.OldValue == null &&
            h.ChangedBy == systemUserId);

        var archiveEntry = detailHistory.Single(h => h.FieldName == "DeletedAt");
        archiveEntry.NewValue.Should().NotBeNullOrWhiteSpace();
        DateTimeOffset.TryParse(archiveEntry.NewValue, out _)
            .Should().BeTrue("because archive history NewValue should be an ISO timestamp");
    }

    [Fact]
    [DisplayName("ArchiveTask with invalid id returns BadRequest")]
    public async Task ArchiveTask_InvalidId_ReturnsBadRequest()
    {
        var response = await _client.PatchAsync("/api/Task/ArchiveTask?id=invalid-id", null);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Invalid task id.");
    }

    [Fact]
    [DisplayName("ArchiveTask with unknown id returns NotFound")]
    public async Task ArchiveTask_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PatchAsync($"/api/Task/ArchiveTask?id={Guid.NewGuid()}", null);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    [DisplayName("ArchiveTask called twice returns BadRequest second time")]
    public async Task ArchiveTask_Twice_ReturnsBadRequestSecondTime()
    {
        var createdTask = await CreateTaskOrThrow("Archive twice", "Archive twice test");

        var firstResponse = await _client.PatchAsync($"/api/Task/ArchiveTask?id={createdTask.Id}", null);
        firstResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var secondResponse = await _client.PatchAsync($"/api/Task/ArchiveTask?id={createdTask.Id}", null);
        secondResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("UnarchiveTask restores task and it appears in GetTasks again")]
    public async Task UnarchiveTask_ValidId_RestoresTaskInGetTasks()
    {
        var createdTask = await CreateTaskOrThrow("Unarchive me", "Unarchive test");

        var archiveResponse = await _client.PatchAsync($"/api/Task/ArchiveTask?id={createdTask.Id}", null);
        archiveResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var unarchiveResponse = await _client.PatchAsync($"/api/Task/UnarchiveTask?id={createdTask.Id}", null);
        unarchiveResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var tasksResponse = await _client.GetAsync("/api/Task/GetTasks");
        tasksResponse.IsSuccessStatusCode.Should().BeTrue();
        var tasks = await tasksResponse.Content.ReadFromJsonAsync<List<TaskDto>>();
        tasks.Should().NotBeNull();
        tasks.Should().Contain(t => t.Id == createdTask.Id);
    }

    [Fact]
    [DisplayName("UnarchiveTask removes task from GetArchivedTasks")]
    public async Task UnarchiveTask_ValidId_RemovesFromArchivedTasks()
    {
        var createdTask = await CreateTaskOrThrow("Unarchive from list", "Unarchive list test");

        await _client.PatchAsync($"/api/Task/ArchiveTask?id={createdTask.Id}", null);
        await _client.PatchAsync($"/api/Task/UnarchiveTask?id={createdTask.Id}", null);

        var response = await _client.GetAsync("/api/Task/GetArchivedTasks");
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>();
        tasks.Should().NotBeNull();
        tasks.Should().NotContain(t => t.Id == createdTask.Id);
    }

    [Fact]
    [DisplayName("UnarchiveTask writes DeletedAt null history entry")]
    public async Task UnarchiveTask_ValidId_WritesUnarchiveHistory()
    {
        var createdTask = await CreateTaskOrThrow("Unarchive history", "Unarchive history test");
        var systemUserId = await GetSystemUserId();

        await _client.PatchAsync($"/api/Task/ArchiveTask?id={createdTask.Id}", null);
        var unarchiveResponse = await _client.PatchAsync($"/api/Task/UnarchiveTask?id={createdTask.Id}", null);
        unarchiveResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var detailHistory = await GetTaskDetailHistory(createdTask.Id);
        detailHistory.Should().Contain(h =>
            h.FieldName == "DeletedAt" &&
            h.NewValue == null &&
            h.OldValue != null &&
            h.ChangedBy == systemUserId,
            "because unarchive should record a DeletedAt -> null entry in history");
    }

   [Fact]
    [DisplayName("UnarchiveTask with invalid id returns BadRequest")]
    public async Task UnarchiveTask_InvalidId_ReturnsBadRequest()
    {
        var response = await _client.PatchAsync("/api/Task/UnarchiveTask?id=invalid-id", null);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Invalid task id.");
    }

    [Fact]
    [DisplayName("ReopenTask with unknown id returns NotFound")]
    public async Task ReopenTask_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"/api/Task/ReopenTask?id={Guid.NewGuid()}", null);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    [DisplayName("UnarchiveTask with unknown id returns NotFound")]
    public async Task UnarchiveTask_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PatchAsync($"/api/Task/UnarchiveTask?id={Guid.NewGuid()}", null);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
    
    [Fact]
    [DisplayName("UnarchiveTask on non-archived task returns BadRequest")]
    public async Task UnarchiveTask_NotArchived_ReturnsBadRequest()
    {
        var createdTask = await CreateTaskOrThrow("Not archived", "Not archived test");

        var response = await _client.PatchAsync($"/api/Task/UnarchiveTask?id={createdTask.Id}", null);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    private async Task<TaskDto> CreateTaskOrThrow(string title, string? description)
    {
        var response = await _client.PostAsJsonAsync("/api/Task/CreateTask", new CreateTaskRequest
        {
            Title = title,
            Description = description
        });

        var error = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue($"because create should succeed, but got: {error}");

        var createdTask = await response.Content.ReadFromJsonAsync<TaskDto>();
        createdTask.Should().NotBeNull();
        return createdTask;
    }

    private async Task<Guid> GetSystemUserId()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var systemUser = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == "system");

        systemUser.Should().NotBeNull("because tests rely on system user for history logging");
        return systemUser.Id;
    }

    private async Task<Guid> EnsureStatus(string statusName)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        var existing = await db.TodoTaskStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == statusName);
        if (existing != null)
        {
            return existing.Id;
        }

        var status = new TodoTaskStatus
        {
            Name = statusName,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null
        };
        await db.TodoTaskStatuses.AddAsync(status);
        await db.SaveChangesAsync();
        return status.Id;
    }

    private async Task<Guid> GetTaskStatusId(Guid taskId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        var statusId = await db.TaskItems
            .AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(t => t.StatusId)
            .FirstOrDefaultAsync();

        statusId.Should().NotBe(Guid.Empty);
        return statusId;
    }

    private async Task<List<TaskDetailHistory>> GetTaskDetailHistory(Guid taskId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        return await db.TaskDetailHistories
            .AsNoTracking()
            .Where(h => h.TaskId == taskId)
            .ToListAsync();
    }

    private async Task<List<TaskHistory>> GetTaskHistory(Guid taskId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        return await db.TaskHistories
            .AsNoTracking()
            .Where(h => h.TaskId == taskId)
            .ToListAsync();
    }
}
