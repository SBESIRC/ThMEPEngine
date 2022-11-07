using RestSharp;
using System;
using System.Threading.Tasks;

namespace ThMEPWSS.BlockNameConfig
{
    public class Program
    {
        public static async Task Run(string[] args)
        {
            await MainAsync(args[0]);
        }

        static async Task MainAsync(string picName)
        {
            var request = new RestRequest(Method.POST);
            request.AddParameter("测试图片", "name");
            request.AddFile("file", picName);
            var restClient = new RestClient { BaseUrl = new Uri("http://172.16.1.87:5000/upload") };
            var response = await restClient.ExecuteAsync(request);
            JsonFile.WriteJsonFile(picName + ".csv", response.Content);
        }
    }
}
