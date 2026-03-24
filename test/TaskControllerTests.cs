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