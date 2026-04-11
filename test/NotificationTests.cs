using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using server.Controller;
using server.DataAccess;
using server.Dto;

namespace test;

public class NotificationTests(CustomWebApplicationFactory factory)
: IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateNotification_ValidData_ReturnsCreatedNotification()
    {
        var userId = await GetTestUserId();

        var newNotification = new CreateNotificationDto
        {
            userId = userId,
            message = "Test notification message"
        };

        var response = await _client.PostAsJsonAsync("/api/Notifications/CreateNotification", newNotification);

        response.IsSuccessStatusCode.Should().BeTrue();
        var createdNotification = await response.Content.ReadFromJsonAsync<NotificationDto>();
        createdNotification.Should().NotBeNull();
        createdNotification!.userId.Should().Be(newNotification.userId);
        createdNotification.message.Should().Be(newNotification.message);
        createdNotification.isRead.Should().BeFalse();
        createdNotification.id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateNotification_UnknownUser_ReturnsNotFound()
    {
        var newNotification = new CreateNotificationDto
        {
            userId = Guid.NewGuid(),
            message = "Missing user"
        };

        var response = await _client.PostAsJsonAsync("/api/Notifications/CreateNotification", newNotification);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("User not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateNotification_EmptyOrWhitespaceMessage_ReturnsBadRequest(string invalidMessage)
    {
        var userId = await GetTestUserId();

        var request = new CreateNotificationDto
        {
            userId = userId,
            message = invalidMessage
        };

        var response = await _client.PostAsJsonAsync("/api/Notifications/CreateNotification", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Be("Message cannot be empty.");
    }

    [Fact]
    public async Task GetAllNotifications_DefaultFilter_ReturnsUnreadOnly_OrderedDesc()
    {
        var userId = await CreateUser("notif-default", "notif-default@test.local");
        await SeedNotification(userId, "old unread", false, DateTime.UtcNow.AddMinutes(-30));
        await SeedNotification(userId, "new unread", false, DateTime.UtcNow.AddMinutes(-10));
        await SeedNotification(userId, "middle read", true, DateTime.UtcNow.AddMinutes(-20));

        var response = await _client.GetAsync($"/api/Notifications/GetAllNotifications?userId={userId}");

        response.IsSuccessStatusCode.Should().BeTrue();
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        notifications.Should().NotBeNull();
        notifications.Should().OnlyContain(n => n.userId == userId && !n.isRead);
        notifications.Should().BeInDescendingOrder(n => n.createdAt);
    }

    [Fact]
    public async Task GetAllNotifications_OnlyUnreadFalse_ReturnsReadAndUnread()
    {
        var userId = await CreateUser("notif-all", "notif-all@test.local");
        await SeedNotification(userId, "unread", false, DateTime.UtcNow.AddMinutes(-10));
        await SeedNotification(userId, "read", true, DateTime.UtcNow.AddMinutes(-5));

        var response = await _client.GetAsync($"/api/Notifications/GetAllNotifications?userId={userId}&onlyUnread=false");

        response.IsSuccessStatusCode.Should().BeTrue();
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        notifications.Should().NotBeNull();
        notifications.Should().Contain(n => !n.isRead);
        notifications.Should().Contain(n => n.isRead);
    }

    [Theory]
    [InlineData("")]
    [InlineData("      ")]
    [InlineData("default")]
    [InlineData("not-a-bool")]
    public async Task GetAllNotifications_InvalidOnlyUnreadValue_DefaultsToUnreadOnly(string onlyUnreadOptions)
    {
        var userId = await CreateUser("notif-invalid-filter", "notif-invalid-filter@test.local");
        await SeedNotification(userId, "unread for invalid filter", false, DateTime.UtcNow.AddMinutes(-10));
        await SeedNotification(userId, "read for invalid filter", true, DateTime.UtcNow.AddMinutes(-5));

        var response = await _client.GetAsync($"/api/Notifications/GetAllNotifications?userId={userId}&onlyUnread={onlyUnreadOptions}");

        response.IsSuccessStatusCode.Should().BeTrue();
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        notifications.Should().NotBeNull();
        notifications.Should().OnlyContain(n => n.userId == userId && !n.isRead);
    }

    [Fact]
    public async Task GetAllNotifications_EmptyStringOnlyUnread_DirectCall_DefaultsToUnreadOnly()
    {
        var userId = await CreateUser("notif-empty-direct", "notif-empty-direct@test.local");
        await SeedNotification(userId, "unread direct", false, DateTime.UtcNow.AddMinutes(-10));
        await SeedNotification(userId, "read direct", true, DateTime.UtcNow.AddMinutes(-5));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var sut = new NotificationsController(db);

        var notifications = await sut.GetAllNotifications(userId.ToString(), "");

        notifications.Should().NotBeNull();
        notifications.Should().OnlyContain(n => n.userId == userId && !n.isRead);
    }

    [Fact]
    public async Task GetAllNotifications_InvalidUserId_ThrowsKeyNotFound()
    {
        Func<Task> act = async () => await _client.GetAsync("/api/Notifications/GetAllNotifications?userId=invalid-user-id");
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Invalid user id.");
    }

    [Fact]
    public async Task GetAllNotifications_UnknownUser_ThrowsKeyNotFound()
    {
        Func<Task> act = async () => await _client.GetAsync($"/api/Notifications/GetAllNotifications?userId={Guid.NewGuid()}");
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task ReadNotification_UnreadNotification_ReturnsNoContent_AndMarksAsRead()
    {
        var userId = await GetTestUserId();
        var notificationId = await SeedNotification(userId, "mark as read", false, DateTime.UtcNow);

        var response = await _client.PatchAsync($"/api/Notifications/ReadNotification?id={notificationId}", null);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var notification = await db.Notifications.FirstAsync(n => n.Id == notificationId);
        notification.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task ReadNotification_InvalidId_ReturnsBadRequest()
    {
        var response = await _client.PatchAsync("/api/Notifications/ReadNotification?id=invalid-id", null);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Be("Invalid notification id.");
    }

    [Fact]
    public async Task ReadNotification_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PatchAsync($"/api/Notifications/ReadNotification?id={Guid.NewGuid()}", null);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadNotification_AlreadyRead_ReturnsBadRequest()
    {
        var userId = await GetTestUserId();
        var notificationId = await SeedNotification(userId, "already read", true, DateTime.UtcNow);

        var response = await _client.PatchAsync($"/api/Notifications/ReadNotification?id={notificationId}", null);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Be("Notification is already marked as read.");
    }

    private async Task<Guid> GetTestUserId()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        return await db.Users
            .Where(u => u.Username == "test-user")
            .Select(u => u.Id)
            .FirstAsync();
    }

    private async Task<Guid> CreateUser(string username, string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (existingUser != null)
        {
            return existingUser.Id;
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

    private async Task<Guid> SeedNotification(Guid userId, string message, bool isRead, DateTime createdAt)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        var notification = new Notification
        {
            UserId = userId,
            Message = $"{message}-{Guid.NewGuid():N}",
            IsRead = isRead,
            CreatedAt = createdAt
        };

        await db.Notifications.AddAsync(notification);
        await db.SaveChangesAsync();
        return notification.Id;
    }
}