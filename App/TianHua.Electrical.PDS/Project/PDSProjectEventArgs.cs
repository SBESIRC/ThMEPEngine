using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project
{
    public class PDSProjectEventArgs : EventArgs
    {
        /// <summary>
        /// 事件附带消息
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// 触发函数名称
        /// </summary>
        public string ApiName { get; set; }
    }

    /// <summary>
    /// 项目数据变动事件参数
    /// </summary>
    public class ProjectDataChangedEventArgs : PDSProjectEventArgs
    {
        /// <summary>
        /// 项目数据变动事件参数
        /// </summary>
        /// <param name="apiName">API名称</param>
        /// <param name="message">信息</param>
        public ProjectDataChangedEventArgs(string apiName, string message)
        {
            this.ApiName = apiName;
            this.Message = message;
        }
    }

    /// <summary>
    /// 全局配置参数变动事件参数
    /// </summary>
    public class ProjectGlobalConfigurationChangedEventArgs : PDSProjectEventArgs
    {
        /// <summary>
        /// 全局配置参数变动事件参数
        /// </summary>
        /// <param name="apiName">API名称</param>
        /// <param name="message">信息</param>
        public ProjectGlobalConfigurationChangedEventArgs(string apiName, string message)
        {
            this.ApiName = apiName;
            this.Message = message;
        }
    }
}
