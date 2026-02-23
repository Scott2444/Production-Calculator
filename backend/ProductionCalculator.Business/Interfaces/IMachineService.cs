using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IMachineService
    {
        Task<ServiceResult<MachineResponse>> AddMachine(string projectPuid, string name, string? description, double baseSpeed, List<string> recipePuids, List<AttributeRateExchange>? attributes = null);
        Task<ServiceResult<MachineResponse>> UpdateMachine(string projectPuid, string puid, string? name, string? description, double baseSpeed, List<string> recipePuids, List<AttributeRateExchange>? attributes = null);
        Task<ServiceResult<MachineResponse>> GetMachineByPuid(string projectPuid, string puid);
        Task<ServiceResult<List<MachineResponse>>> GetMachinesByProjectPuid(string projectPuid);
        Task<ServiceResult> DeleteMachine(string projectPuid, string puid);
    }
}