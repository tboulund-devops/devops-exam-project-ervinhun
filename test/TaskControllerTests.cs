using System.ComponentModel;
using System.Net.Http.Json;
using FluentAssertions;
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
        var response = await _client.PatchAsync($"/api/Task/AssignTask?id=invalid-id&assigneeId={Guid.NewGuid()}", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("AssignTask with invalid assigneeId returns BadRequest")]
    public async Task AssignTask_InvalidAssigneeId()
    {
        // Act
        var response = await _client.PatchAsync($"/api/Task/AssignTask?id={Guid.NewGuid()}&assigneeId=invalid-id", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("AssignTask with non-existing task returns NotFound")]
    public async Task AssignTask_NonExistingTask()
    {
        // Act
        var response = await _client.PatchAsync($"/api/Task/AssignTask?id={Guid.NewGuid()}&assigneeId={Guid.NewGuid()}", null);

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
        var response = await _client.PatchAsync($"/api/Task/AssignTask?id={createdTask!.Id}&assigneeId={Guid.NewGuid()}", null);

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
        var systemUser = users!.First();

        // Act
        var response = await _client.PatchAsync(
            $"/api/Task/AssignTask?id={createdTask!.Id}&assigneeId={systemUser.Id}", null);
        var error = await response.Content.ReadAsStringAsync();
        testOutputHelper.WriteLine($"Status: {response.StatusCode}, Body: {error}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue($"because assign should succeed, but got: {error}");
        var assignedTask = await response.Content.ReadFromJsonAsync<TaskDto>();
        assignedTask.Should().NotBeNull();
        assignedTask!.Assignee.Should().NotBeNull();
        assignedTask.Assignee!.Id.Should().Be(systemUser.Id);
    }
}