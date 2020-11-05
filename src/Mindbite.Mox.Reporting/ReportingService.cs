using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Mindbite.Mox.Reporting.Services
{

    public class ReportingService : IDisposable
    {
        public class Report
        {
            public string UID { get; set; }
            public string Name { get; set; }
            public string ReportPath { get; set; }
            public bool ShowInList { get; set; }
        }

        private readonly MoxReportingOptions _options;
        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReportingService(IOptions<MoxReportingOptions> options, IHttpContextAccessor httpContextAccessor)
        {
            this._options = options.Value;
            this._httpContextAccessor = httpContextAccessor;

            this._client = new HttpClient
            {
                BaseAddress = this._options.ServerUrl
            };
        }

        public void Dispose()
        {
            this._client.Dispose();
        }

        public async Task<IEnumerable<Report>> AllReportsAsync()
        {
            var response = await this._client.GetAsync($"/webapi/v1/reports/{this._options.ReportDirectory}/all?sharedSecret={this._options.SharedSecret}");
            response.EnsureSuccessStatusCode();
             
            return JsonConvert.DeserializeObject<IEnumerable<Report>>(await response.Content.ReadAsStringAsync()) ?? Enumerable.Empty<Report>();
        }

        public async Task<byte[]?> GeneratePDFReportAsync(string reportUID, params object[] parameters)
        {
            var dataDict = new Dictionary<string, object>
            {
                { "source", _options.ReportDirectory },
                { "uid", reportUID }
            };

            for(var i = 0; i < (parameters?.Length ?? 0); i++)
            {
                dataDict[$"P{i + 1}"] = parameters[i];
            }

            var data = new StringContent(JsonConvert.SerializeObject(dataDict), Encoding.UTF8, "application/json");
            var response = await this._client.PostAsync($"/webapi/v1/reports/export/pdf?sharedSecret={this._options.SharedSecret}", data);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}
