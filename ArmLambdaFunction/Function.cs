using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Translate;
using Amazon.Translate.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ArmLambdaFunction
{
    public class Function
    {
        IAmazonTranslate _translateClient = new AmazonTranslateClient();

        public async Task<string> FunctionHandler(string input, ILambdaContext context)
        {
            var response = await _translateClient.TranslateTextAsync(new TranslateTextRequest
            {
                SourceLanguageCode = "en",
                TargetLanguageCode = "es",
                Text = input
            });

            return response.TranslatedText;
        }
    }
}
