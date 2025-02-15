using System;

namespace ProjectAssistant.Features.Exceptions;

public class BaseException : Exception
{
    public readonly string Title;
    public readonly string ErrorCode;
    public int StatusCode { get; private set; } = 500;

    public BaseException(string title, string errorCode,  string message)
        : base(message, null)
    {
        Title = title;
        ErrorCode = errorCode;
    }

    public BaseException(string title, string errorCode, string message, int statusCode)
        : base(message,  null)
    {
        Title = title;
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}