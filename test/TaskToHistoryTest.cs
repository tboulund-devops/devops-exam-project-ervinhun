using System.ComponentModel;
using Xunit.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using server.DataAccess;
using server.Dto;
using server.Utils;

namespace test;

public class TaskToHistoryTest(CustomWebApplicationFactory factory, ITestOutputHelper testOutputHelper)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    [DisplayName("OnCreate with valid user creates task history entry")]
    public async Task OnCreate_WithValidUser_CreatesTaskHistory()
    {
        var (task, systemUserId) = await CreateTaskWithSystemUser();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var sut = new SaveTaskToHistory(db);

        await sut.OnCreate(task, systemUserId);

        var entries = await db.TaskHistories
            .AsNoTracking()
            .Where(h => h.TaskId == task.Id)
            .ToListAsync();

        entries.Should().ContainSingle(h =>
            h.FromStatusId == null &&
            h.ToStatusId == task.StatusId &&
            h.ChangedBy == systemUserId);
    }

    [Fact]
    [DisplayName("OnCreate with unknown user throws KeyNotFoundException")]
    public async Task OnCreate_WithUnknownUser_Throws()
    {
        var (task, _) = await CreateTaskWithSystemUser();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var sut = new SaveTaskToHistory(db);

        Func<Task> act = async () => await sut.OnCreate(task, Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [DisplayName("OnUpdate with field changes creates task detail history entries")]
    public async Task OnUpdate_WithFieldChanges_CreatesDetailHistory()
    {
        var (task, systemUserId) = await CreateTaskWithSystemUser();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var sut = new SaveTaskToHistory(db);

        var request = new UpdateTaskRequest
        {
            Title = "New title",
            Description = "New description",
            AssigneeId = task.AssigneeId
        };

        await sut.OnUpdate(task, request, systemUserId);

        var entries = await db.TaskDetailHistories
            .AsNoTracking()
            .Where(h => h.TaskId == task.Id)
            .ToListAsync();

        entries.Should().ContainSingle(h =>
            h.FieldName == "Title" &&
            h.OldValue == task.Title &&
            h.NewValue == "New title" &&
            h.ChangedBy == systemUserId);

        entries.Should().ContainSingle(h =>
            h.FieldName == "Description" &&
            h.OldValue == task.Description &&
            h.NewValue == "New description" &&
            h.ChangedBy == systemUserId);
    }

    [Fact]
    [DisplayName("OnUpdate with only title changed creates only title history entry")]
    public async Task OnUpdate_OnlyTitleChanged_CreatesOnlyTitleHistory()
    {
        var (task, systemUserId) = await CreateTaskWithSystemUser();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var sut = new SaveTaskToHistory(db);

        var request = new UpdateTaskRequest
        {
            Title = "Title changed",
            Description = task.Description,
            AssigneeId = task.AssigneeId
        };

        await sut.OnUpdate(task, request, systemUserId);

        var entries = await db.TaskDetailHistories
            .AsNoTracking()
            .Where(h => h.TaskId == task.Id)
            .ToListAsync();

        entries.Should().HaveCount(1);
        entries[0].FieldName.Should().Be("Title");
        entries[0].OldValue.Should().Be(task.Title);
        entries[0].NewValue.Should().Be("Title changed");
    }

    [Fact]
    [DisplayName("OnUpdate with description and assignee changed creates exactly two history entries")]
    public async Task OnUpdate_DescriptionAndAssigneeChanged_CreatesExactlyTwoHistoryEntries()
    {
        var (task, systemUserId) = await CreateTaskWithSystemUser();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var sut = new SaveTaskToHistory(db);

        var otherUser = await EnsureUser("other-user", "other-user@test.local");

        var request = new UpdateTaskRequest
        {
            Title = task.Title,
            Description = "Description changed",
            AssigneeId = otherUser
        };

        await sut.OnUpdate(task, request, systemUserId);

        var entries = await db.TaskDetailHistories
            .AsNoTracking()
            .Where(h => h.TaskId == task.Id)
            .OrderBy(h => h.FieldName)
            .ToListAsync();

        var oldAssigneeValue = task.AssigneeId?.ToString();
        var newAssigneeValue = otherUser.ToString();

        entries.Should().HaveCount(2);
        entries.Select(e => e.FieldName).Should().BeEquivalentTo(["AssigneeId", "Description"]);

        entries.Should().ContainSingle(h =>
            h.FieldName == "Description" &&
            h.OldValue == task.Description &&
            h.NewValue == "Description changed");

        entries.Should().ContainSingle(h =>
            h.FieldName == "AssigneeId" &&
            h.OldValue == oldAssigneeValue &&
            h.NewValue == newAssigneeValue);
    }

    [Fact]
    [DisplayName("OnUpdate with due date changed creates due date history entry")]
    public async Task OnUpdate_DueDateChanged_CreatesDueDateHistoryEntry()
    {
        var (task, systemUserId) = await CreateTaskWithSystemUser();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var sut = new SaveTaskToHistory(db);

        var oldDueDate = DateTime.UtcNow.AddDays(1);
        task.DueDate = oldDueDate;
        await db.SaveChangesAsync();

        var newDueDate = oldDueDate.AddDays(2);
        var request = new UpdateTaskRequest
        {
            Title = task.Title,
            Description = task.Description,
            AssigneeId = task.AssigneeId,
            DueDate = newDueDate
        };

        await sut.OnUpdate(task, request, systemUserId);

        var dueDateEntries = await db.TaskDetailHistories
            .AsNoTracking()
            .Where(h => h.TaskId == task.Id && h.FieldName == "DueDate")
            .ToListAsync();

        dueDateEntries.Should().HaveCount(1);
        DateTimeOffset.TryParse(dueDateEntries[0].OldValue, out var parsedOld).Should().BeTrue();
        DateTimeOffset.TryParse(dueDateEntries[0].NewValue, out var parsedNew).Should().BeTrue();
        parsedOld.UtcDateTime.Should().BeCloseTo(oldDueDate, TimeSpan.FromSeconds(1));
        parsedNew.UtcDateTime.Should().BeCloseTo(newDueDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    [DisplayName("OnUpdate with unchanged due date does not create due date history entry")]
    public async Task OnUpdate_DueDateUnchanged_DoesNotCreateDueDateHistoryEntry()
    {
        var (task, systemUserId) = await CreateTaskWithSystemUser();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var sut = new SaveTaskToHistory(db);

        var sameDueDate = DateTime.UtcNow.AddDays(3);
        task.DueDate = sameDueDate;
        await db.SaveChangesAsync();

        var request = new UpdateTaskRequest
        {
            Title = task.Title,
            Description = task.Description,
            AssigneeId = task.AssigneeId,
            DueDate = sameDueDate
        };

        await sut.OnUpdate(task, request, systemUserId);

        var dueDateEntries = await db.TaskDetailHistories
            .AsNoTracking()
            .Where(h => h.TaskId == task.Id && h.FieldName == "DueDate")
            .ToListAsync();

        dueDateEntries.Should().BeEmpty();
    }

    [Fact]
    [DisplayName("OnStatusChange with unknown user throws KeyNotFoundException")]
    public async Task OnStatusChange_WithUnknownUser_Throws()
    {
        var (task, _) = await CreateTaskWithSystemUser();
        var toStatusId = await EnsureStatus("Done");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var sut = new SaveTaskToHistory(db);

        Func<Task> act = async () => await sut.OnStatusChange(task, task.StatusId, toStatusId, Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private async Task<(TaskItem task, Guid systemUserId)> CreateTaskWithSystemUser()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        var statusId = await db.TodoTaskStatuses
            .Where(s => s.Name == "Backlog")
            .Select(s => s.Id)
            .FirstAsync();

        var assigneeId = await db.Users
            .Where(u => u.Username == "test-user")
            .Select(u => (Guid?)u.Id)
            .FirstAsync();

        var task = new TaskItem
        {
            Title = $"history-test-{Guid.NewGuid():N}",
            Description = "Old description",
            StatusId = statusId,
            AssigneeId = assigneeId
        };

        await db.TaskItems.AddAsync(task);
        await db.SaveChangesAsync();

        var systemUserId = await db.Users
            .Where(u => u.Username == "system")
            .Select(u => u.Id)
            .FirstAsync();

        testOutputHelper.WriteLine($"Created task {task.Id}");
        return (task, systemUserId);
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
            CreatedAt = DateTime.UtcNow
        };
        await db.TodoTaskStatuses.AddAsync(status);
        await db.SaveChangesAsync();
        return status.Id;
    }

    private async Task<Guid> EnsureUser(string username, string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        var existing = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);
        if (existing != null)
        {
            return existing.Id;
        }

        var user = new User
        {
            Username = username,
            Email = email
        };

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
        return user.Id;
    }
}