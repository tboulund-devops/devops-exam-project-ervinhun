using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using server.DataAccess;
using server.Dto;

namespace test;

public class TaskCommentControllerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    [DisplayName("CreateComment with valid data returns Success")]
    public async Task CreateComment_ValidData()
    {
        // Arrange
        var createTaskResponse = await _client.PostAsJsonAsync("/api/Task/CreateTask", new CreateTaskRequest
        {
            Title = "Task for comment test",
            Description = "Testing comments"
        });

        createTaskResponse.IsSuccessStatusCode.Should().BeTrue();
        var createdTask = await createTaskResponse.Content.ReadFromJsonAsync<TaskDto>();
        createdTask.Should().NotBeNull();

        var usersResponse = await _client.GetAsync("/api/Task/Users");
        usersResponse.IsSuccessStatusCode.Should().BeTrue();
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNullOrEmpty();
        var systemUser = users!.First(u => u.Username == "system");

        var request = new CreateTaskCommentRequest
        {
            Content = "This is a comment",
            UserId = systemUser.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/Task/{createdTask!.Id}/comments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var comment = await response.Content.ReadFromJsonAsync<TaskCommentDto>();
        comment.Should().NotBeNull();
        comment!.TaskId.Should().Be(createdTask.Id);
        comment.Content.Should().Be("This is a comment");
        comment.UserId.Should().Be(systemUser.Id);
        comment.Username.Should().Be("system");
    }

    [Fact]
    [DisplayName("CreateComment with empty content returns BadRequest")]
    public async Task CreateComment_EmptyContent_ReturnsBadRequest()
    {
        // Arrange
        var createTaskResponse = await _client.PostAsJsonAsync("/api/Task/CreateTask", new CreateTaskRequest
        {
            Title = "Task for invalid comment"
        });

        createTaskResponse.IsSuccessStatusCode.Should().BeTrue();
        var createdTask = await createTaskResponse.Content.ReadFromJsonAsync<TaskDto>();
        createdTask.Should().NotBeNull();

        var request = new CreateTaskCommentRequest
        {
            Content = "   "
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/Task/{createdTask!.Id}/comments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Comment content is required.");
    }

    [Fact]
    [DisplayName("GetCommentsByTaskId with valid task returns comments")]
    public async Task GetCommentsByTaskId_ValidTask_ReturnsComments()
    {
        // Arrange
        var createTaskResponse = await _client.PostAsJsonAsync("/api/Task/CreateTask", new CreateTaskRequest
        {
            Title = "Task for reading comments"
        });

        createTaskResponse.IsSuccessStatusCode.Should().BeTrue();
        var createdTask = await createTaskResponse.Content.ReadFromJsonAsync<TaskDto>();
        createdTask.Should().NotBeNull();

        var usersResponse = await _client.GetAsync("/api/Task/Users");
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNullOrEmpty();
        var systemUser = users!.First(u => u.Username == "system");

        await _client.PostAsJsonAsync($"/api/Task/{createdTask!.Id}/comments", new CreateTaskCommentRequest
        {
            Content = "First comment",
            UserId = systemUser.Id
        });

        await _client.PostAsJsonAsync($"/api/Task/{createdTask.Id}/comments", new CreateTaskCommentRequest
        {
            Content = "Second comment",
            UserId = systemUser.Id
        });

        // Act
        var response = await _client.GetAsync($"/api/Task/{createdTask.Id}/comments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var comments = await response.Content.ReadFromJsonAsync<List<TaskCommentDto>>();
        comments.Should().NotBeNull();
        comments.Should().HaveCount(2);
        comments![0].Content.Should().Be("First comment");
        comments[1].Content.Should().Be("Second comment");
    }

    [Fact]
    [DisplayName("GetCommentsByTaskId with unknown task returns NotFound")]
    public async Task GetCommentsByTaskId_UnknownTask_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/Task/{Guid.NewGuid()}/comments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Task not found");
    }
}