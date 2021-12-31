using RestSharp;

namespace ThAnalytics.SDK
{
    public static class APIMessage
    {
        public static THConfig m_Config = new THConfig()
        {
            Token = "Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJjaW0tYXV0by1hY2NvdW50QHRoYXAuY29tLmNuIiwic2NwIjoidXNlciIsImV4cCI6MTY0NjQ3NDIyMSwiYXVkIjoiVEhNRVAiLCJpYXQiOjE2MTQ5MzgyMjEsImp0aSI6ImRhNmNlNjAzLWIwMTgtNDRiNS1hMDIyLWUyMzAzZDgwOWFhMCJ9.qyRo50Gsm2AObh1CpvVz_7VxMMCoVx61b7nqWimjlao",
            AppVersion = "V1.0",
            ServerUrl = @"https://cybros.thape.com.cn",
            SSOUrl = @"https://sso.thape.com.cn/users/sign_in"
        };

        public static void CADSession(Sessions sessions)
        {
            var client = new RestClient(m_Config.ServerUrl);
            var request = new RestRequest("/api/cad_session", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("JWT-AUD", "THMEP");
            request.AddHeader("Authorization", m_Config.Token);
            request.AddJsonBody(sessions);
            client.ExecuteAsync(request);
        }

        public static void CADOperation(InitiConnection initiConnection)
        {
            var client = new RestClient(m_Config.ServerUrl);
            var request = new RestRequest("/api/cad_operation", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("JWT-AUD", "THMEP");
            request.AddHeader("Authorization", m_Config.Token);
            request.AddJsonBody(initiConnection);
            client.ExecuteAsync(request);
        }
    }
}
