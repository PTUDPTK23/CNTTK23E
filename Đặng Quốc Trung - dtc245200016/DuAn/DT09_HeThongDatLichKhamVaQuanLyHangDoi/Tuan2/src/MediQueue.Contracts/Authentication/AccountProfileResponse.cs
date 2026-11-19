namespace MediQueue.Contracts.Authentication;

public sealed record AccountProfileResponse(
    Guid UserId,
    string? FullName,
    string Email,
    string? PhoneNumber,
    string Role,
    Guid? PatientId,
    Guid? DoctorId,
    DateOnly? DateOfBirth);
