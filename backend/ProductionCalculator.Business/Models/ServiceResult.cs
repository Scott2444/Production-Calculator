namespace ProductionCalculator.Business.Models
{
    public enum ServiceStatus
    {
        Ok200 = 200,
        Created201 = 201,
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

        public static ServiceResult<T> SuccessResult(T data, ServiceStatus status = ServiceStatus.Ok200) => new ServiceResult<T> { Data = data, Status = status };
        public static ServiceResult<T> Fail(ServiceStatus status) => new ServiceResult<T> { Status = status };
    }
}
