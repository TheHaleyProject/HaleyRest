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

        //In a computing context, an epoch is the date and time relative to which a computer's clock and timestamp values are determined
        public static readonly DateTime Epoch = new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc);
        #region Properties
        public static OAuthUtil OAuth = new OAuthUtil();

        #endregion
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
                var bytes = HashUtils.GetRandomBytes(32); //32 bits is 4 byte.
                _random = new Random(BitConverter.ToInt32(bytes.bytes,0));
            }
        }

        private const string Digit = "1234567890";
        private const string Alphabets = "abcdefghijklmnopqrstuvwxyz";
        private static Random _random;
        private static readonly object _randomLock = new object();
    }
}
