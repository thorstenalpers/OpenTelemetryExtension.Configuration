namespace OpenTelemetryExtension.Configuration;

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
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
///   <item><description><see cref="AddTelemetry(IServiceCollection, IConfiguration)"/> — binds from <c>appsettings.json</c>.</description></item>
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
    /// The application configuration. Must contain a <c>Telemetry</c> section
    /// matching <see cref="TelemetryOptions"/>. See <see cref="TelemetryOptions.SectionName"/>.
    /// </param>
    /// <returns>The original <paramref name="services"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>Telemetry</c> configuration section is missing or empty.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Thrown when the bound <see cref="TelemetryOptions"/> fail validation,
    /// e.g. when <see cref="TelemetryOptions.Endpoint"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(TelemetryOptions.SectionName);
        if (!section.Exists())
        {
            throw new InvalidOperationException($"Configuration section '{TelemetryOptions.SectionName}' is missing.");
        }

        var options = new TelemetryOptions();
        section.Bind(options);

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

        // Endpoint is [Required] and validated before ConfigureTelemetry is reached.
#pragma warning disable CS0618 // OtlpExportProtocol.Grpc is intentionally supported; warning only applies to netstandard2.0 without HttpClientFactory
        var endpoint = options.Endpoint!;
        var serviceVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        var builder = services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                if (!string.IsNullOrWhiteSpace(options.ServiceName))
                {
                    resource.AddService(serviceName: options.ServiceName!, serviceVersion: serviceVersion);
                }

                if (!string.IsNullOrWhiteSpace(options.EnvironmentName))
                {
                    resource.AddAttributes([new("deployment.environment", options.EnvironmentName!)]);
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

                if (options.EnableHttpClientInstrumentation)
                {
                    tracing.AddHttpClientInstrumentation(opt => opt.RecordException = options.RecordExceptions);
                }

                if (options.EnableSqlClientInstrumentation)
                {
                    tracing.AddSqlClientInstrumentation(opt => opt.RecordException = options.RecordExceptions);
                }

                options.ConfigureTracing?.Invoke(tracing);

                tracing.AddOtlpExporter(exp =>
                {
                    exp.Endpoint = options.Protocol == OtlpExportProtocol.Grpc ? endpoint : new Uri($"{endpoint}/v1/traces");
                    exp.Protocol = options.Protocol;
                    exp.Headers = options.Headers;
                });
            });
        }

        if (options.EnableMetrics)
        {
            builder.WithMetrics(metrics =>
            {
                if (options.EnableAspNetCoreInstrumentation)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }

                if (options.EnableHttpClientInstrumentation)
                {
                    metrics.AddHttpClientInstrumentation();
                }

                if (options.EnableRuntimeInstrumentation)
                {
                    metrics.AddRuntimeInstrumentation();
                }

                options.ConfigureMetrics?.Invoke(metrics);

                metrics.AddOtlpExporter(exp =>
                {
                    exp.Endpoint = options.Protocol == OtlpExportProtocol.Grpc ? endpoint : new Uri($"{endpoint}/v1/metrics");
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
                        exp.Endpoint = options.Protocol == OtlpExportProtocol.Grpc ? endpoint : new Uri($"{endpoint}/v1/logs");
                        exp.Protocol = options.Protocol;
                        exp.Headers = options.Headers;
                    });
                });
            });
        }
#pragma warning restore CS0618
    }

    /// <summary>
    /// Determines whether a request to <paramref name="path"/> should be instrumented,
    /// i.e. its path does not start (segment-wise) with any of <paramref name="excludedPaths"/>.
    /// </summary>
    /// <param name="path">The incoming request path.</param>
    /// <param name="excludedPaths">Path prefixes to exclude from instrumentation.</param>
    /// <returns><c>true</c> if the request should be instrumented; otherwise <c>false</c>.</returns>
    internal static bool ShouldInstrument(PathString path, string[] excludedPaths)
        => !excludedPaths.Any(p => path.StartsWithSegments(p));

    /// <summary>
    /// Builds the ASP.NET Core instrumentation request filter that skips
    /// requests whose path starts (segment-wise) with any of <paramref name="excludedPaths"/>.
    /// </summary>
    /// <param name="excludedPaths">Path prefixes to exclude from instrumentation.</param>
    /// <returns>A predicate returning <c>true</c> when the request should be instrumented.</returns>
    internal static Func<HttpContext, bool> CreateRequestFilter(string[] excludedPaths)
        => ctx => ShouldInstrument(ctx.Request.Path, excludedPaths);
}
