namespace BookTracker.Api.Exceptions;

public class ApiException(int statusCode, string errorMessage, string errorCode)
    : Exception(errorMessage)
{
    public int StatusCode { get; } = statusCode;
    public string ErrorCode { get; } = errorCode;
}
