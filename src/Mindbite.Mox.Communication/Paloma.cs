using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Mindbite.Mox.Communication
{
    public enum PalomaMessageStatusTypes
    {
        Unknown = 0,
        OK = 200,
        Failed = 500,
        WrongApiKey = 501
    }

    public class PalomaMessage
    {
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
        public string CallBackUrl { get; set; }
        public DateTime SendOn { get; set; }

        private string ApiKey { get; set; }

        private PalomaMessageStatusTypes _MessageStatus;
        public PalomaMessageStatusTypes MessageStatus
        {
            get
            {
                return this._MessageStatus;
            }
        }

        private string _MessageUID;
        public string MessageUID
        {
            get
            {
                return this._MessageUID;
            }
        }

        public PalomaMessage(string ApiKey)
        {
            this.ApiKey = ApiKey.Trim();
            this._MessageStatus = PalomaMessageStatusTypes.Unknown;
        }



        public async Task Send()
        {
            bool IsOk = true;
            if (string.IsNullOrEmpty(this.PhoneNumber.Trim())) { IsOk = false; }
            if (string.IsNullOrEmpty(this.Message.Trim())) { IsOk = false; }

            if (IsOk)
            {
                var response = await this.AddToPalomaQueue();
                if (response.uid == "wrongapikey")
                {
                    this._MessageStatus = PalomaMessageStatusTypes.WrongApiKey;
                }
                else
                {
                    this._MessageStatus = PalomaMessageStatusTypes.OK;
                    this._MessageUID = response.uid;
                }
            }
            else
            {
                this._MessageUID = "";
                this._MessageStatus = PalomaMessageStatusTypes.Failed;
            }

        }

        private async Task<PalomaMessageResponse> AddToPalomaQueue()
        {
            var jsonPostData = ConvertToJson<PalomaMessageRequest>(new PalomaMessageRequest()
            {
                phoneNumber = this.PhoneNumber,
                messageBody = this.Message,
                sendOn = this.SendOn,
                callbackUrl = this.CallBackUrl + "",
            });

            var PostDataContent = new StringContent(jsonPostData, Encoding.UTF8, "application/json");

            var client = new HttpClient();
            var serializer = new DataContractJsonSerializer(typeof(PalomaMessageResponse), new DataContractJsonSerializerSettings
            {
                DateTimeFormat = new DateTimeFormat("yyyy-MM-dd'T'HH:mm:ssZ")
            });

            client.BaseAddress = new Uri("https://paloma.mindbite.se/webapi/v1/" + this.ApiKey + "/queuemessage");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var postResponse = await client.PostAsync(client.BaseAddress, PostDataContent);
            var jsonResponse = await postResponse.Content.ReadAsStreamAsync();

            var palomaResponse = serializer.ReadObject(jsonResponse) as PalomaMessageResponse;
            return palomaResponse;
        }

        private static string ConvertToJson<T>(T data)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
            {
                DateTimeFormat = new DateTimeFormat("yyyy-MM-dd'T'HH:mm:ssZ")
            });
            
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, data);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }

    [DataContract]
    public class PalomaMessageRequest
    {
        [DataMember]
        public string phoneNumber { get; set; }
        [DataMember]
        public string messageBody { get; set; }
        [DataMember]
        public DateTime sendOn { get; set; }
        [DataMember]
        public string callbackUrl { get; set; }

    }

    [DataContract]
    public class PalomaMessageResponse
    {

        [DataMember]
        public string uid { get; set; }
        [DataMember]
        public bool wasQueued { get; set; }
        [DataMember]
        public string serverMessage { get; set; }

    }
}
