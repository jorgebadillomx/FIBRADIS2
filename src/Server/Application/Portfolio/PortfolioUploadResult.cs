namespace Application.Portfolio;

public record PortfolioUploadResult(
    bool Success,
    IReadOnlyList<PositionDto> Positions,
    IReadOnlyList<RowError> Errors);

public record PositionDto(Guid FibraId, string Ticker, int Titulos, decimal CostoPromedio, decimal CostoTotalCompra);

public record RowError(int RowNumber, string Ticker, string Message);
