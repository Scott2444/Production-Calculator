using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;


namespace ProductionCalculator.Business.Services
{
    public class ProductService : IProductService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IProductRepository _repo;
        private readonly IProjectRepository _projectRepo;
        public ProductService(ICurrentUserService currentUser, IProductRepository repo, IProjectRepository projectRepo) 
        { 
            _currentUser = currentUser; 
            _repo = repo;
            _projectRepo = projectRepo;
        }

        // Use _currentUser.UserId or _currentUser.Username as needed

        public async Task<ServiceResult<Product>> AddProduct(string projectPuid, string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<Product>.Fail(ServiceStatus.BadRequest400, "Product name is required.");

            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<Product>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if name already exists for this project
            var existingProducts = await _repo.GetProductsByProjectId(project.Project_Id);
            if (existingProducts.Any(p => p.Name == name)) return ServiceResult<Product>.Fail(ServiceStatus.Conflict409, "Product name already exists for this project.");

            // Limit string lengths
            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);

            // Generate new PUID
            var puid = await PuidHelper.GenerateUniquePuidAsync(_repo.PuidExists);

            var product = new Product
            {
                Product_Id = 0,
                Project_Id = project.Project_Id,
                Puid = puid,
                Name = name,
                Description = description ?? string.Empty,
                Created_At = DateTime.UtcNow,
                Last_Updated = DateTime.UtcNow
            };

            await _repo.AddProduct(product);
            await UpdateProjectLastUpdated(project);
            return ServiceResult<Product>.SuccessResult(product, ServiceStatus.Created201);
        }
        public async Task<ServiceResult<Product>> GetProductByPuid(string projectPuid, string puid)
        {
            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<Product>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Redirect aliased project to canonical project PUID
            if (!string.IsNullOrWhiteSpace(project.Alias_Project_Puid))
            {
                return ServiceResult<Product>.Redirection(ServiceStatus.SeeOther303, $"/api/projects/{project.Alias_Project_Puid}/products/{puid}");
            }

            // Check if product exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var product = await _repo.GetProductByPuid(puid);
            if (product == null || product.Project_Id != project.Project_Id) return ServiceResult<Product>.Fail(ServiceStatus.NotFound404, "Product not found.");

            return ServiceResult<Product>.SuccessResult(product);
        }
        public async Task<ServiceResult<List<Product>>> GetProductsByProjectPuid(string projectPuid)
        {   
            // Authorization already checked if project exists, otherwise they would not have access to it
            // i.e. this should never fail
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<List<Product>>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Redirect aliased project to canonical project PUID
            if (!string.IsNullOrWhiteSpace(project.Alias_Project_Puid))
            {
                return ServiceResult<List<Product>>.Redirection(ServiceStatus.SeeOther303, $"/api/projects/{project.Alias_Project_Puid}/products");
            }

            var products = await _repo.GetProductsByProjectId(project.Project_Id);

            return ServiceResult<List<Product>>.SuccessResult(products);
        }
        public async Task<ServiceResult<Product>> UpdateProduct(string projectPuid, string puid, string? name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name)) return ServiceResult<Product>.Fail(ServiceStatus.BadRequest400, "Product name is required.");

            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult<Product>.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if product exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var product = await _repo.GetProductByPuid(puid);
            if (product == null || product.Project_Id != project.Project_Id) return ServiceResult<Product>.Fail(ServiceStatus.NotFound404, "Product not found.");

            // Check if name already exists for this project
            var existingProducts = await _repo.GetProductsByProjectId(project.Project_Id);
            if (existingProducts.Any(p => p.Name == name && p.Product_Id != product.Product_Id)) return ServiceResult<Product>.Fail(ServiceStatus.Conflict409, "Product name already exists for this project.");

            // Limit string lengths
            name = TruncateHelper.TruncateString(name, 255);
            description = TruncateHelper.TruncateStringNullable(description, 1000);
            
            // Update fields if provided
            product.Name = name;
            product.Description = description;
            product.Last_Updated = DateTime.UtcNow;

            await _repo.UpdateProduct(product);
            await UpdateProjectLastUpdated(project);
            return ServiceResult<Product>.SuccessResult(product);
        }
        public async Task<ServiceResult> DeleteProduct(string projectPuid, string puid)
        {
            // Get projectId from projectPuid
            var project = await _projectRepo.GetProjectByPuid(projectPuid);
            if (project == null) return ServiceResult.Fail(ServiceStatus.NotFound404, "Project not found.");

            // Check if product exists and belongs to project (IMPORTANT FOR AUTHORIZATION!)
            var product = await _repo.GetProductByPuid(puid);
            if (product == null || product.Project_Id != project.Project_Id) return ServiceResult.Fail(ServiceStatus.NotFound404, "Product not found.");

            var isDeleted = await _repo.DeleteProduct(product.Product_Id);
            if (!isDeleted) return ServiceResult.Fail(ServiceStatus.InternalServerError500, "Failed to delete product.");

            await UpdateProjectLastUpdated(project);
            return ServiceResult.SuccessResult(ServiceStatus.NoContent204);
        }

        private async Task UpdateProjectLastUpdated(Project project)
        {
            project.Last_Updated = DateTime.UtcNow;
            await _projectRepo.UpdateProject(project);
        }
    }
}