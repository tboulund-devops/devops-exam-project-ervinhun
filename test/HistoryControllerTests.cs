using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using server.DataAccess;

namespace test;

public class HistoryControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    [DisplayName("GetTaskHistory returns entries ordered by ChangedAt ascending")]
    public async Task GetTaskHistory_ReturnsOrderedEntries()
    {
        var (taskId, systemUserId, fromStatusId, toStatusId) = await SeedHistoryRow();

        var response = await _client.GetAsync("/api/History/GetTaskHistory");
        response.IsSuccessStatusCode.Should().BeTrue();

        var payload = await response.Content.ReadFromJsonAsync<List<TaskHistory>>();
        payload.Should().NotBeNull();
        payload!.Should().BeInAscendingOrder(x => x.ChangedAt);
        payload.Should().Contain(h =>
            h.TaskId == taskId &&
            h.ChangedBy == systemUserId &&
            h.FromStatusId == fromStatusId &&
            h.ToStatusId == toStatusId);
    }

    [Fact]
    [DisplayName("GetTaskDetailHistory returns entries ordered by ChangedAt ascending")]
    public async Task GetTaskDetailHistory_ReturnsOrderedEntries()
    {
        var taskId = await SeedDetailHistoryRow();

        var response = await _client.GetAsync("/api/History/GetTaskDetailHistory");
        response.IsSuccessStatusCode.Should().BeTrue();

        var payload = await response.Content.ReadFromJsonAsync<List<TaskDetailHistory>>();
        payload.Should().NotBeNull();
        payload!.Should().BeInAscendingOrder(x => x.ChangedAt);
        payload.Should().Contain(h =>
            h.TaskId == taskId &&
            h.FieldName == "Title" &&
            h.OldValue == "old-value" &&
            h.NewValue == "new-value");
    }

    [Fact]
    public async Task GetTaskHistory_InvalidRoute_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/History/GetTaskHistoryInvalid");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<(Guid taskId, Guid systemUserId, Guid fromStatusId, Guid toStatusId)> SeedHistoryRow()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        var fromStatusId = await EnsureStatus(db, "Backlog");
        var toStatusId = await EnsureStatus(db, "Review");
        var systemUserId = await db.Users.Where(u => u.Username == "system").Select(u => u.Id).FirstAsync();

        var task = new TaskItem
        {
            Title = $"history-controller-task-{Guid.NewGuid():N}",
            Description = "seed",
            StatusId = toStatusId
        };

        await db.TaskItems.AddAsync(task);
        await db.SaveChangesAsync();

        await db.TaskHistories.AddAsync(new TaskHistory
        {
            TaskId = task.Id,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId,
            ChangedBy = systemUserId,
            ChangedAt = DateTime.UtcNow.AddMinutes(-2)
        });
        await db.SaveChangesAsync();

        return (task.Id, systemUserId, fromStatusId, toStatusId);
    }

    private async Task<Guid> SeedDetailHistoryRow()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        var statusId = await EnsureStatus(db, "Backlog");
        var systemUserId = await db.Users.Where(u => u.Username == "system").Select(u => u.Id).FirstAsync();

        var task = new TaskItem
        {
            Title = $"detail-history-task-{Guid.NewGuid():N}",
            Description = "seed",
            StatusId = statusId
        };

        await db.TaskItems.AddAsync(task);
        await db.SaveChangesAsync();

        await db.TaskDetailHistories.AddAsync(new TaskDetailHistory
        {
            TaskId = task.Id,
            FieldName = "Title",
            OldValue = "old-value",
            NewValue = "new-value",
            ChangedBy = systemUserId,
            ChangedAt = DateTime.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        return task.Id;
    }

    private static async Task<Guid> EnsureStatus(MyDbContext db, string name)
    {
        var existing = await db.TodoTaskStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.Name == name);
        if (existing != null)
        {
            return existing.Id;
        }

        var status = new TodoTaskStatus
        {
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        await db.TodoTaskStatuses.AddAsync(status);
        await db.SaveChangesAsync();
        return status.Id;
    }
}

