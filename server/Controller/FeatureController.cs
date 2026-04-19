using Microsoft.AspNetCore.Mvc;
using server.FHHelper;

namespace server.Controller;

[ApiController]
[Route("api/[controller]")]
public class FeatureController(FeatureStateProvider featureStateProvider): ControllerBase
{
    [HttpGet]
    public IActionResult GetFeatures()
    {
        return Ok(new
        {
            archiveTask = featureStateProvider.IsEnabled("ArchiveTask"),
            getArchivedTasks = featureStateProvider.IsEnabled("GetArchivedTasks"),
            unarchiveTask = featureStateProvider.IsEnabled("UnarchiveTask"),
            createTask = featureStateProvider.IsEnabled("CreateTask"),
            updateTask = featureStateProvider.IsEnabled("UpdateTask"),
            deleteTask = featureStateProvider.IsEnabled("DeleteTask"),
            moveTask = featureStateProvider.IsEnabled("MoveTask"),
            getTasks = featureStateProvider.IsEnabled("GetTasks"),
            taskExpiry = featureStateProvider.IsEnabled("TaskExpiry")
        });
    }
}