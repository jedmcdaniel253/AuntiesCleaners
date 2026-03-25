namespace AuntiesCleaners.Client.Services;

public class TabConfigNotifier
{
    public event Func<Task>? OnTabConfigChanged;

    public async Task NotifyChanged()
    {
        if (OnTabConfigChanged != null)
            await OnTabConfigChanged.Invoke();
    }
}
