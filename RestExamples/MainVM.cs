using Haley.Abstractions;
using Haley.Enums;
using Haley.Events;
using Haley.Models;
using Haley.MVVM;
using Haley.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Haley.Rest;
using System.Windows.Input;
using System.Net.Http;

namespace RestExamples
{
    public class MainVM :BaseVM
    {
        private Dictionary<string, List<string>> _endpointsDictionary = new Dictionary<string, List<string>>();
        private string _request;
        public string RequestMessage
        {
            get { return _request; }
            set { SetProp(ref _request, value); }
        }

        private RequestBodyType requestBodyType;
        public RequestBodyType SelectedBodyType
        {
            get { return requestBodyType; }
            set { SetProp(ref requestBodyType, value); }
        }

        private Method _method;
        public Method SelectedMethod
        {
            get { return _method; }
            set { SetProp(ref _method, value); }
        }

        private ParamType paramType;
        public ParamType SelectedParamType
        {
            get { return paramType; }
            set { SetProp(ref paramType, value); }
        }

        private string _rawText;
        public string RawText
        {
            get { return _rawText; }
            set { SetProp(ref _rawText, value); }
        }

        private string _response;
        public string ResponseMessage
        {
            get { return _response; }
            set { SetProp(ref _response, value); }
        }

        private IClient _selectedClient;
        public IClient SelectedClient
        {
            get { return _selectedClient; }
            set 
            { 
                SetProp(ref _selectedClient, value);
                _setClientInfo();
                _getEndPoints();
            }
        }

        private string _tokenPrefix;
        public string TokenPrefix
        {
            get { return _tokenPrefix; }
            set { SetProp(ref _tokenPrefix, value); }
        }

        private string _tokenvalue;
        public string TokenValue
        {
            get { return _tokenvalue; }
            set { SetProp(ref _tokenvalue, value); }
        }

        private string _clientInfo;
        public string ClientInfo
        {
            get { return _clientInfo; }
            set { SetProp(ref _clientInfo, value); }
        }

        private ObservableCollection<IClient> _clients;
        public ObservableCollection<IClient> Clients
        {
            get { return _clients; }
            set { SetProp(ref _clients, value); }
        }

        private ObservableCollection<string> _endPoints;
        public ObservableCollection<string> EndPoints
        {
            get { return _endPoints; }
            set { SetProp(ref _endPoints, value); }
        }

        private ObservableCollection<ParameterSet> _kvpCollection;
        public ObservableCollection<ParameterSet> ParamsCollection
        {
            get { return _kvpCollection; }
            set { SetProp(ref _kvpCollection, value); }
        }

        private string _selectedEndPoint;
        public string SelectedEndPoint
        {
            get { return _selectedEndPoint; }
            set { SetProp(ref _selectedEndPoint, value); }
        }
        private TabEnum _selectedTab;
        public TabEnum SelectedTab
        {
            get { return _selectedTab; }
            set { SetProp(ref _selectedTab, value); }
        }

        private bool _dictionaryAsMultiformData;
        public bool DictionaryAsMultiformData
        {
            get { return _dictionaryAsMultiformData; }
            set { SetProp(ref _dictionaryAsMultiformData, value); }
        }

        public ICommand AddNewKvpCommand => new DelegateCommand(addNewKVP);
        public ICommand DeleteKVPCommand => new DelegateCommand<object>(deleteKVP);
        public ICommand SendRequestCommand => new DelegateCommand(sendRequest);
        public ICommand ClearTokensCommand => new DelegateCommand(clearToken);
        void clearToken()
        {
            TokenPrefix = string.Empty;
            TokenValue = string.Empty;
        }

        public MainVM() 
        {
            _initiate();
        }
        async void sendRequest()
        {
            ResponseMessage = null;
            RequestMessage = null;
            IResponse response = null;

            //if token value is not null, then add authentication to the request.
            if (!string.IsNullOrWhiteSpace(TokenValue))
            {
                SelectedClient.AddRequestAuthentication(TokenValue, TokenPrefix);
            }
            else
            {
                SelectedClient.ClearRequestAuthentication().ClearRequestHeaders();
            }

            switch (SelectedTab)
            {
                case TabEnum.Parameters:
                    response = await sendParameters();
                    break;
                case TabEnum.RawText:
                    response = await sendRawText();
                    break;
                case TabEnum.MultiFormData:
                    break;
                case TabEnum.FileUpload:
                    break;
            }

            if (response == null) return;

            RequestMessage = RequestMessage + response.OriginalResponse.RequestMessage.ToString();
            if (response.IsSuccess && response is StringResponse strrspns)
            {
                ResponseMessage = strrspns.StringContent;
            }
            else
            {
                StringBuilder responseBuilder = new StringBuilder();
                responseBuilder.AppendLine("FAILED");
                responseBuilder.AppendLine("####-----#####");
                responseBuilder.AppendLine(response.OriginalResponse?.ReasonPhrase);
                responseBuilder.AppendLine(await parseContent(response.Content));
                ResponseMessage = responseBuilder.ToString();
            }
        }

        async Task<IResponse> sendRawText()
        {
            //validate all values.
            var _response = await _selectedClient.SendAsync(SelectedEndPoint, RawText, SelectedMethod,SelectedParamType,true);
            return _response;
        }

        async Task<IResponse> sendParameters()
        {
            IResponse response = null;
            if (SelectedMethod == Method.Post)
            {
                Dictionary<string, string> _paramdic = new Dictionary<string, string>();
                foreach (var item in ParamsCollection)
                {
                    if (string.IsNullOrWhiteSpace(item.Key)) continue;
                    if (!_paramdic.ContainsKey(item.Key))
                    {
                        _paramdic.Add(item.Key, item.Value);
                    }
                }
                
                response = await _selectedClient.PostAsync(SelectedEndPoint, _paramdic);
            }
            else
            {
                var _parmlist = new List<RestParam>();
                foreach (var item in ParamsCollection)
                {
                    _parmlist.Add(new RestParam(item.Key, item.Value, true, SelectedParamType, SelectedBodyType));
                }
                //validate all values.
                response = await _selectedClient.SendAsync(SelectedEndPoint, _parmlist, SelectedMethod);
            }
            return response;
        }

        private async Task<string> parseContent(HttpContent content)
        {
            try
            {
                if (content == null) return "EMPTY CONTENT";
                var _msg = await content.ReadAsStringAsync();
                return _msg;
            }
            catch (Exception ex)
            {
                return "CONTENT NOT AVAILABLE";
            }
        }
        void addNewKVP()
        {
            //Add a new kvp item.
            ParamsCollection.Add(new ParameterSet("test1",string.Empty));
        }
        void deleteKVP(object obj)
        {
            var _paramcol = ParamsCollection.ToList();
            if (obj is ParameterSet set)
            {
                if (ParamsCollection.Contains(set))
                {
                    ParamsCollection.Remove(set);
                }
            }
        }

        private async Task<bool> gorestCallback(HttpRequestMessage arg)
        {
            try
            {
                StringBuilder requestBuilder = new StringBuilder();
                requestBuilder.AppendLine(await parseContent(arg.Content));
                RequestMessage = RequestMessage + requestBuilder.ToString();
                return true;
            }
            catch (Exception ex)
            {
                return true;
            }
        }
        private void _initiate()
        {
            Clients = new ObservableCollection<IClient>();
            ParamsCollection = new ObservableCollection<ParameterSet>();
            SelectedTab = TabEnum.Parameters;
            DictionaryAsMultiformData = true;

            //PREPARE CLIENTS
            //CLIENT 1
            var _client1 = ClientStore.AddClient(APIEnums.publicAPI, $@"https://api.publicapis.org", "PublicAPI");
            _endpointsDictionary.Add(_client1.Id, new List<string>()
            {
              "random","entries",
            });
            //CLIENT 2
            var _client2 = ClientStore.AddClient(APIEnums.goRest, $@"https://gorest.co.in/", "GoRest",gorestCallback);
            _endpointsDictionary.Add(_client2.Id, new List<string>()
            {
                "public/v1/users","public/v1/posts","public/v1/comments","public/v1/todos"
            });

            //CLIENT 3
            var _client3 = ClientStore.AddClient(APIEnums.jsonPlaceHolder, $@"https://jsonplaceholder.typicode.com", "JsonPlaceHolder");
            _endpointsDictionary.Add(_client3.Id, new List<string>()
            {

            });
            //ADD CLIENTS
            Clients.Add(_client2);
            Clients.Add(_client1);
            Clients.Add(_client3);
            SelectedClient = Clients.FirstOrDefault();
        }

        private void _setClientInfo()
        {
            if (SelectedClient == null) ClientInfo = "Client data is empty.";
            StringBuilder sbuilder = new StringBuilder();
            sbuilder.AppendLine($@"{"Name".PadRight(10)} : {SelectedClient.FriendlyName}");
            sbuilder.AppendLine($@"{"URI".PadRight(10)} : {SelectedClient.BaseURI}");
            sbuilder.AppendLine($@"{"Id".PadRight(10)} : {SelectedClient.Id}");
            ClientInfo = sbuilder.ToString();
        }

        private void _getEndPoints()
        {
            EndPoints = null;
            SelectedEndPoint = null;
            if (_endpointsDictionary.ContainsKey(SelectedClient.Id))
            {
                EndPoints = new ObservableCollection<string>(_endpointsDictionary[SelectedClient.Id]);
                SelectedEndPoint = EndPoints.FirstOrDefault();
            }
        }
    }
}
