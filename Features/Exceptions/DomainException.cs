using System;

namespace ProjectAssistant.Features.Exceptions;

public class DomainException : BaseException
{
    public DomainException(string errorCode, string message, string message1)
        : base("Domain exception", errorCode, message)
    {
    }
}
