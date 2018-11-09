using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Api4GP.Core
{


    /// <summary>
    /// Api middleware
    /// </summary>
    public class ApiMiddleware
    {


        /// <summary>
        /// Creates a new instance of <see cref="ApiMiddleware"/>
        /// </summary>
        /// <param name="apiManager">Api manager to use</param>
        /// <param name="next"><see cref="RequestDelegate"/> that follows the middleware in the pipeline</param>
        public ApiMiddleware(RequestDelegate next, IApiManager apiManager)
        {
            Next = next ?? throw new ArgumentNullException(nameof(next));
            ApiManager = apiManager ?? throw new ArgumentNullException(nameof(apiManager));
        }

        /// <summary>
        /// Request delegate that follow this on the pipeline
        /// </summary>
        protected RequestDelegate Next { get; }


        /// <summary>
        /// Api manager
        /// </summary>
        private IApiManager ApiManager { get; }


        /// <summary>
        /// Invoke the current middleware
        /// </summary>
        /// <param name="context">Context that invokes the middleware</param>
        /// <returns>Results</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Wrap request into ApiRequest
            var request = new ApiRequest(context.Request);

            // Check if a managed method
            if (ApiManager.IsManaged(request))
            {
                // Execute the manager
                var result = await ApiManager.ExecuteRequestAsync(request);
                // If there is a result, it will back to the caller
                if (result != null)
                {
                    context.Response.StatusCode = result.StatusCode;
                    if (!string.IsNullOrEmpty(result.Content))
                    {
                        await context.Response.WriteAsync(result.Content);
                    }
                    return;
                }
            }
            await Next(context);
        }



    }
}
