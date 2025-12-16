namespace DailyStoryVideoGenerator.Services;

public interface INavigationService
{
    void NavigateTo<T>() where T : class;
    void GoBack();
    bool CanGoBack { get; }
}

public class NavigationService : INavigationService
{
    private readonly Stack<Type> _navigationStack = new();
    private readonly IServiceProvider _serviceProvider;

    public bool CanGoBack => _navigationStack.Count > 1;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<T>() where T : class
    {
        _navigationStack.Push(typeof(T));
        // 实际导航逻辑
    }

    public void GoBack()
    {
        if (CanGoBack)
        {
            _navigationStack.Pop();
            // 实际返回逻辑
        }
    }
}