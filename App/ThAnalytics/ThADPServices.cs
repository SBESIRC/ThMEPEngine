using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThAnalytics
{
    public interface IADPServices
    {
        // 初始化
        void Initialize();

        // 反初始化
        void UnInitialize();

        // 开启会话
        void StartSession();

        // 结束会话
        void EndSession();

        // 记录CAD命令事件
        void RecordCommandEvent(string GlobalCommandName, double duration);

        //记录CAD系统变量事件
        void RecordSysVerEvent(string sysverName, string sysValue);

        //记录天华命令事件
        void RecordTHCommandEvent(string cmdName, double duration);
    }
}
