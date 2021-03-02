using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThIdentity.SDK
{
    public class DepartmentsItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 天华集团-AI研究中心
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 上海天华建筑设计有限公司
        /// </summary>
        public string company_name { get; set; }
    }

    public class UserDetails
    {
        /// <summary>
        /// 
        /// </summary>
        public string email { get; set; }
        /// <summary>
        ///  
        /// </summary>
        public string position_title { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string clerk_code { get; set; }
        /// <summary>
        ///  
        /// </summary>
        public string chinese_name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string desk_phone { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<DepartmentsItem> departments { get; set; }
    }
}
