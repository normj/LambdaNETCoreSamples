using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;

using Amazon.S3;

using var s3Client = new AmazonS3Client();

// Lambda function that returns the list of S3 buckets.
var handler = async (ILambdaContext context) =>
{
    context.Logger.LogLine("Making SDK call to get S3 buckets");
    return (await s3Client.ListBucketsAsync()).Buckets;
};

// Startup Lambda .NET runtime
await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
    .Build()
    .RunAsync();