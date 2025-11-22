using FluentValidation;
using MediatR;
using Domain.Responses;
using System.Net;

namespace Infrastructure.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Count != 0)
        {
            var errors = failures.Select(f => f.ErrorMessage).ToList();
            var errorMessage = string.Join(", ", errors);
            var responseType = typeof(TResponse);
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Response<>))
            {
                var constructor = responseType.GetConstructor(new[] { typeof(HttpStatusCode), typeof(string) });
                if (constructor != null)
                {
                    return (TResponse)constructor.Invoke(new object[] { HttpStatusCode.BadRequest, errorMessage });
                }
            }
            else if (responseType == typeof(Response<string>)) 
            {
                 return (TResponse)(object)new Response<string>(HttpStatusCode.BadRequest, errorMessage);
            }
            
            throw new ValidationException(failures);
        }

        return await next();
    }
}
