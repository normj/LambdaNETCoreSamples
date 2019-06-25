# Example ASP.NET Core Web API 3.0 using Amazon.Lambda.Runtime

This sample shows how to configure an ASP.NET Core 3.0 project to use the [Amazon.Lambda.RuntimeSupport](https://aws.amazon.com/blogs/developer/announcing-amazon-lambda-runtimesupport/) NuGet package and deploy to Lambda using Lambda's custom runtime feature.

**Note: This sample was writen with .NET Core 3.0 preview 6**

### Preview Version of Amazon.Lambda.AspNetCoreServer

[Amazon.Lambda.AspNetCoreServer](https://github.com/aws/aws-lambda-dotnet/tree/master/Libraries/src/Amazon.Lambda.AspNetCoreServer)
is the NuGet package that has allowed ASP.NET Core application to run as Lambda function since the release of .NET Core Lambda support. 

ASP.NET Core 3.0 has breaking changes that require a new version of **Amazon.Lambda.AspNetCoreServer**. Since .NET Core 3.0 is in preview
the changes to Amazon.Lambda.AspNetCoreServer have not shipped. A preview version has been checked in to the **nuget-cache** directory 
of this project. A **nuget.config** file was also added to add the nuget-cache directory as a NuGet feed.


### LambdaEntryPoint.cs

If the project doesn't already contain a LambdaEntryPoint class then you need to add it. The is same file as that is created by the existing ASP.NET Core Lambda templates.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace CustomRuntimeAspNetCore30
{
    /// <summary>
    /// This class extends from APIGatewayProxyFunction which contains the method FunctionHandlerAsync which is the 
    /// actual Lambda function entry point. 
    /// </summary>
    public class LambdaEntryPoint :
        // When using an ELB's Application Load Balancer as the event source change 
        // the base class to Amazon.Lambda.AspNetCoreServer.ApplicationLoadBalancerFunction
        Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
    {
        /// <summary>
        /// The builder has configuration, logging and Amazon API Gateway already configured. The startup class
        /// needs to be configured in this method using the UseStartup<>() method.
        /// </summary>
        /// <param name="builder"></param>
        protected override void Init(IWebHostBuilder builder)
        {
            builder
                .UseStartup<Startup>();
        }
    }
}
```

### Add bootstrap script

Lambda needs an executable file called bootstrap when using the custom runtime feature. This can be done by adding a text file called `bootstrap` to the project. Be sure to set your project name in the bootstrap file.

```bash
#!/bin/sh
# This is the script that the Lambda host calls to start the custom runtime.

/var/task/<project-name>
```

### Project file

Updates to the project file:
* Make sure the `OutputType` set to **Exe**
* Add the `bootstrap` file to the project as a content file to be copy to the output directory.
* Add `PackageReference` for `Amazon.Lambda.RuntimeSupport`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>netcoreapp3.0</TargetFramework>
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
    <AWSProjectType>Lambda</AWSProjectType>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="bootstrap">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
  
  <ItemGroup>
    <PackageReference Include="Amazon.Lambda.AspNetCoreServer" Version="3.1.9999-preview1" />
    <PackageReference Include="Amazon.Lambda.RuntimeSupport" Version="1.0.0" />
  </ItemGroup>

</Project>
```

### Update the Main method

A Lambda function that uses **Amazon.Lambda.RuntimeSupport** is required to have a Main method to bootstrap the Lambda runtime. An ASP.NET Core project likely already has a Main method to run locally. 

To work in both modes have the Main method check to see a Lambda specific environment variable is set. If it is not set then use regular ASP.NET Core startup code. If the environment variable is set then use the Lambda bootstrap code.

```csharp
using System;`
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

namespace CustomRuntimeAspNetCore30
{
    /// <summary>
    /// The Main function can be used to run the ASP.NET Core application locally using the Kestrel webserver.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")))
            {
                CreateHostBuilder(args).Build().Run();
            }
            else
            {
                var lambdaEntry = new LambdaEntryPoint();
                var functionHandler = (Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>>)(lambdaEntry.FunctionHandlerAsync);
                using (var handlerWrapper = HandlerWrapper.GetHandlerWrapper(functionHandler, new JsonSerializer()))
                using (var bootstrap = new LambdaBootstrap(handlerWrapper))
                {
                    bootstrap.RunAsync().Wait();
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
```

### Updating the serverless.template

In the serverless.template file set the `Runtime` property to `provided` to inform the Lambda service that the function should use custom runtimes.


```json
"AspNetCoreFunction" : {
	"Type" : "AWS::Serverless::Function",
	"Properties": {
		"Handler": "not-required",
		"Runtime": "provided",
		"CodeUri": "",
		"MemorySize": 256,
		"Timeout": 30,
		"Policies": [ "AWSLambdaFullAccess" ],
		"Events": {
			"ProxyResource": {
				"Type": "Api",
				"Properties": {
					"Path": "/{proxy+}",
					"Method": "ANY"
				}
			},
			"RootResource": {
				"Type": "Api",
				"Properties": {
					"Path": "/",
					"Method": "ANY"
				}
			}
		}
	}
}
```

### Configure to package as self contained

To deploy a .NET Core 3.0 Lambda function using Amazon.Lambda.RuntimeSupport the function must be deployed as self contained package.

This can be configured in the `aws-lambda-tools-defaults.json` by setting the `msbuild-parameters` property to `--self-contained true`.

```json
{	
    "profile":"",
    "region" : "us-west-2",
    "configuration" : "Release",
    "s3-prefix"     : "CustomRuntimeAspNetCore30/",
    "template"      : "serverless.template",
    "template-parameters" : "",
    "msbuild-parameters": "--self-contained true"
}
```