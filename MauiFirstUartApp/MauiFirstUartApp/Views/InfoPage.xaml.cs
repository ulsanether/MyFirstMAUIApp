namespace MauiFirstUartApp.Views;

public partial class InfoPage : ContentPage
{
    public InfoPage()
    {
        InitializeComponent();
    }

    private async void OnEmailButtonClicked(object sender, EventArgs e)
    {
        var emailUri = new Uri("mailto:hangmini12@naver.com");
        await Launcher.Default.OpenAsync(emailUri);
    }

    private async void OnGitHubButtonClicked(object sender, EventArgs e)
    {
        var githubUri = new Uri("https://github.com/ulsanether");
        await Launcher.Default.OpenAsync(githubUri);
    }
}