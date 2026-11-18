using System.Security.Cryptography;
using System.Text;

namespace Clinic.Core;

public record Doctor(int Id, string Name, string Specialty, string Room);
public record Slot(Guid Id, int DoctorId, DateOnly Date, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Capacity, int Available);
public record BookRequest(Guid SlotId, string PatientName, string Phone);
public record Appointment(Guid Id, int DoctorId, Guid SlotId, DateOnly Date, string PatientName, string Status, int? Number, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
public record Ticket(int Number, string Status, DateTimeOffset StartsAt);
public record QueueView(int DoctorId, DateOnly Date, int? CurrentNumber, IReadOnlyList<Ticket> Waiting);
public record BusMessage(Guid Id, Guid AppointmentId, int DoctorId, DateOnly Date);
public sealed class RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
public static class Secrets
{
    public static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public static bool Equal(string left, string right) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}
public static class ClinicTime
{
    public static TimeZoneInfo Zone { get; } = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone).DateTime);
    public static DateTimeOffset At(DateOnly date, TimeOnly time) => new(date.ToDateTime(time), TimeSpan.FromHours(7));
}
