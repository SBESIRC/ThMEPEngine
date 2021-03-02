using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThAnalytics.SDK
{
    public class User
    {
        /// <summary>
        /// 
        /// </summary>
        public string username { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string password { get; set; }
    }

    public class SignIn
    {
        /// <summary>
        /// 
        /// </summary>
        public User user { get; set; }
    }
}
