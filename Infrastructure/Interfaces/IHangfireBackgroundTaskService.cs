namespace Infrastructure.Interfaces;

public interface IHangfireBackgroundTaskService
{

    void StartAllBackgroundTasks();
    
    void StopAllBackgroundTasks();
    
    void TriggerBackgroundTask(string taskName);
    
    Dictionary<string, object> GetBackgroundTasksStatus();
}
