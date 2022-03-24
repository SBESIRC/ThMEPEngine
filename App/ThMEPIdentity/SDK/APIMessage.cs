using RestSharp;

namespace ThAnalytics.SDK
{
    public static class APIMessage
    {
        public static THConfig m_Config = new THConfig()
        {
            Token = "Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJjaW0tYXV0by1hY2NvdW50QHRoYXAuY29tLmNuIiwic2NwIjoidXNlciIsImV4cCI6MTk2MzQ0OTY2MSwiYXVkIjoiVEhNRVAiLCJpYXQiOjE2NDgwODk2NjEsImp0aSI6ImNmNDNmODZjLTRiZDktNDU0YS05MWY5LWEzYmQxNjFiOWJhMSJ9.T5N97tnfeTnWuSaMSaLRNxD-2lGaFsOOYEXeP-PIQYs",
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
