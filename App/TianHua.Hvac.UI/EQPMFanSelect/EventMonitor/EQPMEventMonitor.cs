using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.Hvac.UI.EQPMFanSelect.EventMonitor
{
    class EQPMEventMonitor
    {
        private static string _customCmd = null;
        private static bool _runCustomCommand = false;
        private static ObjectId _selectedEntId = ObjectId.Null;
        private static ThFanSelectionDocumentEventHandler documentEvent = null;
        public static void Application_BeginDoubleClick(object sender, BeginDoubleClickEventArgs e)
        {
            _customCmd = null;
            _selectedEntId = ObjectId.Null;

            //Get entity which user double-clicked on
            PromptSelectionResult res = Active.Editor.SelectAtPickBox(e.Location);
            if (res.Status == PromptStatus.OK)
            {
                ObjectId[] ids = res.Value.GetObjectIds();

                //Only when there is one entity selected, we go ahead to see
                //if there is a custom command supposed to target at this entity
                if (ids.Length == 1)
                {
                    _selectedEntId = ids[0];
                    using (AcadDatabase acadDatabase = AcadDatabase.Use(_selectedEntId.Database))
                    {
                        var selectId = _selectedEntId.GetModelIdentifier(ThHvacCommon.RegAppName_FanSelectionEx);
                        if (string.IsNullOrEmpty(selectId))
                        {
                            _customCmd = "";
                        }
                        else 
                        {
                            _customCmd = "THFJEDITEX";
                            EQPMUIServices.Instance.SelectFanBlockGuid = selectId;
                        }
                    }
                    
                    //Find mapped custom command name
                    if (System.Convert.ToInt32(Application.GetSystemVariable("DBLCLKEDIT")) == 0)
                    {
                        //Since "Double click editing" is not enabled, we'll
                        //go ahead to launch our custom command
                        LaunchCustomCommand(Active.Editor);
                    }
                    else
                    {
                        //Since "Double Click Editing" is enabled, a command
                        //defined in CUI/CUIX will be fired. Let the code return
                        //and wait the DocumentLockModeChanged and
                        //DocumentLockModeChangeVetoed event handlers do their job
                        return;
                    }
                }
            }
        }
        public static void DocumentManager_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document == null)
            {
                EQPMUIServices.Instance.HideFanSelectUI();
                //return;
            }
            else 
            {
                var thisDwgId = e.Document.UnmanagedObject.ToString();
                EQPMUIServices.Instance.ShowFanSelectUI(thisDwgId);
            }
            SubscribeToDocumentEvents(e.Document);
        }
        public static void DocumentManager_DocumentLockModeChanged(object sender, DocumentLockModeChangedEventArgs e)
        {
            _runCustomCommand = false;
            if (!e.GlobalCommandName.StartsWith("#"))
            {
                // Lock状态，可以看做命令开始状态
                var cmdName = e.GlobalCommandName;

                // 过滤"EATTEDIT"命令
                if (!cmdName.ToUpper().Equals("EATTEDIT"))
                {
                    return;
                }

                if (!_selectedEntId.IsNull &&
                    !string.IsNullOrEmpty(_customCmd) &&
                    !cmdName.ToUpper().Equals(_customCmd.ToUpper()))
                {
                    e.Veto();
                    _runCustomCommand = true;
                }
            }
        }

        public static void DocumentManager_DocumentLockModeChangeVetoed(object sender, DocumentLockModeChangeVetoedEventArgs e)
        {
            if (_runCustomCommand)
            {
                //Start custom command
                LaunchCustomCommand(Active.Editor);
            }
        }
        public static void SubscribeToObjectOverrule()
        {
            ThFanModelOverruleManager.Instance.Register();
            
        }
        public static void UnSubscribeToObjectOverrule()
        {
            ThFanModelOverruleManager.Instance.UnRegister();
        }
        private static void LaunchCustomCommand(Editor ed)
        {
            //Create implied a selection set
            ed.SetImpliedSelection(new ObjectId[] { _selectedEntId });

            string cmd = _customCmd;

            _customCmd = null;
            _selectedEntId = ObjectId.Null;

            //Start the custom command which has UsePickSet flag set
            Active.Document.SendStringToExecute(string.Format("{0} ", cmd), true, false, false);
        }
        public static void SubscribeToDocumentEvents(Document document) 
        {
            UnSubscribeToDocumentEvents();
            if (null != document)
                documentEvent = new ThFanSelectionDocumentEventHandler(document);
        }
        public static void UnSubscribeToDocumentEvents()
        {
            if (documentEvent != null)
                documentEvent.Dispose();
        }
    }
}
