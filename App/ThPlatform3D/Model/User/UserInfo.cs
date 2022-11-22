using Newtonsoft.Json;
using System.Collections.Generic;

namespace ThPlatform3D.Model.User
{
    public class UserInfo
    {
        /// <summary>
        /// 用户登录成功的基本信息
        /// </summary>
        [JsonIgnore]
        public UserLoginRes UserLogin { get; set; }
        /// <summary>
        /// 用户职位名称
        /// </summary>
        [JsonProperty("position_title")]
        public string PositionTitle { get; set; }
        [JsonProperty("clerk_code")]
        public string ClerkCode { get; set; }
        /// <summary>
        /// 用户中文名称
        /// </summary>
        [JsonProperty("chinese_name")]
        public string ChineseName { get; set; }
        /// <summary>
        /// 用户协同Id
        /// </summary>
        [JsonProperty("pre_sso_id")]
        public string PreSSOId { get; set; }
        /// <summary>
        /// 用户部门信息
        /// </summary>
        public List<Department> Departments { get; set; }

    }
    public class UserLoginRes
    {
        /// <summary>
        /// 用户名（域账号名称）
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// 用户Id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 座机号
        /// </summary>
        [JsonProperty("desk_phone")]
        public string DeskPhone { get; set; }
        public string Email { get; set; }
        /// <summary>
        /// 用户登录认证后的Token
        /// </summary>
        [JsonProperty("jwt_token")]
        public string Token { get; set; }
    }

    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonProperty("company_name")]
        public string CompanyName { get; set; }
    }
}
