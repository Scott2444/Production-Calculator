namespace ProductionCalculator.Business.APIModels
{
    public class PublicProjectSearchPageResponse
    {
        public required List<ProjectResponse> Projects { get; set; }
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public required int TotalCount { get; set; }
        public required int TotalPages { get; set; }
    }
}
