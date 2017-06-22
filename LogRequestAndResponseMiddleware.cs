using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.AspNetCore.Http.Extensions;

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
                    _logger.Log(LogLevel.Information, 1, $"REQUEST METHOD:[{context.Request.Method}] REQUEST BODY:[{requestBodyText}] REQUEST URL:[{url}]", null, _defaultFormatter);

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

                    string responseBody = await streamReaderResponse.ReadToEndAsync();

                    string displayResponseBody = string.IsNullOrEmpty(responseBody) ? "[null]" : "\r\n" + responseBody; // If no body, mark as null, else, carriage return and log response.
                    _logger.Log(LogLevel.Information, 1, $"RESPONSE:{displayResponseBody}", null, _defaultFormatter);

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
