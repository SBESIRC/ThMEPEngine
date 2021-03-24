using System;
using System.Net.Mail;
using ThIdentity.SDK;

namespace ThIdentity
{
    public class ThCybrosUserProfile : IThUserProfile, IDisposable
    {
        private UserDetails UserProfile { get; set; }

        public ThCybrosUserProfile()
        {
            //if (ThIdentityService.IsLogged())
            //{
            //    UserProfile = ThIdentityService.UserProfile;
            //}
        }

        public void Dispose()
        {
            UserProfile = null;
        }

        public string Name
        {
            get
            {
                return UserProfile.chinese_name;
            }
        }

        public string Title
        {
            get
            {
                return UserProfile.position_title;
            }
        }

        public string Company
        {
            get
            {
                return UserProfile.departments[0].company_name;
            }
        }

        public string Department
        {
            get
            {
                return UserProfile.departments[0].name;
            }
        }

        public string Mail
        {
            get
            {
                return UserProfile.email;
            }
        }

        public string Accountname
        {
            get
            {
                var address = new MailAddress(Mail);
                return address.User;
            }
        }

        public bool IsDomainUser()
        {
            return UserProfile != null;
        }
    }
}
