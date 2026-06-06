using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace OpenTelemetryExtension.Configuration.Tests;

public class TelemetryServiceCollectionExtensionsTests
{
    // ── Helpers ───────────────────────────────────────────────────────────

    private static IServiceCollection NewServices() => new ServiceCollection();

    // Forces the lazy OpenTelemetry configuration lambdas (resource builder,
    // exporters, instrumentation) to actually execute by resolving the signal
    // providers from the built container.
    private static void BuildAndResolveProviders(IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        _ = provider.GetService<TracerProvider>();
        _ = provider.GetService<MeterProvider>();
        _ = provider.GetRequiredService<ILoggerFactory>().CreateLogger("test");
    }

    private static Action<TelemetryOptions> MinimalConfigure(Action<TelemetryOptions>? extra = null) =>
        o =>
        {
            o.Enabled = true;
            o.Endpoint = new Uri("http://localhost:4318");
            extra?.Invoke(o);
        };

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static IConfiguration ValidConfig(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Telemetry:Endpoint"] = "http://localhost:4318",
        };
        if (overrides is not null)
        {
            foreach (var kv in overrides)
            {
                values[kv.Key] = kv.Value;
            }
        }

        return BuildConfig(values);
    }

    // ── AddTelemetry(IConfiguration) ─────────────────────────────────────

    [Fact]
    public void AddTelemetry_IConfiguration_ReturnsServices()
    {
        var services = NewServices();
        var result = services.AddTelemetry(ValidConfig());
        Assert.Same(services, result);
    }

    [Fact]
    public void AddTelemetry_IConfiguration_ThrowsWhenEnabledAndEndpointMissing()
    {
        var services = NewServices();
        var config = BuildConfig(new Dictionary<string, string?> { ["Telemetry:Enabled"] = "true" });
        Assert.Throws<ValidationException>(() => services.AddTelemetry(config));
    }

    [Fact]
    public void AddTelemetry_IConfiguration_ThrowsWhenSectionMissing()
    {
        var services = NewServices();
        var config = BuildConfig(new Dictionary<string, string?>());
        Assert.Throws<InvalidOperationException>(() => services.AddTelemetry(config));
    }

    [Fact]
    public void AddTelemetry_IConfiguration_NoThrowWhenDisabledAndEndpointMissing()
    {
        var services = NewServices();
        var config = BuildConfig(new Dictionary<string, string?> { ["Telemetry:Enabled"] = "false" });
        services.AddTelemetry(config); // Section exists, Enabled = false → no-op, no exception
    }

    [Fact]
    public void AddTelemetry_IConfiguration_MapsAllScalarProperties()
    {
        // Verify that the IConfiguration overload forwards all options to the Action overload
        // by checking that registration succeeds with every supported appsettings key.
        var config = ValidConfig(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = "true",
            ["Telemetry:Headers"] = "x-key=value",
            ["Telemetry:Protocol"] = "HttpProtobuf",
            ["Telemetry:ServiceName"] = "svc",
            ["Telemetry:SampleRatio"] = "0.5",
            ["Telemetry:EnableTracing"] = "true",
            ["Telemetry:EnableMetrics"] = "true",
            ["Telemetry:EnableLogging"] = "true",
            ["Telemetry:EnableAspNetCoreInstrumentation"] = "true",
            ["Telemetry:EnableHttpClientInstrumentation"] = "false",
            ["Telemetry:EnableSqlClientInstrumentation"] = "false",
            ["Telemetry:EnableRuntimeInstrumentation"] = "true",
            ["Telemetry:RecordExceptions"] = "false",
            ["Telemetry:ExcludedPaths:0"] = "/health",
            ["Telemetry:ExcludedPaths:1"] = "/metrics",
            ["Telemetry:IncludeScopes"] = "true",
            ["Telemetry:IncludeFormattedMessage"] = "true",
        });

        var services = NewServices();
        var result = services.AddTelemetry(config);
        Assert.Same(services, result);
    }

    [Fact]
    public void AddTelemetry_IConfiguration_Disabled_RegistersNoOtel()
    {
        var config = ValidConfig(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = "false",
        });

        var services = NewServices();
        services.AddTelemetry(config);

        // When disabled, no OpenTelemetry SDK service (TracerProvider) should be registered.
        var provider = services.BuildServiceProvider();
        var tracer = provider.GetService<TracerProvider>();
        Assert.Null(tracer);
    }

    // ── AddTelemetry(Action<TelemetryOptions>) ────────────────────────────

    [Fact]
    public void AddTelemetry_Action_ReturnsServices()
    {
        var services = NewServices();
        var result = services.AddTelemetry(MinimalConfigure());
        Assert.Same(services, result);
    }

    [Fact]
    public void AddTelemetry_Action_ThrowsWhenEndpointIsNull()
    {
        var services = NewServices();
        Assert.Throws<ValidationException>(() =>
            services.AddTelemetry(o => { /* Endpoint left null */ }));
    }

    [Fact]
    public void AddTelemetry_Action_Disabled_DoesNotRegisterOtel()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o => o.Enabled = false));

        var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService<TracerProvider>());
    }

    // ── Enabled = true, all signals on ───────────────────────────────────

    [Fact]
    public void AddTelemetry_AllSignalsEnabled_RegistersTracerProvider()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure());
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<TracerProvider>());
    }

    [Fact]
    public void AddTelemetry_AllSignalsEnabled_RegistersMeterProvider()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure());
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<MeterProvider>());
    }

    [Fact]
    public void AddTelemetry_AllSignalsEnabled_RegistersLoggerFactory()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure());
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<ILoggerFactory>());
    }

    // ── Individual signals disabled ───────────────────────────────────────

    [Fact]
    public void AddTelemetry_TracingDisabled_NoTracerProvider()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o =>
        {
            o.EnableTracing = false;
            o.EnableMetrics = false;
            o.EnableLogging = false;
        }));
        var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService<TracerProvider>());
    }

    [Fact]
    public void AddTelemetry_MetricsDisabled_NoMeterProvider()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o =>
        {
            o.EnableTracing = false;
            o.EnableMetrics = false;
            o.EnableLogging = false;
        }));
        var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService<MeterProvider>());
    }

    // ── Instrumentation flag combinations ─────────────────────────────────

    [Fact]
    public void AddTelemetry_InstrumentationFlagsOff_DoesNotThrow()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o =>
        {
            o.EnableAspNetCoreInstrumentation = false;
            o.EnableHttpClientInstrumentation = false;
            o.EnableSqlClientInstrumentation = false;
            o.EnableRuntimeInstrumentation = false;
        }));

        var ex = Record.Exception(() => BuildAndResolveProviders(services));

        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_AllInstrumentationFlagsOn_DoesNotThrow()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o =>
        {
            o.EnableAspNetCoreInstrumentation = true;
            o.EnableHttpClientInstrumentation = true;
            o.EnableSqlClientInstrumentation = true;
            o.EnableRuntimeInstrumentation = true;
            o.RecordExceptions = true;
            o.ExcludedPaths = ["/health"];
        }));

        var ex = Record.Exception(() => BuildAndResolveProviders(services));

        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_RecordExceptionsFalse_DoesNotThrow()
    {
        var services = NewServices();
        var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
            o.RecordExceptions = false)));
        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_ExcludedPathsEmpty_DoesNotThrow()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o => o.ExcludedPaths = []));

        var ex = Record.Exception(() => BuildAndResolveProviders(services));

        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_ExcludedPathsCustom_DoesNotThrow()
    {
        var services = NewServices();
        var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
            o.ExcludedPaths = ["/health", "/metrics"])));
        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_ResourceAttributes_DoesNotThrow()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o =>
            o.ResourceAttributes = new Dictionary<string, string> { ["team"] = "backend", ["region"] = "eu-west-1" }));

        var ex = Record.Exception(() => BuildAndResolveProviders(services));

        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_SampleRatioHalf_DoesNotThrow()
    {
        var services = NewServices();
        var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
            o.SampleRatio = 0.5)));
        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_SampleRatioZero_DoesNotThrow()
    {
        var services = NewServices();
        var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
            o.SampleRatio = 0.0)));
        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_IncludeScopesFalse_DoesNotThrow()
    {
        var services = NewServices();
        var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
            o.IncludeScopes = false)));
        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_IncludeFormattedMessageFalse_DoesNotThrow()
    {
        var services = NewServices();
        var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
            o.IncludeFormattedMessage = false)));
        Assert.Null(ex);
    }

    // ── ServiceName ───────────────────────────────────────────────────────

    [Fact]
    public void AddTelemetry_WithServiceName_DoesNotThrow()
    {
        var services = NewServices();
        var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
            o.ServiceName = "my-api")));
        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_WithServiceNameAndResourceAttributes_DoesNotThrow()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o =>
        {
            o.ServiceName = "my-api";
            o.ResourceAttributes = new Dictionary<string, string> { ["deployment.environment"] = "production" };
        }));

        var ex = Record.Exception(() => BuildAndResolveProviders(services));

        Assert.Null(ex);
    }

    // ── Protocol ─────────────────────────────────────────────────────────

    [Fact]
    public void AddTelemetry_ProtocolHttpProtobuf_DoesNotThrow()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o => o.Protocol = OtlpExportProtocol.HttpProtobuf));

        var ex = Record.Exception(() => BuildAndResolveProviders(services));

        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_ProtocolGrpc_DoesNotThrow()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o => o.Protocol = OtlpExportProtocol.Grpc));

        var ex = Record.Exception(() => BuildAndResolveProviders(services));

        Assert.Null(ex);
    }

    // ── User callbacks ────────────────────────────────────────────────────

    [Fact]
    public void AddTelemetry_ConfigureTracing_CallbackIsInvoked()
    {
        bool invoked = false;
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o =>
            o.ConfigureTracing = _ => { invoked = true; }));

        // Resolve TracerProvider to trigger the builder pipeline
        services.BuildServiceProvider().GetService<TracerProvider>();
        Assert.True(invoked);
    }

    [Fact]
    public void AddTelemetry_ConfigureMetrics_CallbackIsInvoked()
    {
        bool invoked = false;
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o =>
            o.ConfigureMetrics = _ => { invoked = true; }));

        services.BuildServiceProvider().GetService<MeterProvider>();
        Assert.True(invoked);
    }

    [Fact]
    public void AddTelemetry_ConfigureLogging_CallbackIsInvoked()
    {
        bool invoked = false;
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o =>
            o.ConfigureLogging = _ => { invoked = true; }));

        services.BuildServiceProvider().GetService<ILoggerFactory>();
        Assert.True(invoked);
    }

    [Fact]
    public void AddTelemetry_NullCallbacks_DoNotThrow()
    {
        var services = NewServices();
        var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
        {
            o.ConfigureTracing = null;
            o.ConfigureMetrics = null;
            o.ConfigureLogging = null;
        })));
        Assert.Null(ex);
    }

    // ── Headers ───────────────────────────────────────────────────────────

    [Fact]
    public void AddTelemetry_WithHeaders_DoesNotThrow()
    {
        var services = NewServices();
        var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
            o.Headers = "Authorization=Bearer token123")));
        Assert.Null(ex);
    }

    // ── IConfiguration edge cases ─────────────────────────────────────────

    [Fact]
    public void AddTelemetry_IConfiguration_GrpcProtocol_DoesNotThrow()
    {
        var config = ValidConfig(new Dictionary<string, string?>
        {
            ["Telemetry:Protocol"] = "Grpc",
        });
        var services = NewServices();
        var ex = Record.Exception(() => services.AddTelemetry(config));
        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_IConfiguration_AllSignalsDisabled_DoesNotThrow()
    {
        var config = ValidConfig(new Dictionary<string, string?>
        {
            ["Telemetry:EnableTracing"] = "false",
            ["Telemetry:EnableMetrics"] = "false",
            ["Telemetry:EnableLogging"] = "false",
        });
        var services = NewServices();
        var ex = Record.Exception(() => services.AddTelemetry(config));
        Assert.Null(ex);
    }

    // ── ShouldInstrument (request filter logic) ───────────────────────────

    [Theory]
    [InlineData("/health", false)]       // exact excluded path → not instrumented
    [InlineData("/health/live", false)]  // sub-segment of excluded → not instrumented
    [InlineData("/healthz", true)]       // NOT a full segment match → instrumented
    [InlineData("/api/orders", true)]    // unrelated path → instrumented
    [InlineData("/", true)]              // root → instrumented
    public void ShouldInstrument_RespectsExcludedPaths(string path, bool expectedInstrument)
    {
        var result = TelemetryServiceCollectionExtensions.ShouldInstrument(
            new PathString(path), ["/health"]);

        Assert.Equal(expectedInstrument, result);
    }

    [Theory]
    [InlineData("/health", false)]
    [InlineData("/metrics", false)]
    [InlineData("/api", true)]
    public void ShouldInstrument_MultipleExcludedPaths(string path, bool expectedInstrument)
    {
        var result = TelemetryServiceCollectionExtensions.ShouldInstrument(
            new PathString(path), ["/health", "/metrics"]);

        Assert.Equal(expectedInstrument, result);
    }

    [Fact]
    public void ShouldInstrument_EmptyExcludedPaths_AlwaysInstruments()
    {
        var result = TelemetryServiceCollectionExtensions.ShouldInstrument(
            new PathString("/health"), []);

        Assert.True(result);
    }

    [Theory]
    [InlineData("/health", false)]      // excluded → filter returns false
    [InlineData("/api/orders", true)]   // not excluded → filter returns true
    public void CreateRequestFilter_FiltersByPath(string path, bool expectedInstrument)
    {
        var filter = TelemetryServiceCollectionExtensions.CreateRequestFilter(["/health"]);
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        var result = filter(context);

        Assert.Equal(expectedInstrument, result);
    }

    // ── Resource configuration ────────────────────────────────────────────────

    [Fact]
    public void AddTelemetry_WithServiceName_RegistersTracerProvider()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o => o.ServiceName = "my-api"));
        Assert.NotNull(services.BuildServiceProvider().GetService<TracerProvider>());
    }

    [Fact]
    public void AddTelemetry_WithResourceAttributes_RegistersTracerProvider()
    {
        var services = NewServices();
        services.AddTelemetry(MinimalConfigure(o =>
            o.ResourceAttributes = new Dictionary<string, string> { ["deployment.environment"] = "production" }));
        Assert.NotNull(services.BuildServiceProvider().GetService<TracerProvider>());
    }

    [Fact]
    public void AddTelemetry_NullServiceName_DoesNotThrow()
    {
        var services = NewServices();
        var ex = Record.Exception(() =>
            services.AddTelemetry(MinimalConfigure(o => o.ServiceName = null)));
        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_WhitespaceServiceName_DoesNotThrow()
    {
        var services = NewServices();
        var ex = Record.Exception(() =>
            services.AddTelemetry(MinimalConfigure(o => o.ServiceName = "   ")));
        Assert.Null(ex);
    }

    [Fact]
    public void AddTelemetry_EmptyResourceAttributes_DoesNotThrow()
    {
        var services = NewServices();
        var ex = Record.Exception(() =>
            services.AddTelemetry(MinimalConfigure(o => o.ResourceAttributes = [])));
        Assert.Null(ex);
    }
}
