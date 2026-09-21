using System.Net;

namespace Application.Exceptions;

public class ValidationException : CustomException
{
    public ValidationException(string message, List<string>? errors = null)
        : base(message, errors, HttpStatusCode.BadRequest)
    {
    }
}
