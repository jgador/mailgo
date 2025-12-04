namespace EmailMarketing.Api.Dtos;

public record RecipientDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    DateTime CreatedAt);
