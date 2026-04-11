using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Dto;
using server.Services;
using Testcontainers.PostgreSql;
using Xunit;

namespace test;

public class TaskServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private MyDbContext _context = null!;
    private TaskService _taskService = null!;

    public TaskServiceTests()
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

        _taskService = new TaskService(_context);
    }

    [Fact]
    public async Task GetTasksByQueryAsync_WithNoFilters_ReturnsOnlyNonDeletedTasks_SortedByCreatedAtDesc()
    {
        _ = await SeedTasksAsync();

        var query = new TaskQueryParameters();

        var result = await _taskService.GetTasksByQueryAsync(query);

        result.Should().HaveCount(3);
        result.Select(t => t.Title).Should().ContainInOrder(
            "Newest Task",
            "In Progress Task",
            "Old Task"
        );
    }

    [Fact]
    public async Task GetTasksByQueryAsync_FilterByStatus_ReturnsOnlyMatchingTasks()
    {
        _ = await SeedTasksAsync();

        var query = new TaskQueryParameters
        {
            Status = "Done"
        };

        var result = await _taskService.GetTasksByQueryAsync(query);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Newest Task");
        result[0].Status.Should().Be("Done");
    }

    [Fact]
    public async Task GetTasksByQueryAsync_FilterByStatus_IsCaseInsensitive()
    {
        _ = await SeedTasksAsync();

        var query = new TaskQueryParameters
        {
            Status = "dOnE"
        };

        var result = await _taskService.GetTasksByQueryAsync(query);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Newest Task");
        result[0].Status.Should().Be("Done");
    }

    [Fact]
    public async Task GetTasksByQueryAsync_SortByCreatedAtAsc_ReturnsAscendingOrder()
    {
        _ = await SeedTasksAsync();

        var query = new TaskQueryParameters
        {
            SortBy = "createdAt",
            SortOrder = "asc"
        };

        var result = await _taskService.GetTasksByQueryAsync(query);

        result.Should().HaveCount(3);
        result.Select(t => t.Title).Should().ContainInOrder(
            "Old Task",
            "In Progress Task",
            "Newest Task"
        );
    }

    [Fact]
    public async Task GetTasksByQueryAsync_SortByUpdatedAtDesc_ReturnsDescendingOrder()
    {
        _ = await SeedTasksAsync();

        var query = new TaskQueryParameters
        {
            SortBy = "updatedAt",
            SortOrder = "desc"
        };

        var result = await _taskService.GetTasksByQueryAsync(query);

        result.Should().HaveCount(3);
        result.Select(t => t.Title).Should().ContainInOrder(
            "Newest Task",
            "In Progress Task",
            "Old Task"
        );
    }

    [Fact]
    public async Task GetTasksByQueryAsync_SortByUpdatedAtAsc_ReturnsAscendingOrder()
    {
        _ = await SeedTasksAsync();

        var query = new TaskQueryParameters
        {
            SortBy = "updatedAt",
            SortOrder = "asc"
        };

        var result = await _taskService.GetTasksByQueryAsync(query);

        result.Should().HaveCount(3);
        result.Select(t => t.Title).Should().ContainInOrder(
            "Old Task",
            "In Progress Task",
            "Newest Task"
        );
    }

    [Fact]
    public async Task GetTasksByQueryAsync_InvalidSortOrder_ThrowsArgumentException()
    {
        _ = await SeedTasksAsync();

        var query = new TaskQueryParameters
        {
            SortOrder = "invalid"
        };

        Func<Task> act = async () => await _taskService.GetTasksByQueryAsync(query);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid sortOrder. Use 'asc' or 'desc'.");
    }

    [Fact]
    public async Task GetTasksByQueryAsync_InvalidSortBy_ThrowsArgumentException()
    {
        _ = await SeedTasksAsync();

        var query = new TaskQueryParameters
        {
            SortBy = "title"
        };

        Func<Task> act = async () => await _taskService.GetTasksByQueryAsync(query);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid sortBy. Use 'createdAt' or 'updatedAt'.");
    }

    [Fact]
    public async Task GetTasksByQueryAsync_ReturnsAssignee_WhenTaskHasAssignee()
    {
        _ = await SeedTasksAsync();

        var query = new TaskQueryParameters
        {
            Status = "Todo"
        };

        var result = await _taskService.GetTasksByQueryAsync(query);

        result.Should().HaveCount(1);
        result[0].Assignee.Should().NotBeNull();
        result[0].Assignee!.Username.Should().Be("alice");
    }

    [Fact]
    public async Task GetTasksByQueryAsync_ReturnsNullAssignee_WhenTaskHasNoAssignee()
    {
        _ = await SeedTasksAsync();

        var query = new TaskQueryParameters
        {
            Status = "Done"
        };

        var result = await _taskService.GetTasksByQueryAsync(query);

        result.Should().HaveCount(1);
        result[0].Assignee.Should().BeNull();
    }

    private async Task<Guid> SeedTasksAsync()
{
    var user = new User
    {
        Id = Guid.NewGuid(),
        Username = "alice",
        Email = "alice@test.com"
    };

    var todoStatus = new TodoTaskStatus
    {
        Id = Guid.NewGuid(),
        Name = "Todo"
    };

    var inProgressStatus = new TodoTaskStatus
    {
        Id = Guid.NewGuid(),
        Name = "In Progress"
    };

    var doneStatus = new TodoTaskStatus
    {
        Id = Guid.NewGuid(),
        Name = "Done"
    };

    _context.Users.Add(user);
    _context.TodoTaskStatuses.AddRange(todoStatus, inProgressStatus, doneStatus);

    _context.TaskItems.AddRange(
        new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Old Task",
            Description = "Old description",
            CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 1, 11, 0, 0, DateTimeKind.Utc),
            DeletedAt = null,
            AssigneeId = user.Id,
            Assignee = user,
            StatusId = todoStatus.Id,
            Status = todoStatus
        },
        new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "In Progress Task",
            Description = "In progress description",
            CreatedAt = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 2, 11, 0, 0, DateTimeKind.Utc),
            DeletedAt = null,
            StatusId = inProgressStatus.Id,
            Status = inProgressStatus
        },
        new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Newest Task",
            Description = "Newest description",
            CreatedAt = new DateTime(2024, 1, 3, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 3, 11, 0, 0, DateTimeKind.Utc),
            DeletedAt = null,
            StatusId = doneStatus.Id,
            Status = doneStatus
        },
        new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Deleted Task",
            Description = "Should not be returned",
            CreatedAt = new DateTime(2024, 1, 4, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 4, 11, 0, 0, DateTimeKind.Utc),
            DeletedAt = new DateTime(2024, 1, 5, 10, 0, 0, DateTimeKind.Utc),
            StatusId = todoStatus.Id,
            Status = todoStatus
        }
    );

    await _context.SaveChangesAsync();
    return user.Id;
}
    
    [Fact]
    public async Task GetTasksByQueryAsync_FilterByAssigneeId_ReturnsOnlyMatchingTasks()
    {
        // Arrange
        var assigneeId = await SeedTasksAsync();

        var query = new TaskQueryParameters
        {
            AssigneeId = assigneeId
        };

        // Act
        var result = await _taskService.GetTasksByQueryAsync(query);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Old Task");
        result[0].Assignee.Should().NotBeNull();
        result[0].Assignee!.Id.Should().Be(assigneeId);
        result[0].Assignee.Username.Should().Be("alice");
    }
    
    [Fact]
    public async Task GetTasksByQueryAsync_FilterByAssigneeIdAndStatus_ReturnsOnlyMatchingTasks()
    {
        // Arrange
        var assigneeId = await SeedTasksAsync();

        var query = new TaskQueryParameters
        {
            AssigneeId = assigneeId,
            Status = "Todo"
        };

        // Act
        var result = await _taskService.GetTasksByQueryAsync(query);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Old Task");
        result[0].Status.Should().Be("Todo");
        result[0].Assignee.Should().NotBeNull();
        result[0].Assignee!.Id.Should().Be(assigneeId);
    }
}