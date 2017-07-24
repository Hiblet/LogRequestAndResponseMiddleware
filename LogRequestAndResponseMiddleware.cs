using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.AspNetCore.Http.Extensions;
using System.Collections.Generic;


// This work brings together current approaches to logging request and response data,
// into a single class.  
// Additionally I have used using statements to dispose of MemoryStream and StreamReaders to
// prevent memory leaks.
// A single class and function is used because request and response can be sampled either side
// of the async call to "await next".  Any bugs, please feedback, with thanks in advance.
//
// Ref + Credit: 
//   http://www.sulhome.com/blog/10/log-asp-net-core-request-and-response-using-middleware
//   https://stackoverflow.com/questions/37855384/how-to-log-the-http-response-body-in-asp-net-core-1-0
//   https://stackoverflow.com/questions/43403941/how-to-read-asp-net-core-response-body
//
// Usage: 
//   Set up a logger of choice, eg Console, Debug, 3rd Party etc.
//   Amend Startup.Configure() to include, early in the middleware pipeline...
//     app.UseMiddleware<NZ01.LogRequestAndResponseMiddleware>();


namespace NZ01
{
    public class LogRequestAndResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LogRequestAndResponseMiddleware> _logger;
        private Func<string, Exception, string> _defaultFormatter = (state, exception) => state;

        public LogRequestAndResponseMiddleware(RequestDelegate next, ILogger<LogRequestAndResponseMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Iterate an HTTP message header dictionary and dump to string, assuming the headers have been wrapped for Kestrel.
        /// </summary>
        /// <param name="headers">IHeaderDictionary; Either the HttpContext.Request.Headers object, or the HttpContext.Response.Headers object</param>
        /// <param name="delim">string; Delimiter for resultant list</param>
        /// <returns>
        /// string; List of header key value pairs, in "Key:Value, Key:Value" format
        /// </returns>
        private string processKestrelHeaders(IHeaderDictionary headers, string delim = ", ")
        {
            List<string> listHeaders = new List<string>();

            foreach (var header in headers)
                listHeaders.Add(header.Key.Replace("Header", "") + ":" + header.Value);

            return string.Join(delim, listHeaders);
        }

        public async Task Invoke(HttpContext context)
        {           
            Stream originalResponseBody = context.Response.Body;
            Stream originalRequestBody = context.Request.Body;

            try
            {
                using (var memStreamRequest = new MemoryStream())
                using (var memStreamResponse = new MemoryStream())
                using (var streamReaderRequest = new StreamReader(memStreamRequest))
                using (var streamReaderResponse = new StreamReader(memStreamResponse))
                {
                    ///////////////////////////////////////////////////////////
                    // Request
                    //

                    await context.Request.Body.CopyToAsync(memStreamRequest);
                    memStreamRequest.Seek(0, SeekOrigin.Begin);

                    var url = UriHelper.GetDisplayUrl(context.Request);
                    
                    var requestBodyText = await streamReaderRequest.ReadToEndAsync();
                    string kestrelRequestHeaders = processKestrelHeaders(context.Request.Headers);
                    _logger.Log(LogLevel.Information, 1, 
                        $"REQUEST: METHOD:[{context.Request.Method}]; URL:[{url}]; HEADERS:[{kestrelRequestHeaders}]; BODY:[{requestBodyText}]; ", null, _defaultFormatter);

                    memStreamRequest.Seek(0, SeekOrigin.Begin);
                    context.Request.Body = memStreamRequest;

                    //
                    ///////////////////////////////////////////////////////////

                    ///////////////////////////////////////////////////////////
                    // Response
                    //

                    context.Response.Body = memStreamResponse;

                    //
                    ///////////////////////////////////////////////////////////

                    await _next(context);

                    ///////////////////////////////////////////////////////////
                    // Request
                    //

                    context.Request.Body = originalRequestBody;

                    //
                    ///////////////////////////////////////////////////////////

                    ///////////////////////////////////////////////////////////
                    // Response
                    //

                    memStreamResponse.Position = 0;

                    string kestrelResponseHeaders = processKestrelHeaders(context.Response.Headers);
                    string responseBody = await streamReaderResponse.ReadToEndAsync();
                    string displayResponseBody = string.IsNullOrEmpty(responseBody) ? "NULL" : "\r\n" + responseBody; // If no body, mark as null, else, carriage return and log response.

                    _logger.Log(LogLevel.Information, 1, $"RESPONSE: HEADERS:[{kestrelResponseHeaders}]; BODY:{displayResponseBody}", null, _defaultFormatter);

                    memStreamResponse.Position = 0;

		            // Checking for size prevents exceptions on cached static file requests...
                    if (!string.IsNullOrEmpty(responseBody))
                        await memStreamResponse.CopyToAsync(originalResponseBody);

                    //
                    ///////////////////////////////////////////////////////////
                }
            }
            finally
            {
                context.Response.Body = originalResponseBody;
            }
        }
       
    }
}
