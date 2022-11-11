using ThPlatform3D.Model.Project;
using ThPlatform3D.Model.User;

namespace ThPlatform3D
{
    public class ConfigService
    {
        public static ConfigService ConfigInstance = new ConfigService();
        private ConfigService() 
        {
            LoginUser = null;
            InitBinding();
        }
        /// <summary>
        /// 登录的用户信息
        /// </summary>
        public UserInfo LoginUser { get; set; }
        /// <summary>
        /// 当前图纸绑定的专业
        /// </summary>
        public string BindingMajor { get;protected set; }
        /// <summary>
        /// 当前图纸绑定项目Id
        /// </summary>
        public string BindingPrjId { get; protected set; }
        /// <summary>
        /// 当前图纸绑定的项目名称
        /// </summary>
        public string BindingPrjName { get; protected set; }
        /// <summary>
        /// 当前图纸绑定的子项Id
        /// </summary>
        public string BindingSubPrjId { get; protected set; }
        /// <summary>
        /// 当前图纸绑定的子项名称
        /// </summary>
        public string BindingSbuPrjName { get; protected set; }
        /// <summary>
        /// 清除当前图纸绑定的数据（本地缓存非数据库数据）
        /// </summary>
        public void ClearBinding() 
        {
            InitBinding();
        }
        /// <summary>
        /// 当前图纸绑定的项目信息
        /// </summary>
        /// <param name="project"></param>
        /// <param name="subProject"></param>
        /// <param name="major"></param>
        public void BindingProjectMajor(DBProject project, DBSubProject subProject, string major) 
        {
            InitBinding();
            if (null == project || null == subProject || string.IsNullOrEmpty(major))
                return;
            BindingMajor = major;
            BindingPrjId = project.Id;
            BindingPrjName = project.PrjName;
            BindingSubPrjId = subProject.SubentryId;
            BindingSbuPrjName = subProject.SubEntryName;
        }
        /// <summary>
        /// 是否可以push数据
        /// </summary>
        /// <returns></returns>
        public bool CanPush() 
        {
            if (LoginUser == null || string.IsNullOrEmpty(LoginUser.PreSSOId))
                return false;
            if (string.IsNullOrEmpty(BindingPrjId))
                return false;
            return true;
        }
        /// <summary>
        /// push到Viewer的管道名称
        /// </summary>
        /// <returns></returns>
        public string ViewerPipeName() 
        {
            //暂时没有实现
            return "";
        }
        private void InitBinding() 
        {
            BindingMajor = string.Empty;
            BindingPrjId = string.Empty;
            BindingPrjName = string.Empty;
            BindingSubPrjId = string.Empty;
            BindingSbuPrjName = string.Empty;
        }
    }
}
