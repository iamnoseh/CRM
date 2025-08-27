namespace Infrastructure.BackgroundTasks;

public class BackgroundTaskResult
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Messages { get; set; } = new();
    public List<string> FailedItems { get; set; } = new();

    public override string ToString()
    {
        return $"Success={SuccessCount}, Failed={FailedCount}, Messages=[{string.Join("; ", Messages)}], FailedItems=[{string.Join(",", FailedItems)}]";
    }
}
