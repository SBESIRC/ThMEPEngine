﻿using System;
using System.Net;
using System.Linq;
using RestSharp;

namespace ThIdentity.SDK
{
    public static class APIMessage
    {
        public static THConfig m_Config = new THConfig()
        {
            Token = "",
            AppVersion = "V1.0",
            ServerUrl = @"https://cybros.thape.com.cn",
            SSOUrl = @"https://sso.thape.com.cn/users/sign_in"
        };

        public static void CADSession(Sessions sessions)
        {
            var client = new RestClient(m_Config.ServerUrl);
            var request = new RestRequest("/api/cad_session", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("JWT-AUD", "CAD");
            request.AddHeader("Authorization", m_Config.Token);
            request.AddJsonBody(sessions);
            client.ExecuteAsync(request, response => {
                //Console.WriteLine(response.Content);
            });
        }

        public static void CADOperation(InitiConnection initiConnection)
        {
            var client = new RestClient(m_Config.ServerUrl);
            var request = new RestRequest("/api/cad_operation", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("JWT-AUD", "CAD");
            request.AddHeader("Authorization", m_Config.Token);
            request.AddJsonBody(initiConnection);
            client.ExecuteAsync(request, response => {
                //Console.WriteLine(response.Content);
            });
        }
    }
}
