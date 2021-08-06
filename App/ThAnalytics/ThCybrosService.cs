using System;
using System.Collections.Generic;
using ThAnalytics.SDK;
using ThMEPIdentity;

namespace ThAnalytics
{
    public class ThCybrosService : IADPServices
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThCybrosService instance = new ThCybrosService();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThCybrosService() { }
        internal ThCybrosService() { }
        public static ThCybrosService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public void Initialize()
        {
            //
        }

        public void UnInitialize()
        {
            //
        }

        public void StartSession()
        {
            THRecordingService.SessionBegin();
        }

        public void EndSession()
        {
            THRecordingService.SessionEnd();
        }

        public void RecordTHCommandEvent(string cmdName, double duration)
        {
            // 非协同用户
            if (string.IsNullOrEmpty(ThAcsSystemService.Instance.UserId))
            {
                return;
            }

            // 非指定命令
            if (!ThMEPCmdService.Instance.IsTHCommand(cmdName))
            {
                return;
            }

            // 记录命令事件
            Segmentation thsegmentation = new Segmentation();
            thsegmentation.Add("名称", cmdName);
            thsegmentation.Add("功能", ThMEPCmdService.Instance.Description(cmdName));
            thsegmentation.Add("用户", ThAcsSystemService.Instance.UserId);
            thsegmentation.Add("项目", ThAcsSystemService.Instance.ProjectNumber);
            THRecordingService.RecordEvent("天华命令使用", (int)duration, thsegmentation);
        }

        public void RecordSysVerEvent(string sysverName, string sysverValue)
        {
            Segmentation segmentation = new Segmentation();
            segmentation.Add("名称", sysverName);
            segmentation.Add("值", sysverValue);
            THRecordingService.RecordEvent("CAD系统变量", 0, segmentation);
        }
    }
}
