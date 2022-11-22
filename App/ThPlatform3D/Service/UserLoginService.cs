using RestSharp;
using System;
using ThMEPEngineCore.IO.JSON;
using ThPlatform3D.Model.User;

namespace ThPlatform3D.Service
{
    public class UserLoginService
    {
        string loginUrl = "https://sso.thape.com.cn/users/sign_in";
        string infoUrl = "https://cybros.thape.com.cn/api/me";
        string JWTAUD = "";
        public UserLoginService(string jwtaud)
        {
            JWTAUD = jwtaud;
        }
        public UserInfo UserLoginByNamePsw(string uName, string uPsw)
        {
            var loginRes = UserLogin(uName, uPsw);
            if (string.IsNullOrEmpty(loginRes))
            {
                throw new Exception("用户登录失败，请检查用户名密码");
            }
            UserLoginRes userLogin;
            try
            {
                userLogin = JsonHelper.DeserializeJsonToObject<UserLoginRes>(loginRes);
            }
            catch (Exception ex)
            {
                throw new Exception("用户登录失败，请检查用户名密码");
            }
            if (userLogin == null || string.IsNullOrEmpty(userLogin.Token))
                throw new Exception("用户登录失败，请检查用户名密码");
            var userInfoRes = UserInfo(userLogin.Token);
            if (string.IsNullOrEmpty(userInfoRes))
            {
                throw new Exception("用户登录失败，请检查用户名密码");
            }
            UserInfo userInfo;
            try
            {
                userInfo = JsonHelper.DeserializeJsonToObject<UserInfo>(userInfoRes);
            }
            catch (Exception ex)
            {
                throw new Exception("用户登录失败，请检查用户名密码");
            }
            userInfo.UserLogin = userLogin;
            return userInfo;
        }
        private string UserLogin(string uName, string uPsw)
        {
            string body = "{\"user\":{\"username\":\"" + uName + "\",\"password\":\"" + uPsw + "\"}}";
            var client = new RestClient();
            var request = new RestRequest(new Uri(loginUrl, UriKind.RelativeOrAbsolute), Method.POST);
            request.AddHeader("JWT-AUD", JWTAUD);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(body);
            var response = client.Execute(request);
            return response.Content;
        }
        private string UserInfo(string token)
        {
            var client = new RestClient();
            var request = new RestRequest(new Uri(infoUrl, UriKind.RelativeOrAbsolute), Method.OPTIONS);
            request.AddHeader("JWT-AUD", JWTAUD);
            request.AddHeader("Authorization", string.Format("Bearer {0}", token));
            request.RequestFormat = DataFormat.Json;
            var response = client.Execute(request);
            return response.Content;
        }
    }
}
