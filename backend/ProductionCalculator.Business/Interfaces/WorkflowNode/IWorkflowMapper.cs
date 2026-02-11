using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.APIModels;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowMapper
    {
        WorkflowChartResponse ToResponse(ProjectObjects projectObjects, NodeChart chart);
    }
}