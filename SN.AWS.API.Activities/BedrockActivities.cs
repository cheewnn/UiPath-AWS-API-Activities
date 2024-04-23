using Amazon;
using Amazon.OpenSearchService.Model;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using Amazon.Runtime.CredentialManagement;
using System.Runtime.CompilerServices;
using Amazon.BedrockRuntime.Model;
using Amazon.BedrockRuntime;
using System.Web;
using System.ComponentModel;
using System.Activities;
using System.Runtime.InteropServices;
using System.Text;


namespace SN.AWS.API.Activities
{
    internal class Utility
    {
        //Given a string convert it to a stream
        public static MemoryStream GetStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        //Converts a stream to a string
        public static string GetStringFromStream(Stream stream)
        {
            stream.Position = 0;
            var str = new StringBuilder();
            var reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            stream.Position = 0;
            return result;

        }

        //Save a base64 encoded image to a file
        public static void SaveBase64EncodedImage(string base64Image, string fileName)
        {
            string dir = System.IO.Path.GetDirectoryName(fileName);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            byte[] image = Convert.FromBase64String(base64Image);
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(image);
            }

        }
    }


    public class BedrockSendTextCompletion : CodeActivity
    {
        #region Properties
        [Category("Input")]
        [RequiredArgument]
        [DisplayName("Access Key")]
        [Description("Access key of your IAM User")]
        public InArgument<string> AccessKey { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [DisplayName("Secret Access Key")]
        [Description("Secret Access key of your IAM User")]
        public InArgument<string> SecretAccessKey { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [DisplayName("AWS Region Endpoint")]
        [Description("i.e. us-west-2")]    
        public InArgument<string> Region { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [DisplayName("Model Id")]
        [Description("Model Id of foundation model")]
        public InArgument<string> ModelID { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [DisplayName("Request Body")]
        [Description("Body of the API request. JSON string")]
        public InArgument<string> RequestBody { get; set; }

        [Category("Output")]
        [DisplayName("Completion String")]
        [Description("API response JSON Completion string")]
        public OutArgument<string> CompletionString { get; set; }

        #endregion

        protected override void Execute(CodeActivityContext context)
        {
            
            string in_AccessKey = AccessKey.Get(context);
            string in_SecretAccessKey = SecretAccessKey.Get(context);
            string in_Region = Region.Get(context);
            string in_ModelId = ModelID.Get(context);
            string in_RequestBody = RequestBody.Get(context);
            try
            {
                var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(in_AccessKey, in_SecretAccessKey);
                RegionEndpoint UserRegion = RegionEndpoint.GetBySystemName(in_Region);
                AmazonBedrockRuntimeClient client = new AmazonBedrockRuntimeClient(awsCredentials, UserRegion);
                InvokeModelRequest request = new InvokeModelRequest();
                request.ModelId = in_ModelId;
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.Body = Utility.GetStreamFromString(in_RequestBody);

                var result = client.InvokeModelAsync(request).Result;
                string content = Utility.GetStringFromStream(result.Body);
                Console.WriteLine(content);
                CompletionString.Set(context, content);
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.ToString());
                throw new Exception(ex.Message);
            }

        }
    }
}
