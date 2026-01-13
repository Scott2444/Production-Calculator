namespace ProductionCalculator.Business.Models
{
    public enum ServiceStatus
    {
        Ok200 = 200,
        Created201 = 201,
        NoContent204 = 204,
        SeeOther303 = 303,
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
        public bool Redirect => (int)Status >= 300 && (int)Status < 400;
        public string? ErrorMessage { get; set; }
        public string? Location { get; set; }

        public static ServiceResult<T> SuccessResult(T data, ServiceStatus status = ServiceStatus.Ok200) => new ServiceResult<T> { Data = data, Status = status };
        public static ServiceResult<T> Fail(ServiceStatus status, string? errorMessage = null) => new ServiceResult<T> { Status = status, ErrorMessage = errorMessage };
        public static ServiceResult<T> Redirection(ServiceStatus status, string? location) => new ServiceResult<T> { Status = status, Location = location };
    }
    public class ServiceResult
    {
        public ServiceStatus Status { get; set; }
        public bool Success => (int)Status >= 200 && (int)Status < 300;
        public bool Redirect => (int)Status >= 300 && (int)Status < 400;
        public string? ErrorMessage { get; set; }
        public string? Location { get; set; }

        public static ServiceResult SuccessResult(ServiceStatus status = ServiceStatus.Ok200) => new ServiceResult { Status = status };
        public static ServiceResult Fail(ServiceStatus status, string? errorMessage = null) => new ServiceResult { Status = status, ErrorMessage = errorMessage };
        public static ServiceResult Redirection(ServiceStatus status, string? location) => new ServiceResult { Status = status, Location = location };
    }
}
