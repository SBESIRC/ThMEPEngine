using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
namespace ThMEPWSS.BlockNameConfig
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await MainAsync(args[0]);
        }
        static async Task MainAsync(string picName)
        {
            using (var client = new HttpClient())
            {
                using (Stream fileStream = new FileStream(picName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    try
                    {
                        client.BaseAddress = new Uri("http://172.16.1.87:5000/upload");
                        var content = new MultipartFormDataContent();
                        content.Add(new StringContent("测试图片"), "name");
                        content.Add(new StreamContent(fileStream), "file", "a.zip");

                        client.Timeout = new TimeSpan(0, 0, 30, 0, 0);
                        var result = await client.PostAsync("http://172.16.1.87:5000/upload", content);

                        string resultContent = await result.Content.ReadAsStringAsync();
                        JsonFile.WriteJsonFile(picName +".csv", resultContent);
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }
        }
        static async Task MainAsync2(string picName)
        {
            using (var client = new HttpClient())
            {
                using (Stream fileStream = new FileStream(@"D:\THdetection\image\" + picName + ".jpg", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    try
                    {
                        client.BaseAddress = new Uri("http://172.16.1.87:5000/upload");

                        var content = new MultipartFormDataContent();
                        content.Add(new StringContent("测试图片"), "name");
                        content.Add(new StreamContent(fileStream), "file", "test.jpg");

                        client.Timeout = new TimeSpan(0, 0, 30, 0, 0);
                        var result = await client.PostAsync("http://172.16.1.87:5000/upload", content);

                        string resultContent = await result.Content.ReadAsStringAsync();
                        JsonFile.WriteJsonFile(@"D:\THdetection\label\" + picName + ".json", resultContent);
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }
        }
    }
}
