using Microsoft.EntityFrameworkCore;

namespace OpenTelemetryExtension.Configuration.Sample;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();
}
