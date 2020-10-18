using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Translate;
using Amazon.Translate.Model;
using System;
using System.Threading.Tasks;


using var translateClient = new AmazonTranslateClient();


Func<string, ILambdaContext, Task<string>> func = FunctionHandler;
using(var handlerWrapper = HandlerWrapper.GetHandlerWrapper(func, new DefaultLambdaJsonSerializer()))
using(var bootstrap = new LambdaBootstrap(handlerWrapper))
{
    await bootstrap.RunAsync();
}

async Task<string> FunctionHandler(string input, ILambdaContext context)
{
    var request = new TranslateTextRequest
    {
        Text = input,
        SourceLanguageCode = "en",
        TargetLanguageCode = "es"
    };

    var response = await translateClient.TranslateTextAsync(request);

    return response.TranslatedText;
}
