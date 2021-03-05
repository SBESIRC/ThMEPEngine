using System;
using System.Collections.Generic;
using ThAnalytics.SDK;

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

        private readonly Dictionary<string, string> thcommanfunctiondict = new Dictionary<string, string>
        {
            //
        };

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

        public void RecordCommandEvent(string cmdName, double duration)
        {
            Segmentation segmentation = new Segmentation();
            segmentation.Add("名称", cmdName);
            if (thcommanfunctiondict.ContainsKey(cmdName))
            {
                segmentation.Add("功能", thcommanfunctiondict[cmdName]);
            }
            THRecordingService.RecordEvent("CAD命令使用", (int)duration, segmentation);
        }

        public void RecordTHCommandEvent(string cmdName, double duration)
        {
            if (ThAcsSystemService.Instance.UserId == null ||
                ThAcsSystemService.Instance.ProjectNumber == null)
            {
                return;
            }

            if (thcommanfunctiondict.ContainsKey(cmdName))
            {
                Segmentation thsegmentation = new Segmentation();
                thsegmentation.Add("名称", cmdName);
                thsegmentation.Add("功能", thcommanfunctiondict[cmdName]);
                thsegmentation.Add("用户", ThAcsSystemService.Instance.UserId);
                thsegmentation.Add("项目", ThAcsSystemService.Instance.ProjectNumber);
                THRecordingService.RecordEvent("天华命令使用", (int)duration, thsegmentation);
            }
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
