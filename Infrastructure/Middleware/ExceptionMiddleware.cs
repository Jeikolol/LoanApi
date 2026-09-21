using Application.Exceptions;
using Application.Interfaces;
using Application.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
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
        private readonly bool _isDevelopment;

        public ExceptionMiddleware(IStringLocalizer<ExceptionMiddleware> t, ISerializerService jsonSerializer, IHostEnvironment environment)
        {
            _t = t;
            _jsonSerializer = jsonSerializer;
            _isDevelopment = environment.IsDevelopment();
        }

        private string Translate(string message) =>
            MessageTemplate.Translate(message, template =>
            {
                LocalizedString translated = _t[template];
                return translated.ResourceNotFound ? null : translated.Value;
            });

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
                    ErrorId = errorId,
                    SupportMessage = _t["Provide the ErrorId {0} to the support team for further analysis.", errorId]
                };
                string outerMessage = exception.Message;
                if (_isDevelopment)
                {
                    errorResult.Source = exception.TargetSite?.DeclaringType?.FullName;
                    errorResult.Exception = outerMessage.Trim();
                }
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
                        errorResult.Messages = (e.ErrorMessages ?? [e.Message]).Select(Translate).ToList();
                        break;

                    case KeyNotFoundException:
                        errorResult.StatusCode = (int)HttpStatusCode.NotFound;
                        errorResult.Messages = [_isDevelopment ? outerMessage : Translate("The requested resource was not found.")];
                        break;

                    default:
                        errorResult.StatusCode = (int)HttpStatusCode.InternalServerError;
                        errorResult.Messages = [_isDevelopment ? outerMessage : Translate("An unexpected error occurred.")];
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
