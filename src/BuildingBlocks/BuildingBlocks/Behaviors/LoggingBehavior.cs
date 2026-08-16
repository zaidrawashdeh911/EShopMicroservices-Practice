using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BuildingBlocks.Behaviors;

// This logging behavior class works for both commands and queries, as it implements the IPipelineBehavior interface from MediatR.
public class LoggingBehavior<TRequest, TResponse>
    (ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest: notnull, IRequest<TResponse>
    where TResponse : notnull
{
    // This method is called when a request is being handled by the MediatR pipeline.
    // It logs the start and end of the request handling, as well as the time taken to handle the request.
    // If the time taken is greater than 3 seconds, it logs a warning.
    // The next() delegate is called to continue the pipeline and get the response.
    // The cancellationToken is used to cancel the request if needed.
    // The method returns the response from the next() delegate.
    // The method is asynchronous and returns a Task<TResponse>.
    // The method uses a Stopwatch to measure the time taken to handle the request.
    // The method uses the measured time taken to handle the request for performance logging.
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("[START] Handle request={Request} - Response={Response} - RequestData={RequestData}",
            typeof(TRequest).Name, typeof(TResponse).Name, request);

        var timer = new Stopwatch();
        timer.Start();

        var response = await next();

        timer.Stop();

        var timeTaken = timer.Elapsed;
        if (timeTaken.Seconds > 3) // if the request is greater than 3 seconds, then log the warnings
            logger.LogWarning("[PERFORMANCE] The request {Request} took {TimeTaken} seconds.",
                typeof(TRequest).Name, timeTaken.Seconds);

        logger.LogInformation("[END] Handled {Request} with {Response}", typeof(TRequest).Name, typeof(TResponse).Name);
        return response;
    }
}
