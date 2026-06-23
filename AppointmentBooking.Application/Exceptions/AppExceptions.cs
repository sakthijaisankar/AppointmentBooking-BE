namespace AppointmentBooking.Application.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
}

public class ValidationException : AppException
{
    public IEnumerable<string> Errors { get; }

    public ValidationException(string message, IEnumerable<string>? errors = null) : base(message)
    {
        Errors = errors ?? Array.Empty<string>();
    }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message) { }
}
