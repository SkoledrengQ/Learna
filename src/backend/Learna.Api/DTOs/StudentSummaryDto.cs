namespace Learna.Api.DTOs;

// Lightweight student projection used inside class-membership and enrollment
// rosters, to avoid ambiguity between the Student's numeric Id and its human
// readable StudentId code.
public record StudentSummaryDto(
    int Id,
    PersonNameDto Name,
    string StudentId,
    string Email
);
