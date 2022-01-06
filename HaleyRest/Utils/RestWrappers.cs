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
    public static class RestWrappers
    {
        #region GitHub
        public static class GitHub
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="directory_url"></param>
            /// <param name="download_list"> If null, then all files in the directory will be downloaded.</param>
            /// <param name="ignore_sha_list">If not null, such sha values will be ignored while downloading</param>
            /// <returns></returns>
            public static async Task<Dictionary<string, string>> GetFiles(string download_url, List<string> download_list = null, List<string> ignore_sha_list = null)
            {
                try
                {
                    Dictionary<string, string> result = new Dictionary<string, string>(); //Dowload path , Sha value
                                                                                          //Check if the directory is working
                    var _result = await invoke(null, download_url, is_anonymous: true); //Whenever we send a request, it checks whether it
                    if (_result.status_code != HttpStatusCode.OK) return result;

                    dynamic _dir_files = JsonSerializer.Deserialize<dynamic>(_result.content);
                    //dynamic _dir_files = JsonConvert.DeserializeObject(_result.content); //Gets all the files inside the directory

                    foreach (var _file in _dir_files) //We get the list of all files in the folder. We can either choose to download single file or all the files.
                    {
                        //Get file details
                        string _download_url = _file.download_url;
                        string _name = _file.name;
                        string server_sha = _file.sha;

                        //Condition 1 : Download File
                        if (download_list != null && download_list?.Count > 0)
                        {
                            if (!(download_list.Contains(_name))) continue; //Our download list doesn't contain this file
                        }

                        //Condition 2 : Ignore Sha
                        if (ignore_sha_list != null && ignore_sha_list?.Count > 0) //Very rare that two files have same sha
                        {
                            if (ignore_sha_list.Contains(server_sha)) continue; //We need to ignore this because we already have this file in our local with same sha
                        }

                        //Passed all above?  Well, go ahead and download.
                        string _downloaded_file = downloadFromWeb(_download_url, _name); //Since this is not async method, we will wait till we get the downloads
                        if (!result.ContainsKey(_downloaded_file)) result.Add(_downloaded_file, server_sha);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="github_url">Should be a directy file and not a directory</param>
            /// <returns></returns>
            public static async Task<(string downloaded_file_sha, string downloaded_path)> GetFile(string github_file_url)
            {
                try
                {
                    string _path = null;
                    string _sha = null;

                    var github_response = await invoke(null, github_file_url, is_anonymous: true); //Whenever we send a request, it checks whether it
                    if (github_response.status_code != HttpStatusCode.OK) return (null, null);

                    //Get content
                    //dynamic _content = JsonConvert.DeserializeObject(github_response.content);
                    dynamic _content = JsonSerializer.Deserialize<dynamic>(github_response.content);
                    //Since we know that it is from github, we also know that it is supposed to have download url
                    if (_content.download_url != null)
                    {
                        _path = downloadFromWeb((string)_content.download_url, (string)_content.name);
                        _sha = _content.sha;
                    }
                    return (_sha, _path);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
        #endregion
    }
}
