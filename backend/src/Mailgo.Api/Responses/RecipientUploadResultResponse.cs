using System.Text.Json.Serialization;

namespace Mailgo.Api.Responses;

public class RecipientUploadResultResponse
{
    public RecipientUploadResultResponse(int totalRows, int inserted, int skippedInvalid)
    {
        TotalRows = totalRows;
        Inserted = inserted;
        SkippedInvalid = skippedInvalid;
    }

    [JsonPropertyName("totalRows")]
    public int TotalRows { get; }

    [JsonPropertyName("inserted")]
    public int Inserted { get; }

    [JsonPropertyName("skippedInvalid")]
    public int SkippedInvalid { get; }
}

