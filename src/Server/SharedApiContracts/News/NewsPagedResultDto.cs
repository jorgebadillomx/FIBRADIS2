namespace SharedApiContracts.News;

public sealed record NewsPagedResultDto(
    IReadOnlyList<NewsArticleDto> Items,
    int Total,
    int Page,
    int PageSize
);
