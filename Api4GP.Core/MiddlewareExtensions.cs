using Microsoft.AspNetCore.Builder;
using System;

namespace Api4GP.Core
{
    public static class MiddlewareExtensions
    {

        #region AddApi4GP


        /// <summary>
        /// Enables Api4GP middleware
        /// </summary>
        /// <param name="app"><see cref="IApplicationBuilder"/> where to append the Api4GP middleware</param>
        /// <param name="apiManager">Manager to use</param>
        /// <returns><see cref="IApplicationBuilder"/> with Api4GP</returns>
        public static IApplicationBuilder UseApi4GP(this IApplicationBuilder app, IApiManager apiManager)
        {
            // Check params
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (apiManager == null) throw new ArgumentNullException(nameof(apiManager));

            // Register the middleware
            return app.UseMiddleware<ApiMiddleware>(apiManager);
        }


        #endregion
    }
}
