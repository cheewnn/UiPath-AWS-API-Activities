using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SN.AWS.API.Activities
{
    #region SendHttpRequestActivity
    
    public class AWS4SendHttpRequest : CodeActivity
    {
        #region Properties
        [Category("Input")]
        [RequiredArgument]
        [DisplayName("Endpoint URL")]
        [Description("Endpoint URL of request. It should in the format of: 'https://{host}}/{action}'.\ni.e. https://bedrock-runtime.us-west-2.amazonaws.com/model/amazon.titan-text-express-v1/invoke")]
        public InArgument<string> Endpoint { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [DisplayName("Http Method")]
        [Description("Http Method. POST/GET/PUT...")]
        public InArgument<string> HTTPMethod { get; set; }

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
        [DisplayName("AWS Service Name")]
        [Description("i.e. bedrock")]
        public InArgument<string> Service { get; set; }

        [Category("Input")]
        [DisplayName("Request Body")]
        [Description("Body of the API request.")]
        public InArgument<string> RequestContent { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [DisplayName("Content Type")]
        [Description("Content type of the request body. i.e. application/json")]
        public InArgument<string> ContentType { get; set; }

        [Category("Input")]
        [DisplayName("Additional Headers")]
        [Description("[Optional] Additional headers for request. Dictionary: key = header name, value = header value")]
        public InArgument<Dictionary<string,string>> AdditionalHeaders { get; set; }

        [Category("Output")]
        [DisplayName("Response String")]
        [Description("Response string from http response")]
        public OutArgument<string> ResponseString { get; set; }

        [Category("Output")]
        [DisplayName("Http Response")]
        [Description("Response string from http response")]
        public OutArgument<HttpResponseMessage> HttpResponse { get; set; }
        #endregion


        protected override void Execute(CodeActivityContext context)
        {
            string in_Endpoint = Endpoint.Get(context);
            string in_Method = HTTPMethod.Get(context);
            string in_AccessKey = AccessKey.Get(context);
            string in_SecretAccessKey = SecretAccessKey.Get(context);
            string in_Region = Region.Get(context);
            string in_Service = Service.Get(context);
            string in_RequestContent = RequestContent.Get(context);
            string in_ContentType = ContentType.Get(context);
            Dictionary<string,string> in_AdditionalHeaders = AdditionalHeaders.Get(context);

            var signer = new AWS4Signer(in_AccessKey, in_SecretAccessKey);
            var content = new StringContent(in_RequestContent);

            try
            {
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(in_ContentType);
            } 
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new Exception("Content type requested is invalid. Please check that it is a valid content type.");
            }
            var method = new HttpMethod(in_Method.ToString().ToUpper());
            var httpRequest = new HttpRequestMessage
            {
                Method = method,
                RequestUri = new Uri(in_Endpoint),
                Content = content
            };

            if (in_AdditionalHeaders != null)
            {
                foreach (KeyValuePair<string, string> headers in in_AdditionalHeaders)
                {
                    httpRequest.Headers.Add(headers.Key, headers.Value);
                }
            }            

            try 
            {
                httpRequest = signer.SignRequest(httpRequest, in_Service, in_Region).ConfigureAwait(false).GetAwaiter().GetResult();
                var client = new HttpClient();
                var response = client.SendAsync(httpRequest).ConfigureAwait(false).GetAwaiter().GetResult();
                var responseStr = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                ResponseString.Set(context, responseStr);
                HttpResponse.Set(context, response);

            } catch (Exception ex)
            {
                httpRequest.Dispose();
                throw new Exception(ex.Message);
            }
        }
    }
    #endregion
}
