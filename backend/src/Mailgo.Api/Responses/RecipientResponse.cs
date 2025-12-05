namespace Mailgo.Api.Responses;

public record RecipientResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    DateTime CreatedAt);

