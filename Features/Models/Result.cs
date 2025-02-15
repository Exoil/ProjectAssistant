using System;

namespace ProjectAssistant.Features.Models;

public readonly struct Result<TValue, TError>
    where TError : Exception
{
    private readonly TValue? _value;

    public TValue Value
    {
        get
        {
            if (_isError)
                throw Error!;

            return _value!;
        }
    }
    public readonly TError? Error;
    private readonly bool _isError;
    public bool IsSuccess => !_isError;

    private Result(TValue value)
    {
        _isError = false;
        _value = value;
        Error = default;
    }

    private Result(TError? error)
    {
        _isError = true;
        _value = default;
        Error = error;
    }

    /// <summary>
    /// Implicit conversion from TValue type to Result TValue, TError. It enables you to
    /// create a Result without having to explicitly instantiate a new Result.
    /// </summary>
    public static implicit operator Result<TValue, TError>(TValue value)
        => new(value);

    /// <summary>
    /// Implicit conversion from TError type to Result TValue, TError. It enables you to
    /// create a Result without having to explicitly instantiate a new Result.
    /// </summary>
    public static implicit operator Result<TValue, TError>(TError error)
        => new(error);
} 
