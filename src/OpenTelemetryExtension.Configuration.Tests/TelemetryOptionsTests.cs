using System.ComponentModel.DataAnnotations;

namespace OpenTelemetryExtension.Configuration.Tests;

public class TelemetryOptionsTests
{
    // ── Default values ────────────────────────────────────────────────────

    [Fact]
    public void Defaults_SectionName_IsCorrect()
        => Assert.Equal("Telemetry", TelemetryOptions.SectionName);

    [Fact]
    public void Defaults_Enabled_IsFalse()
        => Assert.False(new TelemetryOptions().Enabled);

    [Fact]
    public void Defaults_Endpoint_IsNull()
        => Assert.Null(new TelemetryOptions().Endpoint);

    [Fact]
    public void Defaults_Headers_IsEmpty()
        => Assert.Equal(string.Empty, new TelemetryOptions().Headers);

    [Fact]
    public void Defaults_Protocol_IsHttpProtobuf()
        => Assert.Equal(OtlpExportProtocol.HttpProtobuf, new TelemetryOptions().Protocol);

    [Fact]
    public void Defaults_ServiceName_IsNull()
        => Assert.Null(new TelemetryOptions().ServiceName);

    [Fact]
    public void Defaults_EnvironmentName_IsNull()
        => Assert.Null(new TelemetryOptions().EnvironmentName);

    [Fact]
    public void Defaults_EnableTracing_IsTrue()
        => Assert.True(new TelemetryOptions().EnableTracing);

    [Fact]
    public void Defaults_EnableMetrics_IsTrue()
        => Assert.True(new TelemetryOptions().EnableMetrics);

    [Fact]
    public void Defaults_EnableLogging_IsTrue()
        => Assert.True(new TelemetryOptions().EnableLogging);

    [Fact]
    public void Defaults_EnableAspNetCoreInstrumentation_IsTrue()
        => Assert.True(new TelemetryOptions().EnableAspNetCoreInstrumentation);

    [Fact]
    public void Defaults_EnableHttpClientInstrumentation_IsTrue()
        => Assert.True(new TelemetryOptions().EnableHttpClientInstrumentation);

    [Fact]
    public void Defaults_EnableSqlClientInstrumentation_IsFalse()
        => Assert.False(new TelemetryOptions().EnableSqlClientInstrumentation);

    [Fact]
    public void Defaults_EnableRuntimeInstrumentation_IsTrue()
        => Assert.True(new TelemetryOptions().EnableRuntimeInstrumentation);

    [Fact]
    public void Defaults_ResourceAttributes_IsEmpty()
        => Assert.Empty(new TelemetryOptions().ResourceAttributes);

    [Fact]
    public void Defaults_SampleRatio_IsOne()
        => Assert.Equal(1.0, new TelemetryOptions().SampleRatio);

    [Fact]
    public void Defaults_RecordExceptions_IsTrue()
        => Assert.True(new TelemetryOptions().RecordExceptions);

    [Fact]
    public void Defaults_ExcludedPaths_ContainsHealth()
        => Assert.Equal(["/health"], new TelemetryOptions().ExcludedPaths);

    [Fact]
    public void Defaults_IncludeScopes_IsTrue()
        => Assert.True(new TelemetryOptions().IncludeScopes);

    [Fact]
    public void Defaults_IncludeFormattedMessage_IsTrue()
        => Assert.True(new TelemetryOptions().IncludeFormattedMessage);

    [Fact]
    public void Defaults_ConfigureTracing_IsNull()
        => Assert.Null(new TelemetryOptions().ConfigureTracing);

    [Fact]
    public void Defaults_ConfigureMetrics_IsNull()
        => Assert.Null(new TelemetryOptions().ConfigureMetrics);

    [Fact]
    public void Defaults_ConfigureLogging_IsNull()
        => Assert.Null(new TelemetryOptions().ConfigureLogging);

    // ── Property setters ─────────────────────────────────────────────────

    [Fact]
    public void Property_Enabled_CanBeSetToFalse()
    {
        var o = new TelemetryOptions { Enabled = false };
        Assert.False(o.Enabled);
    }

    [Fact]
    public void Property_Endpoint_CanBeSet()
    {
        var uri = new Uri("http://localhost:4318");
        var o = new TelemetryOptions { Endpoint = uri };
        Assert.Equal(uri, o.Endpoint);
    }

    [Fact]
    public void Property_Headers_CanBeSet()
    {
        var o = new TelemetryOptions { Headers = "x-api-key=abc" };
        Assert.Equal("x-api-key=abc", o.Headers);
    }

    [Fact]
    public void Property_Protocol_CanBeSetToGrpc()
    {
        var o = new TelemetryOptions { Protocol = OtlpExportProtocol.Grpc };
        Assert.Equal(OtlpExportProtocol.Grpc, o.Protocol);
    }

    [Fact]
    public void Property_ServiceName_CanBeSet()
    {
        var o = new TelemetryOptions { ServiceName = "my-service" };
        Assert.Equal("my-service", o.ServiceName);
    }

    [Fact]
    public void Property_EnvironmentName_CanBeSet()
    {
        var o = new TelemetryOptions { EnvironmentName = "production" };
        Assert.Equal("production", o.EnvironmentName);
    }

    [Fact]
    public void Property_ConfigureTracing_CanBeSet()
    {
        Action<TracerProviderBuilder> cb = _ => { };
        var o = new TelemetryOptions { ConfigureTracing = cb };
        Assert.Same(cb, o.ConfigureTracing);
    }

    [Fact]
    public void Property_ConfigureMetrics_CanBeSet()
    {
        Action<MeterProviderBuilder> cb = _ => { };
        var o = new TelemetryOptions { ConfigureMetrics = cb };
        Assert.Same(cb, o.ConfigureMetrics);
    }

    [Fact]
    public void Property_ConfigureLogging_CanBeSet()
    {
        Action<Microsoft.Extensions.Logging.ILoggingBuilder> cb = _ => { };
        var o = new TelemetryOptions { ConfigureLogging = cb };
        Assert.Same(cb, o.ConfigureLogging);
    }

    // ── DataAnnotations validation ────────────────────────────────────────

    [Fact]
    public void Validation_Passes_WhenEndpointIsSet()
    {
        var o = new TelemetryOptions { Endpoint = new Uri("http://localhost:4318") };
        // Should not throw
        Validator.ValidateObject(o, new ValidationContext(o), validateAllProperties: true);
    }

    [Fact]
    public void Validation_Fails_WhenEndpointIsNull()
    {
        var o = new TelemetryOptions { Endpoint = null };
        Assert.Throws<ValidationException>(() =>
            Validator.ValidateObject(o, new ValidationContext(o), validateAllProperties: true));
    }
}
