using System.Threading.Tasks;

namespace Api4GP.Core
{

    /// <summary>
    /// Api manager
    /// </summary>
    public interface IApiManager
    {


        /// <summary>
        /// True if the manager works with the given request
        /// </summary>
        /// <param name="request">Request to check</param>
        /// <returns>True if the given request is managed by this manager, elsewhere false </returns>
        bool IsManaged(ApiRequest request);


        /// <summary>
        /// Does the work
        /// </summary>
        /// <param name="request">Request to process</param>
        /// <returns>Response</returns>
        Task<ApiResponse> DoWorkAsync(ApiRequest request);
    }
}
