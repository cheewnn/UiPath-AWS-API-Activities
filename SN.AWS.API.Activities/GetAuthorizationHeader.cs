using Amazon.Runtime.Internal;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SN.AWS.API.Activities
{
    public class GetAWS4AuthHeader : CodeActivity
    {
        #region Properties
        [Category("Input")]
        [RequiredArgument]
        [DisplayName("Endpoint URL")]
        [Description("Endpoint URL of request. It should in the format of: 'https://{host}}/{action}'.\ni.e. https://bedrock-runtime.us-west-2.amazonaws.com/model/amazon.titan-text-express-v1/invoke")]
        public InArgument<string> Endpoint { get; set; }

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
        [DisplayName("Host")]
        [Description("i.e. https://bedrock-runtime.us-west-2.amazonaws.com")]
        public InArgument<string> Host { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [DisplayName("Http Method")]
        [Description("Http Method. POST/GET/PUT...")]
        public InArgument<string> HTTPMethod { get; set; }

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
        [Description("Additional headers for request. Dictionary: key = header name, value = header value")]
        public InArgument<Dictionary<string, string>> AdditionalHeaders { get; set; }

        [Category("Output")]
        [DisplayName("Authentication Header")]
        [Description("Calculated Authentication header value.")]
        public OutArgument<string> AuthorizationHeader { get; set; }

        [Category("Output")]
        [DisplayName("Request Headers")]
        [Description("All request headers required for the http request")]
        public OutArgument<Dictionary<string,string>> RequestHeaders { get; set; }

        [Category("Output")]
        [DisplayName("Signature")]
        [Description("Calculated signature")]
        public OutArgument<string> Signature { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext context)
        {
            string in_Endpoint = Endpoint.Get(context);
            string in_AccessKey = AccessKey.Get(context);
            string in_SecretAccessKey = SecretAccessKey.Get(context);
            string in_Host = Host.Get(context);
            string in_Method = HTTPMethod.Get(context);
            string in_Region = Region.Get(context);
            string in_Service = Service.Get(context);
            string in_RequestContent = RequestContent.Get(context);
            string in_ContentType = ContentType.Get(context);
            Dictionary<string, string> in_AdditionalHeaders = AdditionalHeaders.Get(context);

            var _RequestHeaders = new Dictionary<string, string>();
            if (in_AdditionalHeaders != null)
            {
                _RequestHeaders = DeepCopy(in_AdditionalHeaders);
            } 
            var signer = new AWS4Signer(in_AccessKey, in_SecretAccessKey);
            var EndpointUri = new Uri(in_Endpoint);
            if (in_Host == null)
            {
                in_Host = EndpointUri.Host;
            }
            _RequestHeaders.Add("host", in_Host);
            byte[] bytesToHash = new byte[0];
            if (in_RequestContent != null)
            {
                bytesToHash = Encoding.UTF8.GetBytes(in_RequestContent);
            }
            string str1 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
            if (bytesToHash.Length != 0)
            {
                str1 = signer.Hash(bytesToHash);
            }
            if (!_RequestHeaders.Keys.Contains("x-amz-content-sha256"))
            {
                _RequestHeaders.Add("x-amz-content-sha256", str1);
            }
            string str2 = "";
            if (_RequestHeaders.Keys.Contains("x-amz-date")){
                DateTime validatedDateTime;
                if (!DateTime.TryParseExact(_RequestHeaders["x-amz-date"], "yyyyMMddTHHmmssZ", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None,  out validatedDateTime))
                {
                    throw new Exception($"x-amz-date: {_RequestHeaders["x-amz-date"]} set in additional headers is of the wrong format. Please enter with format 'yyyyMMddTHHmmssZ'");
                } else
                {
                    str2 = _RequestHeaders["x-amz-date"];
                    Console.WriteLine( str2 );
                }
            } else
            {
                
                DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
                str2 = dateTimeOffset.ToString("yyyyMMddTHHmmssZ");
                _RequestHeaders.Add("x-amz-date", str2);
            }


            string dateStamp = str2.Split("T")[0];
            Console.WriteLine(dateStamp);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(in_Method.ToString() + "\n");
            stringBuilder.Append(string.Join("/", ((IEnumerable<string>)EndpointUri.AbsolutePath.Split('/')).Select<string, string>(new Func<string, string>(Uri.EscapeDataString))) + "\n");
            string canonicalQueryParams = GetCanonicalQueryParams(EndpointUri);
            stringBuilder.Append(canonicalQueryParams + "\n");
            List<string> values = new List<string>();
            var SortedHeaders = new SortedDictionary<string, string>((IComparer<string>)StringComparer.Ordinal);
            foreach(KeyValuePair<string, string> kvp in _RequestHeaders)
            {
                SortedHeaders.Add(kvp.Key, kvp.Value);
            }
            foreach (KeyValuePair<string, string> keyValuePair in SortedHeaders)
            {
                stringBuilder.Append(keyValuePair.Key.ToLowerInvariant());
                stringBuilder.Append(":");
                stringBuilder.Append(string.Join(",", keyValuePair.Value.Trim()));
                stringBuilder.Append("\n");
                values.Add(keyValuePair.Key.ToLowerInvariant());
            }
            stringBuilder.Append("\n");
            string str3 = string.Join(";", (IEnumerable<string>)values);
            stringBuilder.Append(str3+"\n");
            stringBuilder.Append(str1);
            string str4 = dateStamp + "/" + in_Region + "/" + in_Service + "/aws4_request";
            string data = "AWS4-HMAC-SHA256\n" + str2 + "\n" + str4 + "\n" + signer.Hash(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
            string hexString = AWS4Signer.ToHexString((IReadOnlyCollection<byte>)AWS4Signer.HmacSha256(AWS4Signer.GetSignatureKey(in_SecretAccessKey, dateStamp, in_Region, in_Service), data));
            string _AuthenticationHeader = "AWS4-HMAC-SHA256 Credential=" + in_AccessKey + "/" + str4 + ", SignedHeaders=" + str3 + ", Signature=" + hexString;
            _RequestHeaders.Add("Authorization", _AuthenticationHeader);
            AuthorizationHeader.Set(context, _AuthenticationHeader);
            RequestHeaders.Set(context, _RequestHeaders);
            Signature.Set(context,hexString);
        }
        

        private static string GetCanonicalQueryParams(Uri RequestUri)
        {
            SortedDictionary<string, IEnumerable<string>> source = new SortedDictionary<string, IEnumerable<string>>((IComparer<string>)StringComparer.Ordinal);
            NameValueCollection queryString = HttpUtility.ParseQueryString(RequestUri.Query);
            foreach (string allKey in queryString.AllKeys)
            {
                string key = allKey;
                if (key == null)
                {
                    source.Add(Uri.EscapeDataString(queryString[key]), (IEnumerable<string>)new string[1]
                    {
            Uri.EscapeDataString(queryString[key]) + "="
                    });
                }
                else
                {
                    IEnumerable<string> strings = ((IEnumerable<string>)queryString[key].Split(',')).OrderBy<string, string>((Func<string, string>)(v => v)).Select<string, string>((Func<string, string>)(v => Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(v)));
                    source.Add(Uri.EscapeDataString(key), strings);
                }
            }
            return string.Join("&", source.SelectMany<KeyValuePair<string, IEnumerable<string>>, string>((Func<KeyValuePair<string, IEnumerable<string>>, IEnumerable<string>>)(a => a.Value)));
        }

        private static Dictionary<TKey, TValue> DeepCopy<TKey, TValue>(Dictionary<TKey, TValue> original)
        {
            // Serialize the original dictionary
            using (var memoryStream = new System.IO.MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, original);
                memoryStream.Position = 0;

                // Deserialize the serialized data to get a deep copy
                return (Dictionary<TKey, TValue>)formatter.Deserialize(memoryStream);
            }
        }
    }
}
