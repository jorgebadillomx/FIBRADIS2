namespace Application.News;

public sealed record FibraMatchInfo(Guid Id, string Ticker, IReadOnlyList<string> NameVariants);
