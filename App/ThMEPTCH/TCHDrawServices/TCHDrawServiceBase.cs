using AcHelper;
using System.Collections.Generic;
using System.IO;
using ThCADExtension;
using ThMEPTCH.Data;
using ThMEPTCH.Data.IO;

namespace ThMEPTCH.TCHDrawServices
{
    public abstract class TCHDrawServiceBase
    {
        public string TCHDBPath;
        public string TCHTemplateDBPath;
        protected ThSQLiteHelper DBHelper;
        protected List<string> ClearDataTables;
        protected abstract string CmdName { get; }
        public TCHDrawServiceBase()
        {
            DBHelper = null;
            TCHTemplateDBPath = ThCADCommon.TCHWSSDBPath();
            ClearDataTables = new List<string>();
        }
        protected virtual void InitTCHDatabase()
        {
            if (string.IsNullOrEmpty(TCHDBPath) || string.IsNullOrEmpty(TCHTemplateDBPath))
                return;
            if (!File.Exists(TCHTemplateDBPath))
                return;
            if (File.Exists(TCHDBPath))
                File.Delete(TCHDBPath);
            File.Copy(TCHTemplateDBPath, TCHDBPath);
            DBHelper = new ThSQLiteHelper(TCHDBPath);
        }
        public virtual void DrawExecute(bool sendImpTCHCmd =true)
        {
            InitTCHDatabase();
            OpenDBConnect();
            ClearDBTableHistoricalData();
            WriteModelToTCHDatabase();
            CloseDBConnect();
            if (sendImpTCHCmd) 
            {
                DrawTCHDatabaseToCAD();
                DeleteDBFile();
            }
        }
        protected virtual void ClearDBTableHistoricalData() 
        {
            if (null == ClearDataTables || ClearDataTables.Count < 1)
                return;
            foreach (var item in ClearDataTables) 
            {
                if (string.IsNullOrEmpty(item))
                    continue;
                DBHelper.ClearTable(item);
            }
        }
        protected void WriteModelToTCH(object tchTableModel,string tableName,ref ulong dataId) 
        {
            var pointSqlStr = ThSQLHelper.TabelModelToSqlString(tableName, tchTableModel);
            DBHelper.Execute(pointSqlStr);
            dataId = dataId + 1;
        }
        protected void OpenDBConnect()
        {
            if (null == DBHelper)
                return;
            DBHelper.Conn();
        }
        protected void CloseDBConnect(bool delHelper = false)
        {
            if (null == DBHelper)
                return;
            DBHelper.CloseConnect(delHelper);
            if (delHelper)
                DBHelper = null;
        }
        protected void DeleteDBFile()
        {
            if (string.IsNullOrEmpty(TCHDBPath) || !File.Exists(TCHDBPath))
                return;
            if (File.Exists(TCHDBPath))
                File.Delete(TCHDBPath);
        }
        protected abstract void WriteModelToTCHDatabase();
        protected virtual void DrawTCHDatabaseToCAD()
        {
            if (string.IsNullOrEmpty(CmdName))
                return;
#if ACAD_ABOVE_2014
            Active.Editor.Command(CmdName, TCHDBPath, " ");
#else
            ResultBuffer args = new ResultBuffer(
               new TypedValue((int)LispDataType.Text, string.Format("_.{0}", CmdName)),
               new TypedValue((int)LispDataType.Text, TCHDBPath),
               new TypedValue((int)LispDataType.Text, " "));
            Active.Editor.AcedCmd(args);
#endif
        }

    }
}
