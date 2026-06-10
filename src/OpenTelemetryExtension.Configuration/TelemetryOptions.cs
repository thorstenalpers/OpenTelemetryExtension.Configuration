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
///   "ResourceAttributes":              { "deployment.environment": "production", "team": "backend" },
///   "AdditionalTracingSources":        [ "Npgsql" ],
///   "AdditionalMeters":                [ "MyApp.Orders" ],
///   "SampleRatio":                     1.0,
///   "EnableTracing":                   true,
///   "EnableMetrics":                   true,
///   "EnableLogging":                   true,
///   "EnableAspNetCoreInstrumentation": true,
///   "EnableHttpClientInstrumentation": true,
///   "EnableRuntimeInstrumentation":    true,
///   "RecordExceptions":                true,
///   "ExcludedPaths":                   ["/health"],
///   "IncludeScopes":                   true,
///   "IncludeFormattedMessage":         true
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
    /// Default: <c>true</c>.
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
    /// Default: <c>HttpProtobuf</c> — works through standard HTTP proxies and firewalls without extra gRPC configuration.
    /// </summary>
    public OtlpExportProtocol Protocol { get; set; } = OtlpExportProtocol.HttpProtobuf;

    /// <summary>
    /// Logical service name reported to the telemetry backend.
    /// Default: <c>null</c>
    /// </summary>
    public string? ServiceName { get; set; } = null;

    /// <summary>
    /// Additional OpenTelemetry resource attributes added alongside <see cref="ServiceName"/>.
    /// Use <c>deployment.environment</c> to report the environment.
    /// Example: <c>{ "deployment.environment": "production", "team": "backend" }</c>
    /// Default: <c>{}</c>
    /// </summary>
    public Dictionary<string, string> ResourceAttributes { get; set; } = [];

    /// <summary>
    /// Additional <c>ActivitySource</c> names to collect traces from, registered via
    /// <c>AddSource</c>. Use this to enable source-based instrumentation (e.g. database
    /// drivers like <c>Npgsql</c>, <c>MySqlConnector</c>) or your own application sources
    /// without writing code.
    /// Example: <c>[ "Npgsql", "MyApp" ]</c>
    /// Default: <c>[]</c>
    /// </summary>
    public string[] AdditionalTracingSources { get; set; } = [];

    /// <summary>
    /// Additional <c>Meter</c> names to collect metrics from, registered via <c>AddMeter</c>.
    /// Example: <c>[ "MyApp.Orders" ]</c>
    /// Default: <c>[]</c>
    /// </summary>
    public string[] AdditionalMeters { get; set; } = [];

    /// <summary>
    /// Fraction of traces to sample. <c>1.0</c> samples everything, <c>0.1</c> samples 10%.
    /// Uses <c>ParentBased(TraceIdRatioBased)</c> sampler.
    /// Default: <c>1.0</c>
    /// </summary>
    [Range(0.0, 1.0)]
    public double SampleRatio { get; set; } = 1.0;

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
    /// Request paths excluded from tracing. Matched as path prefixes per segment.
    /// Default: <c>["/health"]</c>
    /// </summary>
    public string[] ExcludedPaths { get; set; } = ["/health"];

    /// <summary>
    /// Whether log scopes are included in exported log records.
    /// Default: <c>true</c>
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Whether the formatted log message is included in exported log records.
    /// Default: <c>true</c>
    /// </summary>
    public bool IncludeFormattedMessage { get; set; } = true;

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
