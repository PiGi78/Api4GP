using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Api4GP.Core
{

    /// <summary>
    /// Request of a web api
    /// </summary>
    public class ApiRequest
    {

        /// <summary>
        /// Creates a new instance of <see cref="ApiRequest"/> for the given <see cref="HttpRequest"/>
        /// </summary>
        /// <param name="request">Actual <see cref="HttpRequest"/></param>
        public ApiRequest(HttpRequest request)
        {
            ActualRequest = request ?? throw new ArgumentNullException(nameof(request));
        }

        /// <summary>
        /// Actual request
        /// </summary>
        public HttpRequest ActualRequest { get; }



        #region Path

        private string[] _paths = null;

        /// <summary>
        /// Path array (the first element is the managed one)
        /// </summary>
        public string[] Paths
        {
            get
            {
                if (_paths == null)
                {
                    _paths = new string[0];
                    if (ActualRequest.Path.HasValue)
                    {
                        _paths = ActualRequest.Path.Value.TrimStart('/').Split('/');
                    }
                }
                return _paths;
            }
        }


        #endregion



        private string _content = null;

        /// <summary>
        /// Request content
        /// </summary>
        public async Task<string> GetContentAsync()
        {
            // If already extracted, return it
            if (_content != null) return _content;

            // Enables rewind (else other method can't read the content anymore)
            ActualRequest.EnableRewind();
            
            // Read content
            _content = string.Empty;
            int bufferSize = ActualRequest.ContentLength.HasValue ? (int)ActualRequest.ContentLength.Value : 1024;
            using (var reader = new StreamReader(ActualRequest.Body, Encoding.Default, false, bufferSize, leaveOpen: true))
            {
                _content = await reader.ReadToEndAsync();
            }

            // Set the position to the beginning (for other that can read content)
            ActualRequest.Body.Position = 0;

            // Return the content
            return _content;
        }


    }
}
