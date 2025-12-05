namespace Mailgo.Api.Responses;

public record RecipientUploadResultResponse(int TotalRows, int Inserted, int SkippedInvalid);

