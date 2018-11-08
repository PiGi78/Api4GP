using System.Collections.Generic;

namespace Api4GP.Core
{

    /// <summary>
    /// Response of a web api call
    /// </summary>
    public class ApiResponse
    {

        /// <summary>
        /// Response headers
        /// </summary>
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Http status code
        /// </summary>
        public int StatusCode { get; set; }


        /// <summary>
        /// Content to send to client
        /// </summary>
        public string Content { get; set; }
    }
}
