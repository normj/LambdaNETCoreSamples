# Sample .NET 6 Custom Runtime Lambda Function

This project is configured to be deployed to AWS Lambda as a custom runtime. It uses the NuGet package 
[Amazon.Lambda.RuntimeSupport](https://github.com/aws/aws-lambda-dotnet/tree/master/Libraries/src/Amazon.Lambda.RuntimeSupport)
as the .NET Lambda runtime.

In my observations if you turn on the trimming and ready to run features as describe below the cold start performance
for a .NET 6 custom runtime is on par with a .NET Core 3.1 managed runtime. Would love to hear other people's findings.

## Deploying to Lambda
The easiest way to deploy a .NET 6 Custom Runtime Lambda function is to use the [Amazon.Lambda.Tools](https://www.nuget.org/packages/Amazon.Lambda.Tools/) .NET CLI tool. 
To install the tool use the following command:
```
    dotnet tool install -g Amazon.Lambda.Tools
```

Once installed to run the following command to start the deployment process:
```
    dotnet lambda deploy-function
```

## Details of the project

The section below describe important things to know about what is going on in the code and project file for this Lambda project.

### Publish as self contained

In order to deploy a .NET Lambda function as a custom runtime you need to include the .NET runtime in the publish bundle. This project has the
**aws-lambda-tools-defaults.json** file set the `msbuild-parameters` property with the value `--self-contained true` which tells the underlying
dotnet publish command to included the .NET Runtime.

### Trimming the size

Since this is a self contained publish including the .NET runtime the deployment bundle is larger then a deployment bundle for a 
managed runtime like .NET Core 3.1. By default the hello world Lambda function is about 33 Megs. .NET 6 has new tricks up 
its sleeve for reducing the size with its [Trimming features](https://docs.microsoft.com/en-us/dotnet/core/deploying/trimming-options).

In the csproj file trimming has been enabled by setting the `PublishTrimmed` property to true. The default 
trimming mode in .NET 6 is `link` mode which actually trims out unused code from assemblies. This can reduce the 
hello world Lambda function from 33 megs to 11 megs. In your particular Lambda function you might find this 
setting is too aggressive and in that case you should set `TrimMode` to `copyused` which trims at an 
assembly level or turn off trimming.

### Turn on Ready to Run (R2R)

R2R is the process of converting assemblies from the agnostic IL to machine specific instruction code. This conversion process would
normally happen at startup of the application which can be costly for cold starts but R2R can greatly improve the startup.
R2R has been around for quite sometime but it has always been awkward to use especially from a development environment.

As mentioned in the .NET 6 [RC1 blog post](https://devblogs.microsoft.com/dotnet/announcing-net-6-release-candidate-1/#crossgen2)
there is a new tool to generate R2R images called crossgen2. This makes it really easy to create R2R images of your 
assemblies. All that you have to do is set the `PublishReadyToRun` property to `true` in the project file. Unlike 
past versions of .NET you no longer have to build on Linux to turn this feature. You can develop on any platform and 
publish to Lambda assemblies that have already been converted to machine specific instruction code.

Turning on R2R is also critical if you enable trimming because when the link trimming is executed it removes the 
R2R images from the system packages. So you need to enabled R2R to get all those assemblies optimized again. It
will increase the size of the deployment bundle a bit but it is worth it.

### The Code
The actual Lambda function in this case is a simple function that shows how to use the AWS SDK for .NET by returning back 
the list of S3 bucket. Since this is a custom runtime the programming model is a little different then
.NET Lambda functions for managed runtime like .NET Core 3.1.

In a custom runtime the project is a executable console application that contains a `main` function to bootstrap
the Lambda runtime.

Since this is .NET 6 we can take advantage of all of the latest C# features like top level statements to reduce the 
boiler plate code to a tiny amount.


```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;

using Amazon.S3;
using Amazon.S3.Model;

using var s3Client = new AmazonS3Client();

// Lambda function that returns the list of S3 buckets.
Func<ILambdaContext, Task<List<S3Bucket>>> listS3BucketsLambdaFunction = async (context) =>
{
    return (await s3Client.ListBucketsAsync()).Buckets;
};

// Startup Lambda .NET runtime
using var handlerWrapper = HandlerWrapper.GetHandlerWrapper(listS3BucketsLambdaFunction, new DefaultLambdaJsonSerializer());
using var bootstrap = new LambdaBootstrap(handlerWrapper);
await bootstrap.RunAsync();
```