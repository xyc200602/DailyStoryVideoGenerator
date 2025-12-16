namespace DailyStoryVideoGenerator.Services;

public interface IDialogService
{
    Task<bool> ShowQuestionAsync(string title, string message);
    Task ShowInfoAsync(string title, string message);
    Task ShowWarningAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task<string?> ShowOpenFileDialogAsync(string filter = "所有文件|*.*");
    Task<string?> ShowSaveFileDialogAsync(string filter = "所有文件|*.*");
}

public class DialogService : IDialogService
{
    public async Task<bool> ShowQuestionAsync(string title, string message)
    {
        return await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var result = System.Windows.MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        });
    }

    public async Task ShowInfoAsync(string title, string message)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            System.Windows.MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        });
    }

    public async Task ShowWarningAsync(string title, string message)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            System.Windows.MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        });
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            System.Windows.MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        });
    }

    public async Task<string?> ShowOpenFileDialogAsync(string filter = "所有文件|*.*")
    {
        return await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = filter
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        });
    }

    public async Task<string?> ShowSaveFileDialogAsync(string filter = "所有文件|*.*")
    {
        return await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        });
    }
}