# LogRequestAndResponseMiddleware


## Description

This class is an AspNetCore middleware class to log the request and response data for an HTTP request.
[Middleware is a component that makes up the HTTP processing pipeline in AspNetCore.]


## Contents

### LogRequestAndResponseMiddleware.cs

A single file, containing a single class.
The class takes an injected logger object, and attempts to log using that object, so this will work with any logger implementing Microsoft.Extensions.Logging.ILogger type interfaces.


## Installation

Use in an AspNetCore project as per other Middleware.


## Usage

Amend the Configure() function in the Startup class to include, somewhere near the start of the pipeline...

         app.UseMiddleware<NZ01.LogRequestAndResponseMiddleware>();


## Contributing

Bug reports, suggestions all welcome, pull requests etc.  
Function bloat should be avoided.


## Credits

This work is drawn from several sources, and attempts to unify the approaches.
Additionally, this class prevents exceptions that occur for cached static files.

   - http://www.sulhome.com/blog/10/log-asp-net-core-request-and-response-using-middleware
   - https://stackoverflow.com/questions/37855384/how-to-log-the-http-response-body-in-asp-net-core-1-0
   - https://stackoverflow.com/questions/43403941/how-to-read-asp-net-core-response-body


## License

Attribution-ShareAlike 4.0 International
 - If this code is forked and adapted, please publish and share the improvements if it does not break IP restrictions.
 - Use is welcome in commercial code, but if you wish to pay back something, please donate to Second Chance Tasman Dogs Home, or better still, adopt a Rescue Dog and make the world a slightly less awful place.
   https://secondchanceadoptadog.com
