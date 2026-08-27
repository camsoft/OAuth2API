namespace WebAPICourse.Services
{
    public class ServiceResult<T>
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public T? Data { get; init; }

        public static ServiceResult<T> Ok(T data) =>
            new() { Success = true, Data = data };

        public static ServiceResult<T> Fail(string errorMessage) =>
            new() { Success = false, ErrorMessage = errorMessage };
    }
}
