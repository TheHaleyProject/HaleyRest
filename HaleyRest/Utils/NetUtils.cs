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

namespace Haley.Utils
{
    public static class NetUtils
    {
        public static string DownloadFromWeb(string download_link, string file_name)
        {
            try
            {
                string _path = null;
                _path = Path.Combine(Path.GetTempPath() + file_name);
                if (File.Exists(_path)) File.Delete(_path); 
                Uri _download_url = new Uri((string)download_link);
                using (var _client = new WebClient())
                {
                    _client.DownloadFile(_download_url, _path);
                }
                return _path;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
