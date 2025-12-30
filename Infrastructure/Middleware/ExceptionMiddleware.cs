using Application.Exceptions;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Serilog;
using Serilog.Context;
using System.Net;

namespace Infrastructure.Middleware
{
    internal class ExceptionMiddleware : IMiddleware
    {
        private readonly IStringLocalizer<ExceptionMiddleware> _t;
        private readonly ISerializerService _jsonSerializer;

        public ExceptionMiddleware(IStringLocalizer<ExceptionMiddleware> t, ISerializerService jsonSerializer)
        {
            _t = t;
            _jsonSerializer = jsonSerializer;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                string errorId = Guid.NewGuid().ToString();
                LogContext.PushProperty("ErrorId", errorId);
                LogContext.PushProperty("StackTrace", exception.StackTrace);
                var errorResult = new ErrorResult
                {
                    Source = exception.TargetSite?.DeclaringType?.FullName,
                    Exception = exception.Message.Trim(),
                    ErrorId = errorId,
                    SupportMessage = _t["Provide the ErrorId {0} to the support team for further analysis.", errorId]
                };
                errorResult.Messages.Add(exception.Message);
                if (exception is not CustomException && exception.InnerException != null)
                {
                    while (exception.InnerException != null)
                    {
                        exception = exception.InnerException;
                    }
                }

                switch (exception)
                {
                    case CustomException e:
                        errorResult.StatusCode = (int)e.StatusCode;
                        if (e.ErrorMessages is not null)
                        {
                            errorResult.Messages = e.ErrorMessages;
                        }

                        break;

                    case KeyNotFoundException:
                        errorResult.StatusCode = (int)HttpStatusCode.NotFound;
                        break;

                    default:
                        errorResult.StatusCode = (int)HttpStatusCode.InternalServerError;
                        break;
                }

                Log.Error($"{errorResult.Exception} Request failed with Status Code {context.Response.StatusCode} and Error Id {errorId}.");
                var response = context.Response;
                if (!response.HasStarted)
                {
                    response.ContentType = "application/json";
                    response.StatusCode = errorResult.StatusCode;
                    await response.WriteAsync(_jsonSerializer.Serialize(errorResult));
                }
                else
                {
                    Log.Warning("Can't write error response. Response has already started.");
                }
            }
        }
    }
}
