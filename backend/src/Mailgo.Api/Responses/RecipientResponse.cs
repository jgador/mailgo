using System.Text.Json.Serialization;

namespace Mailgo.Api.Responses;

public class RecipientResponse
{
    public RecipientResponse(
        Guid id,
        string email,
        string? firstName,
        string? lastName,
        DateTime createdAt)
    {
        Id = id;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        CreatedAt = createdAt;
    }

    [JsonPropertyName("id")]
    public Guid Id { get; }

    [JsonPropertyName("email")]
    public string Email { get; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; }
}

