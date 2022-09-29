using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Runtime;
using System.Runtime.CompilerServices;
using Haley.Models;
using Haley.Enums;
using System.Text.Json;
using System.Xml.Schema;
using Microsoft.Extensions.Logging;

namespace Haley.Utils
{
    public static class NetUtils {
        internal static readonly string[] UriRfc3986CharsToEscape = { "!", "*", "'", "(", ")" };
        internal static readonly string[] UriRfc3968EscapedHex = { "%21", "%2A", "%27", "%28", "%29" };
        internal const string Digit = "1234567890";
        internal const string Alphabets = "abcdefghijklmnopqrstuvwxyz";
        internal const string AlphabetsUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        internal const string Unreserved = AlphabetsUpper + Alphabets + Digit + "-._~";

        private static Random _random;
        private static readonly object _randomLock = new object();
        //In a computing context, an epoch is the date and time relative to which a computer's clock and timestamp values are determined
        public static readonly DateTime Epoch = new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc);
        public static string DownloadFromWeb(string download_link, string file_name) {
            try {
                string _path = null;
                _path = Path.Combine(Path.GetTempPath() + file_name);
                if (File.Exists(_path)) File.Delete(_path);
                Uri _download_url = new Uri(download_link);
                using (var _client = new WebClient()) {
                    _client.DownloadFile(_download_url, _path);
                }
                return _path;
            } catch (Exception ex) {
                throw ex;
            }
        }

        public static string GetTimeStamp() {
            return GetUnixTimeStamp(DateTime.UtcNow);
        }

        public static string GetUnixTimeStamp(DateTime datetime) {
            //Generate Unix Time (seconds consumed since 1-1-1970)
            var timeSpan = (datetime - Epoch);
            var totalseconds = (long)timeSpan.TotalSeconds;
            return totalseconds.ToString();
        }

        //BELOW PARTS FROM REST SHARP

        #region Rest Sharp(nuget) / OAuth(Nuget) Methods
        public static string UrlEncodeRelaxed(string value) {
            // Escape RFC 3986 chars first.
            var escapedRfc3986 = new StringBuilder(value);

            for (var i = 0; i < UriRfc3986CharsToEscape.Length; i++) {
                var t = UriRfc3986CharsToEscape[i];

                escapedRfc3986.Replace(t, UriRfc3968EscapedHex[i]);
            }

            // Do RFC 2396 escaping by calling the .NET method to do the work.
            var escapedRfc2396 = Uri.EscapeDataString(escapedRfc3986.ToString());

            // Return the fully-RFC3986-escaped string.
            return escapedRfc2396;
        }

        /// <summary>
        /// URL encodes a string based on section 5.1 of the OAuth spec.
        /// Namely, percent encoding with [RFC3986], avoiding unreserved characters,
        /// upper-casing hexadecimal characters, and UTF-8 encoding for text value pairs.
        /// </summary>
        public static string UrlEncodeStrict(string value)
            => string.Join("", value.Select(x => Unreserved.Contains(x) ? x.ToString() : $"%{(byte)x:X2}"));

        public static string ConstructRequestUrl(Uri url) {
            if (url == null) {
                throw new ArgumentNullException("url");
            }

            var sb = new StringBuilder();

            var requestUrl = string.Format("{0}://{1}", url.Scheme, url.Host); //Something like, http://hippobim.com
            var qualified = string.Format(":{0}", url.Port);
            var basic = url.Scheme == "http" && url.Port == 80;
            var secure = url.Scheme == "https" && url.Port == 443;

            sb.Append(requestUrl);
            sb.Append(!basic && !secure ? qualified : "");
            sb.Append(url.AbsolutePath);

            return sb.ToString(); //.ToLower();
        }

        public static string GetNonce(int numberOfBits = 32, int resultLength = 0) {
            ////CONCEPT 1-Not working (because we need exaclty 32 bit) but alphanumeric replaces the symbols which returns error
            //var nonce = HashUtils.GetRandomAlphaNumericValue(numberOfBits);
            //if (resultLength > 0) return nonce.Substring(0, resultLength);
            //return nonce;

            //CONCEPT 2
            InitializeRandom();
            const string chars = (Alphabets + Digit);
            var nonce = new char[16];
            lock (_randomLock) {
                for (var i = 0; i < nonce.Length; i++) {
                    nonce[i] = chars[_random.Next(0, chars.Length)];
                }
            }
            return new string(nonce);

            //return Guid.NewGuid().ToString();

            //return Convert.ToBase64String(Encoding.UTF8.GetBytes(GetTimeStamp()));
        }

        private static void InitializeRandom() {
            if (_random == null) {
                _random = new Random();
            };
        }
        #endregion

    }
}
