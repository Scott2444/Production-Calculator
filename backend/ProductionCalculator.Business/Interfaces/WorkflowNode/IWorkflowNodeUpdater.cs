using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Records;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IWorkflowNodeUpdater
    {
        NodeUpdateImpact ApplyPutUpdate(FullNode fullNode, WorkflowNodeRequest request, ProjectObjects projectObjects);
    }
}