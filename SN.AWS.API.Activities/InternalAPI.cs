using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SN.AWS.API.Activities
{
    internal class AWS4Signer : IDisposable
    {
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly SHA256 _sha256;
        private const string ALGORITHM = "AWS4-HMAC-SHA256";
        private const string EMPTY_STRING_HASH = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        public AWS4Signer(string accessKey, string secretKey)
        {
            if (string.IsNullOrEmpty(accessKey))
                throw new ArgumentOutOfRangeException(nameof(accessKey), (object)accessKey, "Not a valid access_key.");
            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentOutOfRangeException(nameof(secretKey), (object)secretKey, "Not a valid secret_key.");
            this._accessKey = accessKey;
            this._secretKey = secretKey;
            this._sha256 = SHA256.Create();
        }
        public string Hash(byte[] bytesToHash) => AWS4Signer.ToHexString((IReadOnlyCollection<byte>)this._sha256.ComputeHash(bytesToHash));

        public static byte[] HmacSha256(byte[] key, string data) => new HMACSHA256(key).ComputeHash(Encoding.UTF8.GetBytes(data));

        public static byte[] GetSignatureKey(
          string key,
          string dateStamp,
          string regionName,
          string serviceName)
        {
            return AWS4Signer.HmacSha256(AWS4Signer.HmacSha256(AWS4Signer.HmacSha256(AWS4Signer.HmacSha256(Encoding.UTF8.GetBytes("AWS4" + key), dateStamp), regionName), serviceName), "aws4_request");
        }

        public static string ToHexString(IReadOnlyCollection<byte> array)
        {
            StringBuilder stringBuilder = new StringBuilder(array.Count * 2);
            foreach (byte num in (IEnumerable<byte>)array)
                stringBuilder.AppendFormat("{0:x2}", (object)num);
            return stringBuilder.ToString();
        }

        public async Task<HttpRequestMessage> SignRequest(
          HttpRequestMessage request,
          string service,
          string region,
          TimeSpan? timeOffset = null)
        {
            if (string.IsNullOrEmpty(service))
                throw new ArgumentOutOfRangeException(nameof(service), (object)service, "Not a valid service.");
            if (string.IsNullOrEmpty(region))
                throw new ArgumentOutOfRangeException(nameof(region), (object)region, "Not a valid region.");
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Headers.Host == null)
                request.Headers.Host = request.RequestUri.Host;
            byte[] bytesToHash = new byte[0];
            if (request.Content != null)
                bytesToHash = await request.Content.ReadAsByteArrayAsync();
            string str1 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
            if (bytesToHash.Length != 0)
                str1 = this.Hash(bytesToHash);
            if (!request.Headers.Contains("x-amz-content-sha256"))
                request.Headers.Add("x-amz-content-sha256", str1);
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
            if (timeOffset.HasValue)
                dateTimeOffset = dateTimeOffset.Add(timeOffset.Value);
            string str2 = "";
            string dateStamp;
            if (request.Headers.Contains("x-amz-date"))
            {
                DateTime validatedDateTime;
                if (!DateTime.TryParseExact(request.Headers.GetValues("x-amz-date").First(), "yyyyMMddTHHmmssZ", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out validatedDateTime))
                {
                    throw new Exception($"x-amz-date: {request.Headers.GetValues("x-amz-date").First()} set in additional headers is of the wrong format. Please enter with format 'yyyyMMddTHHmmssZ'");
                }
                else
                {
                    str2 = request.Headers.GetValues("x-amz-date").First();

                    dateStamp = str2.Split("T")[0];
                    
                }
            }
            else
            {
                
                str2 = dateTimeOffset.ToString("yyyyMMddTHHmmssZ");
                request.Headers.Add("x-amz-date", str2);
                dateStamp = dateTimeOffset.ToString("yyyyMMdd");
                //dateStamp = dateTimeOffset.ToString().Split("T")[0];
            }
            
            
            
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(request.Method?.ToString() + "\n");
            stringBuilder.Append(string.Join("/", ((IEnumerable<string>)request.RequestUri.AbsolutePath.Split('/')).Select<string, string>(new Func<string, string>(Uri.EscapeDataString))) + "\n");
            string canonicalQueryParams = AWS4Signer.GetCanonicalQueryParams(request);
            stringBuilder.Append(canonicalQueryParams + "\n");
            List<string> values = new List<string>();
            foreach (KeyValuePair<string, IEnumerable<string>> keyValuePair in (IEnumerable<KeyValuePair<string, IEnumerable<string>>>)request.Headers.OrderBy<KeyValuePair<string, IEnumerable<string>>, string>((Func<KeyValuePair<string, IEnumerable<string>>, string>)(a => a.Key.ToLowerInvariant()), (IComparer<string>)StringComparer.OrdinalIgnoreCase))
            {
                stringBuilder.Append(keyValuePair.Key.ToLowerInvariant());
                stringBuilder.Append(":");
                stringBuilder.Append(string.Join(",", keyValuePair.Value.Select<string, string>((Func<string, string>)(s => s.Trim()))));
                stringBuilder.Append("\n");
                values.Add(keyValuePair.Key.ToLowerInvariant());
            }
            stringBuilder.Append("\n");
            string str3 = string.Join(";", (IEnumerable<string>)values);
            stringBuilder.Append(str3 + "\n");
            stringBuilder.Append(str1);
            string str4 = dateStamp + "/" + region + "/" + service + "/aws4_request";
            string data = "AWS4-HMAC-SHA256\n" + str2 + "\n" + str4 + "\n" + this.Hash(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
            string hexString = AWS4Signer.ToHexString((IReadOnlyCollection<byte>)AWS4Signer.HmacSha256(AWS4Signer.GetSignatureKey(this._secretKey, dateStamp, region, service), data));
            request.Headers.TryAddWithoutValidation("Authorization", "AWS4-HMAC-SHA256 Credential=" + this._accessKey + "/" + str4 + ", SignedHeaders=" + str3 + ", Signature=" + hexString);
            return request;
        }


        private static string GetCanonicalQueryParams(HttpRequestMessage request)
        {
            SortedDictionary<string, IEnumerable<string>> source = new SortedDictionary<string, IEnumerable<string>>((IComparer<string>)StringComparer.Ordinal);
            NameValueCollection queryString = HttpUtility.ParseQueryString(request.RequestUri.Query);
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

        public void Dispose() => this._sha256.Dispose();
    }
    
}
