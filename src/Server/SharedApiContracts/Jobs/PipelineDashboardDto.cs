namespace SharedApiContracts.Jobs;

public sealed record PipelineDashboardDto(
    IReadOnlyList<PipelineStatusDto> Pipelines,
    IReadOnlyList<PipelineErrorLogDto> RecentErrors
);
