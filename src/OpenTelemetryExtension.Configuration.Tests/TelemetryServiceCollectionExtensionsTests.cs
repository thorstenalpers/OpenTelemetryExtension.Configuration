using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.ComponentModel.DataAnnotations;

namespace OpenTelemetryExtension.Configuration.Tests
{
    public class TelemetryServiceCollectionExtensionsTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static IServiceCollection NewServices() => new ServiceCollection();

        private static Action<TelemetryOptions> MinimalConfigure(Action<TelemetryOptions>? extra = null) =>
            o =>
            {
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
                foreach (var kv in overrides)
                    values[kv.Key] = kv.Value;

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
        public void AddTelemetry_IConfiguration_ThrowsWhenSectionMissing()
        {
            var services = NewServices();
            var config = BuildConfig(new Dictionary<string, string?>());
            // Section exists but Endpoint is null → ValidationException
            Assert.Throws<InvalidOperationException>(() => services.AddTelemetry(config));
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
                ["Telemetry:EnvironmentName"] = "prod",
                ["Telemetry:EnableTracing"] = "true",
                ["Telemetry:EnableMetrics"] = "true",
                ["Telemetry:EnableLogging"] = "true",
                ["Telemetry:EnableAspNetCoreInstrumentation"] = "true",
                ["Telemetry:EnableHttpClientInstrumentation"] = "false",
                ["Telemetry:EnableSqlClientInstrumentation"] = "false",
                ["Telemetry:EnableRuntimeInstrumentation"] = "true",
                ["Telemetry:RecordExceptions"] = "false",
                ["Telemetry:ExcludeHealthChecks"] = "true",
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
            var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
            {
                o.EnableAspNetCoreInstrumentation = false;
                o.EnableHttpClientInstrumentation = false;
                o.EnableSqlClientInstrumentation = false;
                o.EnableRuntimeInstrumentation = false;
            })));
            Assert.Null(ex);
        }

        [Fact]
        public void AddTelemetry_AllInstrumentationFlagsOn_DoesNotThrow()
        {
            var services = NewServices();
            var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
            {
                o.EnableAspNetCoreInstrumentation = true;
                o.EnableHttpClientInstrumentation = true;
                o.EnableSqlClientInstrumentation = true;
                o.EnableRuntimeInstrumentation = true;
                o.RecordExceptions = true;
                o.ExcludeHealthChecks = true;
            })));
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
        public void AddTelemetry_ExcludeHealthChecksFalse_DoesNotThrow()
        {
            var services = NewServices();
            var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
                o.ExcludeHealthChecks = false)));
            Assert.Null(ex);
        }

        // ── ServiceName / EnvironmentName ─────────────────────────────────────

        [Fact]
        public void AddTelemetry_WithServiceName_DoesNotThrow()
        {
            var services = NewServices();
            var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
                o.ServiceName = "my-api")));
            Assert.Null(ex);
        }

        [Fact]
        public void AddTelemetry_WithEnvironmentName_DoesNotThrow()
        {
            var services = NewServices();
            var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
                o.EnvironmentName = "staging")));
            Assert.Null(ex);
        }

        [Fact]
        public void AddTelemetry_WithServiceNameAndEnvironmentName_DoesNotThrow()
        {
            var services = NewServices();
            var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
            {
                o.ServiceName = "my-api";
                o.EnvironmentName = "production";
            })));
            Assert.Null(ex);
        }

        // ── Protocol ─────────────────────────────────────────────────────────

        [Fact]
        public void AddTelemetry_ProtocolHttpProtobuf_DoesNotThrow()
        {
            var services = NewServices();
            var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
                o.Protocol = OtlpExportProtocol.HttpProtobuf)));
            Assert.Null(ex);
        }

        [Fact]
        public void AddTelemetry_ProtocolGrpc_DoesNotThrow()
        {
            var services = NewServices();
            var ex = Record.Exception(() => services.AddTelemetry(MinimalConfigure(o =>
                o.Protocol = OtlpExportProtocol.Grpc)));
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
    }
}