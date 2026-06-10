namespace OpenTelemetryExtension.Configuration;

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
#if !NETSTANDARD2_0
using Microsoft.AspNetCore.Http;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// Extension methods for registering OpenTelemetry services on <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// Two registration overloads are provided:
/// <list type="bullet">
///   <item><description><see cref="AddTelemetry(IServiceCollection, IConfiguration, string)"/> — binds from <c>appsettings.json</c>.</description></item>
///   <item><description><see cref="AddTelemetry(IServiceCollection, Action{TelemetryOptions})"/> — configures inline in code.</description></item>
/// </list>
/// When <see cref="TelemetryOptions.Enabled"/> is <c>false</c> no OpenTelemetry
/// services are registered and the method returns immediately.
/// </remarks>
public static class TelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Registers OpenTelemetry tracing, metrics and logging using values
    /// from the <c>Telemetry</c> section of <paramref name="configuration"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">
    /// The application configuration. Must contain a section matching
    /// <see cref="TelemetryOptions"/>. See <see cref="TelemetryOptions.SectionName"/>.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section to bind. Defaults to
    /// <see cref="TelemetryOptions.SectionName"/> (<c>Telemetry</c>) when <c>null</c> or empty.
    /// </param>
    /// <returns>The original <paramref name="services"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration section is missing or empty.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Thrown when the bound <see cref="TelemetryOptions"/> fail validation,
    /// e.g. when <see cref="TelemetryOptions.Endpoint"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration, string? sectionName = null)
        => services.AddTelemetry(configuration, configure: null, sectionName);

    /// <summary>
    /// Registers OpenTelemetry tracing, metrics and logging by binding the
    /// <c>Telemetry</c> section of <paramref name="configuration"/> and then
    /// applying <paramref name="configure"/> on top. Both sources are combined:
    /// values bound from configuration can be overridden in code, and code-only
    /// options such as <see cref="TelemetryOptions.ConfigureTracing"/> can be set.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">
    /// The application configuration. Must contain a <c>Telemetry</c> section
    /// matching <see cref="TelemetryOptions"/>. See <see cref="TelemetryOptions.SectionName"/>.
    /// </param>
    /// <param name="configure">
    /// An optional delegate applied after binding, e.g. to register additional
    /// instrumentation via <see cref="TelemetryOptions.ConfigureTracing"/>.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section to bind. Defaults to
    /// <see cref="TelemetryOptions.SectionName"/> (<c>Telemetry</c>) when <c>null</c> or empty.
    /// </param>
    /// <returns>The original <paramref name="services"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration section is missing or empty.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Thrown when the resulting <see cref="TelemetryOptions"/> fail validation,
    /// e.g. when <see cref="TelemetryOptions.Endpoint"/> is <c>null</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddTelemetry(builder.Configuration, o =>
    /// {
    ///     o.ConfigureTracing = tracing => tracing.AddSource("MyApp");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration, Action<TelemetryOptions>? configure, string? sectionName = null)
    {
        var name = string.IsNullOrWhiteSpace(sectionName) ? TelemetryOptions.SectionName : sectionName!;
        var section = configuration.GetSection(name);
        if (!section.Exists())
        {
            throw new InvalidOperationException($"Configuration section '{name}' is missing.");
        }

        var options = new TelemetryOptions();
        section.Bind(options);
        configure?.Invoke(options);

        if (options.Enabled)
        {
            Validator.ValidateObject(options, new ValidationContext(options), validateAllProperties: true);
        }

        ConfigureTelemetry(services, options);
        return services;
    }

    /// <summary>
    /// Registers OpenTelemetry tracing, metrics and logging using a configuration callback.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">
    /// A delegate to configure <see cref="TelemetryOptions"/>.
    /// At minimum <see cref="TelemetryOptions.Endpoint"/> must be set.
    /// </param>
    /// <returns>The original <paramref name="services"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Thrown when the configured <see cref="TelemetryOptions"/> fail validation,
    /// e.g. when <see cref="TelemetryOptions.Endpoint"/> is <c>null</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddTelemetry(o =>
    /// {
    ///     o.Endpoint    = new Uri("http://localhost:4318");
    ///     o.ServiceName = "my-api";
    ///     o.ConfigureTracing = tracing => tracing.AddSource("MyApp");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddTelemetry(this IServiceCollection services, Action<TelemetryOptions> configure)
    {
        var options = new TelemetryOptions();
        configure(options);

        Validator.ValidateObject(options, new ValidationContext(options), validateAllProperties: true);

        ConfigureTelemetry(services, options);
        return services;
    }

    private static void ConfigureTelemetry(IServiceCollection services, TelemetryOptions options)
    {
        if (!options.Enabled)
        {
            return;
        }

        var endpoint = options.Endpoint!;
        var serviceVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        var builder = services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                if (!string.IsNullOrWhiteSpace(options.ServiceName))
                {
                    resource.AddService(serviceName: options.ServiceName!, serviceVersion: serviceVersion);
                }

                if (options.ResourceAttributes.Count > 0)
                {
                    resource.AddAttributes(options.ResourceAttributes.Select(kv => new KeyValuePair<string, object>(kv.Key, kv.Value)));
                }
            });

        if (options.EnableTracing)
        {
            builder.WithTracing(tracing =>
            {
                tracing.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.SampleRatio)));

#if !NETSTANDARD2_0
                if (options.EnableAspNetCoreInstrumentation)
                {
                    tracing.AddAspNetCoreInstrumentation(opt =>
                    {
                        opt.RecordException = options.RecordExceptions;
                        if (options.ExcludedPaths.Length > 0)
                        {
                            opt.Filter = CreateRequestFilter(options.ExcludedPaths);
                        }
                    });
                }
#endif

                if (options.EnableHttpClientInstrumentation)
                {
                    tracing.AddHttpClientInstrumentation(opt => opt.RecordException = options.RecordExceptions);
                }

                foreach (var source in options.AdditionalTracingSources)
                {
                    tracing.AddSource(source);
                }

                options.ConfigureTracing?.Invoke(tracing);

                tracing.AddOtlpExporter(exp =>
                {
#pragma warning disable CS0618 // OtlpExportProtocol.Grpc is intentionally supported; warning only applies to netstandard2.0 without HttpClientFactory
                    exp.Endpoint = options.Protocol == OtlpExportProtocol.Grpc ? endpoint : new Uri($"{endpoint}/v1/traces");
#pragma warning restore CS0618
                    exp.Protocol = options.Protocol;
                    exp.Headers = options.Headers;
                });
            });
        }

        if (options.EnableMetrics)
        {
            builder.WithMetrics(metrics =>
            {
#if !NETSTANDARD2_0
                if (options.EnableAspNetCoreInstrumentation)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }
#endif

                if (options.EnableHttpClientInstrumentation)
                {
                    metrics.AddHttpClientInstrumentation();
                }

                if (options.EnableRuntimeInstrumentation)
                {
                    metrics.AddRuntimeInstrumentation();
                }

                foreach (var meter in options.AdditionalMeters)
                {
                    metrics.AddMeter(meter);
                }

                options.ConfigureMetrics?.Invoke(metrics);

                metrics.AddOtlpExporter(exp =>
                {
#pragma warning disable CS0618 // OtlpExportProtocol.Grpc is intentionally supported; warning only applies to netstandard2.0 without HttpClientFactory
                    exp.Endpoint = options.Protocol == OtlpExportProtocol.Grpc ? endpoint : new Uri($"{endpoint}/v1/metrics");
#pragma warning restore CS0618
                    exp.Protocol = options.Protocol;
                    exp.Headers = options.Headers;
                });
            });
        }

        if (options.EnableLogging)
        {
            services.AddLogging(logging =>
            {
                options.ConfigureLogging?.Invoke(logging);

                logging.AddOpenTelemetry(otel =>
                {
                    otel.IncludeScopes = options.IncludeScopes;
                    otel.IncludeFormattedMessage = options.IncludeFormattedMessage;

                    otel.AddOtlpExporter(exp =>
                    {
#pragma warning disable CS0618 // OtlpExportProtocol.Grpc is intentionally supported; warning only applies to netstandard2.0 without HttpClientFactory
                        exp.Endpoint = options.Protocol == OtlpExportProtocol.Grpc ? endpoint : new Uri($"{endpoint}/v1/logs");
#pragma warning restore CS0618
                        exp.Protocol = options.Protocol;
                        exp.Headers = options.Headers;
                    });
                });
            });
        }
    }

#if !NETSTANDARD2_0
    internal static bool ShouldInstrument(PathString path, string[] excludedPaths)
    {
        return !excludedPaths.Any(p => path.StartsWithSegments(p));
    }

    internal static Func<HttpContext, bool> CreateRequestFilter(string[] excludedPaths)
    {
        return ctx => ShouldInstrument(ctx.Request.Path, excludedPaths);
    }
#endif
}
