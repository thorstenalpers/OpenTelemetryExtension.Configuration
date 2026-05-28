namespace OpenTelemetryExtension.Configuration;

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

/// <summary>
/// Configuration options for OpenTelemetry logging, tracing and metrics.
/// </summary>
/// <example>
/// appsettings.json:
/// <code>
/// "Telemetry": {
///   "Endpoint":                        "http://localhost:4318",
///   "Enabled":                         true,
///   "Headers":                         "",
///   "Protocol":                        "HttpProtobuf",
///   "ServiceName":                     null,
///   "EnvironmentName":                 null,
///   "EnableTracing":                   true,
///   "EnableMetrics":                   true,
///   "EnableLogging":                   true,
///   "EnableAspNetCoreInstrumentation": true,
///   "EnableHttpClientInstrumentation": true,
///   "EnableSqlClientInstrumentation":  true,
///   "EnableRuntimeInstrumentation":    true,
///   "RecordExceptions":                true,
///   "ExcludeHealthChecks":             true
/// }
/// </code>
/// </example>
public sealed class TelemetryOptions
{
    /// <summary>Configuration section name within <c>appsettings.json</c>.</summary>
    public const string SectionName = "Telemetry";

    /// <summary>
    /// Whether telemetry is enabled at all.
    /// If <c>false</c>, no OpenTelemetry services are registered.
    /// Default: <c>true</c>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// OTLP endpoint for logs, traces and metrics.
    /// Example: <c>http://localhost:4318</c>
    /// Default: <c>null</c> (required)
    /// </summary>
    [Required]
    public Uri? Endpoint { get; set; } = null;

    /// <summary>
    /// Optional OTLP exporter headers, e.g. for authentication.
    /// Format: <c>key1=value1,key2=value2</c>
    /// Default: <c>""</c>
    /// </summary>
    public string Headers { get; set; } = string.Empty;

    /// <summary>
    /// OTLP export protocol.
    /// Valid values: <c>HttpProtobuf</c> (port 4318), <c>Grpc</c> (port 4317).
    /// Default: <c>Grpc</c>
    /// </summary>
    public OtlpExportProtocol Protocol { get; set; } = OtlpExportProtocol.Grpc;

    /// <summary>
    /// Logical service name reported to the telemetry backend.
    /// Default: <c>null</c>
    /// </summary>
    public string? ServiceName { get; set; } = null;

    /// <summary>
    /// Environment name reported as <c>deployment.environment</c> resource attribute.
    /// Example: <c>production</c>, <c>staging</c>
    /// Default: <c>null</c>
    /// </summary>
    public string? EnvironmentName { get; set; } = null;

    /// <summary>
    /// Whether distributed tracing is enabled.
    /// Default: <c>true</c>
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Whether metrics collection is enabled.
    /// Default: <c>true</c>
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Whether log export via OpenTelemetry is enabled.
    /// Default: <c>true</c>
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Whether incoming ASP.NET Core HTTP requests are instrumented.
    /// Default: <c>true</c>
    /// </summary>
    public bool EnableAspNetCoreInstrumentation { get; set; } = true;

    /// <summary>
    /// Whether outgoing <see cref="System.Net.Http.HttpClient"/> requests are instrumented.
    /// Default: <c>true</c>
    /// </summary>
    public bool EnableHttpClientInstrumentation { get; set; } = true;

    /// <summary>
    /// Whether SQL database calls via <c>SqlClient</c> are instrumented.
    /// Default: <c>true</c>
    /// </summary>
    public bool EnableSqlClientInstrumentation { get; set; } = true;

    /// <summary>
    /// Whether .NET runtime metrics (GC, memory, thread pool) are collected.
    /// Default: <c>true</c>
    /// </summary>
    public bool EnableRuntimeInstrumentation { get; set; } = true;

    /// <summary>
    /// Whether exceptions including stack traces are recorded on spans.
    /// Disable in high-throughput environments to reduce memory pressure.
    /// Default: <c>true</c>
    /// </summary>
    public bool RecordExceptions { get; set; } = true;

    /// <summary>
    /// Whether <c>/health</c> endpoints are excluded from tracing.
    /// Recommended in production to reduce telemetry noise.
    /// Default: <c>true</c>
    /// </summary>
    public bool ExcludeHealthChecks { get; set; } = true;

    /// <summary>
    /// Optional callback to register additional tracing instrumentation.
    /// Not configurable via <c>appsettings.json</c>.
    /// Example: <c>tracing => tracing.AddSource("MyApp")</c>
    /// Default: <c>null</c>
    /// </summary>
    public Action<TracerProviderBuilder>? ConfigureTracing { get; set; } = null;

    /// <summary>
    /// Optional callback to register additional metrics instrumentation.
    /// Not configurable via <c>appsettings.json</c>.
    /// Example: <c>metrics => metrics.AddMeter("MyApp")</c>
    /// Default: <c>null</c>
    /// </summary>
    public Action<MeterProviderBuilder>? ConfigureMetrics { get; set; } = null;

    /// <summary>
    /// Optional callback to further configure the logging pipeline,
    /// invoked before OpenTelemetry logging is added.
    /// Not configurable via <c>appsettings.json</c>.
    /// Example: <c>logging => logging.AddConsole()</c>
    /// Default: <c>null</c>
    /// </summary>
    public Action<ILoggingBuilder>? ConfigureLogging { get; set; } = null;
}