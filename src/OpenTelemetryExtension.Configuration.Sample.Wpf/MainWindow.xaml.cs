using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace OpenTelemetryExtension.Configuration.Sample.Wpf;

public partial class MainWindow : Window
{
    public static readonly ActivitySource ActivitySource = new("Sample.Wpf");
    private static readonly Meter Meter = new("Sample.Wpf");
    private static readonly Counter<long> WorkCounter = Meter.CreateCounter<long>("sample.wpf.work.count");
    private static readonly HttpClient Http = new();

    private readonly ILogger<MainWindow> _logger;
    private int _clicks;

    public MainWindow(ILogger<MainWindow> logger)
    {
        _logger = logger;
        InitializeComponent();
    }

    private async void OnDoWorkClick(object sender, RoutedEventArgs e)
    {
        WorkButton.IsEnabled = false;
        using var activity = ActivitySource.StartActivity("DoTracedWork");
        activity?.SetTag("sample.click", ++_clicks);

        _logger.LogInformation("Doing traced work (click {Click})", _clicks);
        WorkCounter.Add(1);

        try
        {
            // The outgoing request is captured automatically by the built-in HttpClient instrumentation.
            using var response = await Http.GetAsync("https://example.com");
            StatusText.Text = $"Done — span, metric and log exported (HTTP {(int)response.StatusCode}, {_clicks} clicks).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Traced work failed");
            StatusText.Text = "Work failed — see the exported logs for details.";
        }
        finally
        {
            WorkButton.IsEnabled = true;
        }
    }
}
