using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;

using Amazon.S3;
using Amazon.S3.Model;

using var s3Client = new AmazonS3Client();

// Delegate that returns the list of S3 buckets.
async Task<List<S3Bucket>> ListBucketsDelegate(ILambdaContext context) => (await s3Client.ListBucketsAsync()).Buckets;

// Startup Lambda .NET runtime
using var handlerWrapper = HandlerWrapper.GetHandlerWrapper(ListBucketsDelegate, new DefaultLambdaJsonSerializer());
using var bootstrap = new LambdaBootstrap(handlerWrapper);
await bootstrap.RunAsync();