using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Dto;
using server.Services;
using Testcontainers.PostgreSql;
using Xunit;

namespace test;

public class TaskCommentServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private MyDbContext _context = null!;
    private TaskCommentService _taskCommentService = null!;

    public TaskCommentServiceTests()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await CreateFreshContextAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _container.DisposeAsync();
    }

    private async Task CreateFreshContextAsync()
    {
        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        _context = new MyDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        _taskCommentService = new TaskCommentService(_context);
    }

    [Fact]
    public async Task GetCommentsByTaskIdAsync_WhenTaskExists_ReturnsOnlyNonDeletedCommentsOrderedByCreatedAt()
    {
        // Arrange
        var seeded = await SeedTaskWithCommentsAsync();

        // Act
        var result = await _taskCommentService.GetCommentsByTaskIdAsync(seeded.TaskId);

        // Assert
        result.Should().HaveCount(2);
        result.Select(c => c.Content).Should().ContainInOrder("First comment", "Second comment");

        result[0].TaskId.Should().Be(seeded.TaskId);
        result[0].UserId.Should().Be(seeded.UserId);
        result[0].Username.Should().Be("alice");

        result[1].TaskId.Should().Be(seeded.TaskId);
        result[1].UserId.Should().Be(seeded.UserId);
        result[1].Username.Should().Be("alice");
    }

    [Fact]
    public async Task GetCommentsByTaskIdAsync_WhenTaskDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistingTaskId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _taskCommentService.GetCommentsByTaskIdAsync(nonExistingTaskId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Task not found");
    }

    [Fact]
    public async Task CreateCommentAsync_WithValidTaskAndUser_CreatesCommentAndReturnsDto()
    {
        // Arrange
        var seeded = await SeedBasicTaskAsync();

        var request = new CreateTaskCommentRequest
        {
            Content = "My new comment",
            UserId = seeded.UserId
        };

        // Act
        var result = await _taskCommentService.CreateCommentAsync(seeded.TaskId, request);

        // Assert
        result.Should().NotBeNull();
        result.TaskId.Should().Be(seeded.TaskId);
        result.Content.Should().Be("My new comment");
        result.UserId.Should().Be(seeded.UserId);
        result.Username.Should().Be("alice");

        var savedComment = await _context.TaskComments.FirstOrDefaultAsync(c => c.Id == result.Id);
        savedComment.Should().NotBeNull();
        savedComment!.Content.Should().Be("My new comment");
        savedComment.TaskId.Should().Be(seeded.TaskId);
        savedComment.UserId.Should().Be(seeded.UserId);
    }

    [Fact]
    public async Task CreateCommentAsync_WithNullUserId_CreatesComment()
    {
        // Arrange
        var seeded = await SeedBasicTaskAsync();

        var request = new CreateTaskCommentRequest
        {
            Content = "Anonymous comment",
            UserId = null
        };

        // Act
        var result = await _taskCommentService.CreateCommentAsync(seeded.TaskId, request);

        // Assert
        result.TaskId.Should().Be(seeded.TaskId);
        result.Content.Should().Be("Anonymous comment");
        result.UserId.Should().BeNull();
        result.Username.Should().BeNull();
    }

    [Fact]
    public async Task CreateCommentAsync_TrimmedContent_SavesTrimmedValue()
    {
        // Arrange
        var seeded = await SeedBasicTaskAsync();

        var request = new CreateTaskCommentRequest
        {
            Content = "   trimmed comment   ",
            UserId = seeded.UserId
        };

        // Act
        var result = await _taskCommentService.CreateCommentAsync(seeded.TaskId, request);

        // Assert
        result.Content.Should().Be("trimmed comment");
    }

    [Fact]
    public async Task CreateCommentAsync_EmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var seeded = await SeedBasicTaskAsync();

        var request = new CreateTaskCommentRequest
        {
            Content = "   ",
            UserId = seeded.UserId
        };

        // Act
        Func<Task> act = async () => await _taskCommentService.CreateCommentAsync(seeded.TaskId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Comment content is required.");
    }

    [Fact]
    public async Task CreateCommentAsync_WhenTaskDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var seeded = await SeedBasicTaskAsync();

        var request = new CreateTaskCommentRequest
        {
            Content = "Comment",
            UserId = seeded.UserId
        };

        // Act
        Func<Task> act = async () => await _taskCommentService.CreateCommentAsync(Guid.NewGuid(), request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Task not found");
    }

    [Fact]
    public async Task CreateCommentAsync_WhenUserDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var seeded = await SeedBasicTaskAsync();

        var request = new CreateTaskCommentRequest
        {
            Content = "Comment",
            UserId = Guid.NewGuid()
        };

        // Act
        Func<Task> act = async () => await _taskCommentService.CreateCommentAsync(seeded.TaskId, request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found");
    }

    private async Task<(Guid TaskId, Guid UserId)> SeedBasicTaskAsync()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "alice",
            Email = "alice-comments@test.com"
        };

        var status = new TodoTaskStatus
        {
            Id = Guid.NewGuid(),
            Name = "Todo-Comments"
        };

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task for comments",
            Description = "Comment test task",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null,
            StatusId = status.Id,
            Status = status
        };

        _context.Users.Add(user);
        _context.TodoTaskStatuses.Add(status);
        _context.TaskItems.Add(task);

        await _context.SaveChangesAsync();

        return (task.Id, user.Id);
    }

    private async Task<(Guid TaskId, Guid UserId)> SeedTaskWithCommentsAsync()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "alice",
            Email = "alice-seeded-comments@test.com"
        };

        var status = new TodoTaskStatus
        {
            Id = Guid.NewGuid(),
            Name = "Todo-Comments-Seeded"
        };

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task with seeded comments",
            Description = "Used for get comments tests",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null,
            StatusId = status.Id,
            Status = status
        };

        var firstComment = new TaskComment
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            UserId = user.Id,
            User = user,
            Content = "First comment",
            CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            DeletedAt = null
        };

        var secondComment = new TaskComment
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            UserId = user.Id,
            User = user,
            Content = "Second comment",
            CreatedAt = new DateTime(2024, 1, 1, 11, 0, 0, DateTimeKind.Utc),
            DeletedAt = null
        };

        var deletedComment = new TaskComment
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            UserId = user.Id,
            User = user,
            Content = "Deleted comment",
            CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            DeletedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.TodoTaskStatuses.Add(status);
        _context.TaskItems.Add(task);
        _context.TaskComments.AddRange(firstComment, secondComment, deletedComment);

        await _context.SaveChangesAsync();

        return (task.Id, user.Id);
    }
}