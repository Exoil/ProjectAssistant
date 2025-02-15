using System;
using ProjectAssistant.Features.Exceptions;

namespace ProjectAssistant.Features.Utilities;

public static class ExceptionUtility
{
    public static DomainException ToUnhandledDomainException(this Exception exception)
        => new DomainException(
            "Unhandled exception",
            "500",
            exception.Message);

    public static Exception ResolveExceptionToReturn(Exception exception) => exception switch
    {
        _ => exception.ToUnhandledDomainException()
    };
}
