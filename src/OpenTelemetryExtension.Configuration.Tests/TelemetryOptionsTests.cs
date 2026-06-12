using System.ComponentModel.DataAnnotations;

namespace OpenTelemetryExtension.Configuration.Tests;

[Trait("Category", "Unit")]
public sealed class TelemetryOptionsTests
{
    // ── Default values ────────────────────────────────────────────────────

    [Fact]
    public void Defaults_SectionName_IsCorrect()
        => Assert.Equal("Telemetry", TelemetryOptions.SectionName);

    [Fact]
    public void Defaults_Enabled_IsTrue()
        => Assert.True(new TelemetryOptions().Enabled);

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
    public void Defaults_AdditionalTracingSources_IsEmpty()
        => Assert.Empty(new TelemetryOptions().AdditionalTracingSources);

    [Fact]
    public void Defaults_AdditionalMeters_IsEmpty()
        => Assert.Empty(new TelemetryOptions().AdditionalMeters);

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

    // ── DataAnnotations validation ────────────────────────────────────────

    [Fact]
    public void Validation_Passes_WhenEndpointIsSet()
    {
        var o = new TelemetryOptions { Endpoint = new Uri("http://localhost:4318") };
        Validator.ValidateObject(o, new ValidationContext(o), validateAllProperties: true);
    }

    [Fact]
    public void Validation_Fails_WhenEndpointIsNull()
    {
        var o = new TelemetryOptions { Endpoint = null };
        Assert.Throws<ValidationException>(() =>
            Validator.ValidateObject(o, new ValidationContext(o), validateAllProperties: true));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void Validation_Fails_WhenSampleRatioOutOfRange(double sampleRatio)
    {
        var o = new TelemetryOptions
        {
            Endpoint = new Uri("http://localhost:4318"),
            SampleRatio = sampleRatio,
        };

        Assert.Throws<ValidationException>(() =>
            Validator.ValidateObject(o, new ValidationContext(o), validateAllProperties: true));
    }
}
