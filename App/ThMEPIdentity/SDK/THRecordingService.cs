using System;

namespace ThAnalytics.SDK
{
    public class THRecordingService
    {
        public static string m_Guid = Guid.NewGuid().ToString();

        public static void SessionBegin()
        {
            APIMessage.CADSession(new Sessions()
            {
                session = m_Guid,
                operation = "Begin",
                ip_address = FuncMac.GetIpAddress(),
                mac_address = FuncMac.GetNetCardMacAddress(),
            });
        }

        public static void SessionEnd()
        {
            APIMessage.CADSession(new Sessions()
            {
                session = m_Guid,
                operation = "End",
                ip_address = FuncMac.GetIpAddress(),
                mac_address = FuncMac.GetNetCardMacAddress(),
            });
        }

        public static void RecordEvent(string CmdName, int Duration, Segmentation _Segmentation)
        {
            APIMessage.CADOperation(new InitiConnection()
            {
                cmd_name = CmdName,
                cmd_seconds = Duration,
                session_id = m_Guid,
                cmd_data = _Segmentation
            });
        }
    }
}
