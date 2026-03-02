using System.ComponentModel;
using System.Net.Http.Json;
using FluentAssertions;
using server.Dto;
using Xunit.Abstractions;

namespace test;

public class UnitTest1 : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _client;

    public UnitTest1(CustomWebApplicationFactory factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        _client = _factory.CreateClient();
    }

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
        _testOutputHelper.WriteLine($"Status: {createResponse.StatusCode}, Error: {error}");

        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskDto>();

        // Act
        var response = await _client.GetAsync($"/api/Task/GetTaskById?id={createdTask.Id}");

        // Assert
        createResponse.IsSuccessStatusCode.Should().BeTrue($"because create should succeed, but got: {error}");
        
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
        _testOutputHelper.WriteLine($"Status: {response.StatusCode}, Error: {error}");

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
        _testOutputHelper.WriteLine($"Status: {response.StatusCode}, Error: {error}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    [DisplayName("CreateTask with invalid AssigneeId returns BadRequest")]
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
        _testOutputHelper.WriteLine($"Status: {response.StatusCode}, Error: {error}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        error.Equals($"Assignee not found with id: '{newTask.AssigneeId}'").Should()
            .BeTrue($"because the error message should indicate the assignee was not found, but got: {error}");
    }
}