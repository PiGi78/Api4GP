using Api4GP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api4GP.Tests
{
    public class TestApiManager : IApiManager
    {
        public Task<ApiResponse> DoWorkAsync(ApiRequest request)
        {
            var response = new ApiResponse
            {
                StatusCode = 200,
                Content = $"Method: {request.ActualRequest.Method} - Path: {string.Join('/', request.Paths)} - Content: {request.GetContentAsync().GetAwaiter().GetResult()}"
            };
            return Task.FromResult(response);
        }

        public bool IsManaged(ApiRequest request)
        {
            var path = request.Paths[0];
            if (!string.IsNullOrEmpty(path) &&
                path.Equals("test", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            return false;
        }
    }
}
