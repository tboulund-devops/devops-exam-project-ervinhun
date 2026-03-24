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
        var seeded = await SeedTaskHistoryRows();

        var response = await _client.GetAsync("/api/History/GetTaskHistory");
        response.IsSuccessStatusCode.Should().BeTrue();

        var payload = await response.Content.ReadFromJsonAsync<List<TaskHistory>>();
        payload.Should().NotBeNull();
        payload!.Should().BeInAscendingOrder(x => x.ChangedAt);

        var taskEntries = payload
            .Where(h => h.TaskId == seeded.TaskId)
            .OrderBy(h => h.ChangedAt)
            .ToList();

        taskEntries.Count.Should().BeGreaterThanOrEqualTo(2);
        taskEntries.Select(h => h.Id).Should().ContainInOrder(seeded.OlderEntryId, seeded.NewerEntryId);
        taskEntries.Should().Contain(h => h.ChangedBy == seeded.SystemUserId);
    }

    [Fact]
    [DisplayName("GetTaskDetailHistory returns entries ordered by ChangedAt ascending")]
    public async Task GetTaskDetailHistory_ReturnsOrderedEntries()
    {
        var seeded = await SeedTaskDetailHistoryRows();

        var response = await _client.GetAsync("/api/History/GetTaskDetailHistory");
        response.IsSuccessStatusCode.Should().BeTrue();

        var payload = await response.Content.ReadFromJsonAsync<List<TaskDetailHistory>>();
        payload.Should().NotBeNull();
        payload!.Should().BeInAscendingOrder(x => x.ChangedAt);

        var taskEntries = payload
            .Where(h => h.TaskId == seeded.TaskId)
            .OrderBy(h => h.ChangedAt)
            .ToList();

        taskEntries.Count.Should().BeGreaterThanOrEqualTo(2);
        taskEntries.Select(h => h.Id).Should().ContainInOrder(seeded.OlderEntryId, seeded.NewerEntryId);
        taskEntries.Select(h => h.FieldName).Should().Contain(new[] { "Title", "Description" });
    }

    [Fact]
    public async Task GetTaskHistory_InvalidRoute_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/History/GetTaskHistoryInvalid");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<(Guid TaskId, Guid SystemUserId, Guid OlderEntryId, Guid NewerEntryId)> SeedTaskHistoryRows()
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

        var newerEntry = new TaskHistory
        {
            TaskId = task.Id,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId,
            ChangedBy = systemUserId,
            ChangedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var olderEntry = new TaskHistory
        {
            TaskId = task.Id,
            FromStatusId = toStatusId,
            ToStatusId = fromStatusId,
            ChangedBy = systemUserId,
            ChangedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        // Insert in reverse timestamp order to prove endpoint sorting is based on ChangedAt.
        await db.TaskHistories.AddAsync(newerEntry);
        await db.TaskHistories.AddAsync(olderEntry);
        await db.SaveChangesAsync();

        return (task.Id, systemUserId, olderEntry.Id, newerEntry.Id);
    }

    private async Task<(Guid TaskId, Guid OlderEntryId, Guid NewerEntryId)> SeedTaskDetailHistoryRows()
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

        var newerEntry = new TaskDetailHistory
        {
            TaskId = task.Id,
            FieldName = "Description",
            OldValue = "old-description",
            NewValue = "new-description",
            ChangedBy = systemUserId,
            ChangedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var olderEntry = new TaskDetailHistory
        {
            TaskId = task.Id,
            FieldName = "Title",
            OldValue = "old-title",
            NewValue = "new-title",
            ChangedBy = systemUserId,
            ChangedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        // Insert in reverse timestamp order to prove endpoint sorting is based on ChangedAt.
        await db.TaskDetailHistories.AddAsync(newerEntry);
        await db.TaskDetailHistories.AddAsync(olderEntry);
        await db.SaveChangesAsync();

        return (task.Id, olderEntry.Id, newerEntry.Id);
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

