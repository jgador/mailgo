using System.Text.Json.Serialization;

namespace Mailgo.Api.Responses;

public class PagedResult<T>
{
    public PagedResult(
        IReadOnlyCollection<T> items,
        int page,
        int pageSize,
        int totalItems,
        int totalPages)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = totalPages;
    }

    [JsonPropertyName("items")]
    public IReadOnlyCollection<T> Items { get; }

    [JsonPropertyName("page")]
    public int Page { get; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; }

    [JsonPropertyName("totalItems")]
    public int TotalItems { get; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; }
}

