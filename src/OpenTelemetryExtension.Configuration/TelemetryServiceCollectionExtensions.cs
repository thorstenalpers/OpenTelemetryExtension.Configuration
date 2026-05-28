using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace OpenTelemetryExtension.Configuration
{
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
            var options = configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>()
                ?? throw new InvalidOperationException("Telemetry configuration missing.");

            return services.AddTelemetry(o =>
            {
                o.Enabled = options.Enabled;
                o.Endpoint = options.Endpoint;
                o.Headers = options.Headers;
                o.Protocol = options.Protocol;
                o.ServiceName = options.ServiceName;
                o.EnvironmentName = options.EnvironmentName;
                o.EnableTracing = options.EnableTracing;
                o.EnableMetrics = options.EnableMetrics;
                o.EnableLogging = options.EnableLogging;
                o.EnableAspNetCoreInstrumentation = options.EnableAspNetCoreInstrumentation;
                o.EnableHttpClientInstrumentation = options.EnableHttpClientInstrumentation;
                o.EnableSqlClientInstrumentation = options.EnableSqlClientInstrumentation;
                o.EnableRuntimeInstrumentation = options.EnableRuntimeInstrumentation;
                o.RecordExceptions = options.RecordExceptions;
                o.ExcludeHealthChecks = options.ExcludeHealthChecks;
            });
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
            if (options.Endpoint is null)
                throw new ValidationException($"{nameof(TelemetryOptions.Endpoint)} is required.");

            ConfigureTelemetry(services, options);
            return services;
        }

        private static void ConfigureTelemetry(IServiceCollection services, TelemetryOptions options)
        {
            if (!options.Enabled)
                return;

            var serviceVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            var builder = services.AddOpenTelemetry()
                .ConfigureResource(resource =>
                {
                    if (!string.IsNullOrWhiteSpace(options.ServiceName))
                        resource.AddService(serviceName: options.ServiceName, serviceVersion: serviceVersion);

                    if (!string.IsNullOrWhiteSpace(options.EnvironmentName))
                        resource.AddAttributes(new List<KeyValuePair<string, object>>
                        {
                            new KeyValuePair<string, object>("deployment.environment", options.EnvironmentName)
                        });
                });

            if (options.EnableTracing)
            {
                builder.WithTracing(tracing =>
                {
                    if (options.EnableAspNetCoreInstrumentation)
                    {
                        tracing.AddAspNetCoreInstrumentation(opt =>
                        {
                            opt.RecordException = options.RecordExceptions;
                            if (options.ExcludeHealthChecks)
                                opt.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                        });
                    }

                    if (options.EnableHttpClientInstrumentation)
                        tracing.AddHttpClientInstrumentation(opt => opt.RecordException = options.RecordExceptions);

                    if (options.EnableSqlClientInstrumentation)
                        tracing.AddSqlClientInstrumentation(opt => opt.RecordException = options.RecordExceptions);

                    options.ConfigureTracing?.Invoke(tracing);

                    tracing.AddOtlpExporter(exp =>
                    {
                        exp.Endpoint = options.Protocol == OtlpExportProtocol.Grpc ? options.Endpoint! : new Uri($"{options.Endpoint!}/v1/traces");
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
                        metrics.AddAspNetCoreInstrumentation();

                    if (options.EnableHttpClientInstrumentation)
                        metrics.AddHttpClientInstrumentation();

                    if (options.EnableRuntimeInstrumentation)
                        metrics.AddRuntimeInstrumentation();

                    options.ConfigureMetrics?.Invoke(metrics);

                    metrics.AddOtlpExporter(exp =>
                    {
                        exp.Endpoint = new Uri($"{options.Endpoint}/v1/metrics");
                        exp.Endpoint = options.Protocol == OtlpExportProtocol.Grpc ? options.Endpoint! : new Uri($"{options.Endpoint!}/v1/metrics");
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
                        otel.IncludeScopes = true;
                        otel.IncludeFormattedMessage = true;

                        otel.AddOtlpExporter(exp =>
                        {
                            exp.Endpoint = options.Protocol == OtlpExportProtocol.Grpc ? options.Endpoint! : new Uri($"{options.Endpoint!}/v1/logs");
                            exp.Protocol = options.Protocol;
                            exp.Headers = options.Headers;
                        });
                    });
                });
            }
        }
    }
}