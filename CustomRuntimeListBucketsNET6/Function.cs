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