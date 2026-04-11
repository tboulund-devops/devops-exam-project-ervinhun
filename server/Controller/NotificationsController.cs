using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Dto;

namespace server.Controller;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController(MyDbContext ctx) : ControllerBase
{
    [HttpGet(nameof(GetAllNotifications))]
    public async Task<List<NotificationDto>> GetAllNotifications([FromQuery] string userId,
        [FromQuery] string? onlyUnread)
    {
        if (!Guid.TryParse(userId, out var queryUserId))
        {
            throw new KeyNotFoundException("Invalid user id.");
        }

        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == queryUserId && u.DeletedAt == null);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }
        
        if (onlyUnread != null && onlyUnread != "true" && onlyUnread != "false")
        {
            onlyUnread = "true";
        }

        var filterForUnread = onlyUnread == null || onlyUnread.Equals("true", StringComparison.OrdinalIgnoreCase);
        var notificationQuery = ctx.Notifications
            .Where(n => n.UserId == queryUserId);

        if (filterForUnread)
        {
            notificationQuery = notificationQuery.Where(n => !n.IsRead);
        }

        var notifications = notificationQuery.OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                id = n.Id,
                userId = n.UserId,
                message = n.Message,
                isRead = n.IsRead,
                createdAt = n.CreatedAt
            });

        return await notifications.ToListAsync();
    }
    
    [HttpPost(nameof(CreateNotification))]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto request)
    {
        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == request.userId && u.DeletedAt == null);
        if (user == null)
        {
            return NotFound($"User not found with id: '{request.userId}'");
        }
        
        if (string.IsNullOrWhiteSpace(request.message))
        {
            return BadRequest("Message cannot be empty.");
        }

        var notification = new Notification
        {
            UserId = request.userId,
            Message = request.message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await ctx.Notifications.AddAsync(notification);
        await ctx.SaveChangesAsync();

        return Ok(new NotificationDto
        {
            id = notification.Id,
            userId = notification.UserId,
            message = notification.Message,
            isRead = notification.IsRead,
            createdAt = notification.CreatedAt
        });
    }
    
    [HttpPatch(nameof(ReadNotification))]
    public async Task<IActionResult> ReadNotification([FromQuery] string id)
    {
        if (!Guid.TryParse(id, out var notificationId))
        {
            return BadRequest("Invalid notification id.");
        }

        var notification = await ctx.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId);

        if (notification == null)
        {
            return NotFound();
        }

        if (notification.IsRead)
        {
            return BadRequest("Notification is already marked as read.");
        }

        notification.IsRead = true;
        await ctx.SaveChangesAsync();

        return NoContent();
    }
}