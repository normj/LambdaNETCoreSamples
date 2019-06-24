using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.Json;

namespace CustomRuntimeAspNetCore22
{
    /// <summary>
    /// The Main function can be used to run the ASP.NET Core application locally using the Kestrel webserver.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")))
            {
                BuildWebHost(args).Run();
            }
            else
            {
                var lambdaEntry = new LambdaEntryPoint();
                var functionHandler = (Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>>)(lambdaEntry.FunctionHandlerAsync);
                using(var handlerWrapper = HandlerWrapper.GetHandlerWrapper(functionHandler, new JsonSerializer()))
                using(var bootstrap = new LambdaBootstrap(handlerWrapper))
                {
                    bootstrap.RunAsync().Wait();
                }
            }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
