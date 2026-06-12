namespace OpenTelemetryExtension.Configuration.IntegrationTests.Utils;

// A Fact that skips itself when the OpenObserve backend is not reachable, so the
// suite stays green on machines without the telemetry stack running.
public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute()
    {
        if (!Reachability.OpenObserveAvailable)
        {
            Skip = "OpenObserve is not reachable on localhost:30117 — start it via infrastructure/helm/helm-install-openobserve.cmd.";
        }
    }
}
