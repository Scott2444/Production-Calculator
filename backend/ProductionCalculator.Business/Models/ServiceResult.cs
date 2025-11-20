namespace ProductionCalculator.Business.Models
{
    public enum ServiceStatus
    {
        Ok200 = 200,
        Created201 = 201,
        NoContent204 = 204,
        BadRequest400 = 400,
        Unauthorized401 = 401,
        NotFound404 = 404,
        Conflict409 = 409,
        InternalServerError500 = 500
    }

    public class ServiceResult<T>
    {
        public ServiceStatus Status { get; set; }
        public T? Data { get; set; }
        public bool Success => (int)Status >= 200 && (int)Status < 300;
        public string? ErrorMessage { get; set; }

        public static ServiceResult<T> SuccessResult(T data, ServiceStatus status = ServiceStatus.Ok200) => new ServiceResult<T> { Data = data, Status = status };
        public static ServiceResult<T> Fail(ServiceStatus status, string? errorMessage = null) => new ServiceResult<T> { Status = status, ErrorMessage = errorMessage };
    }
    public class ServiceResult
    {
        public ServiceStatus Status { get; set; }
        public bool Success => (int)Status >= 200 && (int)Status < 300;
        public string? ErrorMessage { get; set; }

        public static ServiceResult SuccessResult(ServiceStatus status = ServiceStatus.Ok200) => new ServiceResult { Status = status };
        public static ServiceResult Fail(ServiceStatus status, string? errorMessage = null) => new ServiceResult { Status = status, ErrorMessage = errorMessage };
    }
}
