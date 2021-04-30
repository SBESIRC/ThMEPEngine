using System;
using RestSharp;
using THMEPCore3D.Model;
using Newtonsoft.Json.Linq;
using THMEPCore3D.Interface;
using System.Collections.Generic;
using THMEPCore3D.Service;

namespace THMEPCore3D.Engine
{
    public class ThRoomQueryEngine : IQuery,IDisposable
    {
        public List<Project> Results { get; set; }
        public ThRoomQueryEngine()
        {
            Results = new List<Project>();
        }
        public void Dispose()
        {
            //
        }

        public void Query(List<ThModelCode> codes)
        {
            codes.ForEach(o => GetData(o));
        }
        private void GetData(ThModelCode modelCode)
        {

            var client = new RestClient("http://212.64.94.58:8002");
            var request = new RestRequest("/Aud/Api/AudSpaceJsonController/Query", Method.GET);
            request.AddQueryParameter("modelId", modelCode.ModelId);
            request.AddQueryParameter("modelSubentryId", modelCode.ModelSubEntryId);
            client.ExecuteAsync(request, response => {
                if(!string.IsNullOrEmpty(response.Content))
                {
                    var roomParse = new ThRoomJsonParseService();
                    Results.Add(roomParse.Parse(response.Content));
                }                          
            });
        }
    }
}
