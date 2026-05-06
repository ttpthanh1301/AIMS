using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AIMS.BackendServer.Data.Converters;

/// <summary>
/// Converter for DateTime values to ensure PostgreSQL compatibility.
/// PostgreSQL requires all timestamp with time zone values to have Kind=UTC.
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        v => v.Kind == DateTimeKind.Unspecified
            ? new DateTime(v.Ticks, DateTimeKind.Utc)
            : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

/// <summary>
/// Converter for nullable DateTime values to ensure PostgreSQL compatibility.
/// </summary>
public class UtcNullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public UtcNullableDateTimeConverter() : base(
        v => v.HasValue
            ? (v.Value.Kind == DateTimeKind.Unspecified
                ? new DateTime(v.Value.Ticks, DateTimeKind.Utc)
                : v.Value.ToUniversalTime())
            : null,
        v => v.HasValue
            ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
            : null)
    {
    }
}
