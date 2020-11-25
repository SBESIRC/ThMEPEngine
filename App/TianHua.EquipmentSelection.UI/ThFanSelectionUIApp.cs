using System;
using AcHelper;
using Linq2Acad;
using System.IO;
using ThCADExtension;
using Autodesk.AutoCAD.Runtime;
using TianHua.FanSelection.UI.CAD;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using ThMEPHAVC.CAD;

namespace TianHua.FanSelection.UI
{
    public class ThFanSelectionUIApp : IExtensionApplication
    {
        private static string _customCmd = null;
        private static bool _runCustomCommand = false;
        private static ObjectId _selectedEntId = ObjectId.Null;
        private static CustomCommandMappers _customCommands = null;
        private static ThFanSelectionDocumentEventHandler documentEventHandler = null;

        public void Initialize()
        {
            SubscribeToOverrules();
            AddDoubleClickHandler();
            SubscribeToDocumentManagerEvents();
            if (Active.Document != null)
            {
                SubscribeToDocumentEvents(Active.Document);
            }
        }

        public void Terminate()
        {
            UnsubscribeToOverrules();
            RemoveDoubleClickHandler();
            UnSubscribeToDocumentManagerEvents();
        }

        [CommandMethod("TIANHUACAD", "THFJDW", CommandFlags.Modal)]
        public void DrawFanModels()
        {
            //轴流单速
            string axialPath = Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.AXIAL_Parameters);
            //轴流双速
            string axialDoublePath = Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.AXIAL_Parameters_Double);
            //离心前倾双速
            string htfcFrontDoublePath = Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.HTFC_Parameters_Double);
            //离心前倾单速
            string htfcFrontSinglePath = Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.HTFC_Parameters);
            //离心后倾单速
            string htfcBackSinglePath = Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.HTFC_Parameters_Single);

            var axialModel = new DrawAxialModelsInCAD(axialPath);
            var axialDoubleModel = new DrawAxialModelsInCAD(axialDoublePath);
            var htfcFrontDouble = new DrawHtfcModelsInCAD(htfcFrontDoublePath);
            var htfcFrontSingle = new DrawHtfcModelsInCAD(htfcFrontSinglePath);
            var htfcBackSingle = new DrawHtfcModelsInCAD(htfcBackSinglePath);

            axialModel.DrawInCAD();
            axialDoubleModel.DrawInCAD();
            htfcFrontDouble.DrawInCAD();
            htfcFrontSingle.DrawInCAD();
            htfcBackSingle.DrawInCAD();

        }

        [CommandMethod("TIANHUACAD", "THFJ", CommandFlags.Modal)]
        public void ThEquipmentSelection()
        {
            var dwgName = Convert.ToInt32(AcadApp.GetSystemVariable("DWGTITLED"));
            if (dwgName == 0)
            {
                AcadApp.ShowAlertDialog("请先保存当前图纸!");
                return;
            }
            Active.Document.CreateModelSelectionDialog();
            Active.Document.ShowModelSelectionDialog();
        }

        [CommandMethod("TIANHUACAD", "THFJZH", CommandFlags.Modal)]
        public void ThEquipmentConvert()
        {
            fmConvert _fmConvert = new fmConvert();
            AcadApp.ShowModelessDialog(_fmConvert);
        }


        [CommandMethod("TIANHUACAD", "THFJEDIT", CommandFlags.UsePickSet)]
        public void ThEquipmentEdit()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ObjectId entId = GetSelectedEntity();
                if (!entId.IsNull)
                {
                    Active.Document.ShowModelSelectionDialog();
                    Active.Document.Form().ShowFormByID(entId.GetModelIdentifier());
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THFJBLOCK", CommandFlags.NoHistory)]
        public void ThEquipmentBlock()
        {
            using (var cmd = new ThModelBlockCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFJINPLACEEDITBLOCK", CommandFlags.NoHistory)]
        public void ThEquipmentInPlaceEditBlock()
        {
            using (var cmd = new ThModelInPlaceEditBlockCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFJUIUPDATE", CommandFlags.NoUndoMarker)]
        public void ThEquipmentUiUpdate()
        {
            using (var cmd = new ThModelUiUpdateCommand())
            {
                cmd.Execute();
            }
        }

        private ObjectId GetSelectedEntity()
        {
            PromptSelectionResult res = Active.Editor.GetSelection();
            if (res.Status == PromptStatus.OK)
            {
                return res.Value.GetObjectIds()[0];
            }
            else
            {
                return ObjectId.Null;
            }
        }

        private static void AddDoubleClickHandler()
        {
            AcadApp.BeginDoubleClick += Application_BeginDoubleClick;
            _customCommands = CustomCommandsFactory.CreateDefaultCustomCommandMappers();
        }

        private static void RemoveDoubleClickHandler()
        {
            AcadApp.BeginDoubleClick -= Application_BeginDoubleClick;
        }

        private static void SubscribeToDocumentManagerEvents()
        {
            AcadApp.DocumentManager.DocumentCreated += DocumentManager_DocumentCreated;
            AcadApp.DocumentManager.DocumentActivated += DocumentManager_DocumentActivated;
            AcadApp.DocumentManager.DocumentLockModeChanged += DocumentManager_DocumentLockModeChanged;
            AcadApp.DocumentManager.DocumentLockModeChangeVetoed += DocumentManager_DocumentLockModeChangeVetoed;
            AcadApp.DocumentManager.DocumentToBeDeactivated += DocumentManager_DocumentToBeDeactivated;
            AcadApp.DocumentManager.DocumentToBeDestroyed += DocumentManager_DocumentToBeDestroyed;
        }

        private static void UnSubscribeToDocumentManagerEvents()
        {
            AcadApp.DocumentManager.DocumentCreated -= DocumentManager_DocumentCreated;
            AcadApp.DocumentManager.DocumentActivated -= DocumentManager_DocumentActivated;
            AcadApp.DocumentManager.DocumentLockModeChanged -= DocumentManager_DocumentLockModeChanged;
            AcadApp.DocumentManager.DocumentLockModeChangeVetoed -= DocumentManager_DocumentLockModeChangeVetoed;
            AcadApp.DocumentManager.DocumentToBeDeactivated -= DocumentManager_DocumentToBeDeactivated;
            AcadApp.DocumentManager.DocumentToBeDestroyed -= DocumentManager_DocumentToBeDestroyed;
        }

        private static void SubscribeToDocumentEvents(Document document)
        {
            documentEventHandler = new ThFanSelectionDocumentEventHandler(document);
        }

        private static void UnSubscribeToDocumentEvents(Document document)
        {
            documentEventHandler.Dispose();
        }

        private static void SubscribeToOverrules()
        {
            ThFanModelOverruleManager.Instance.Register();
        }

        private static void UnsubscribeToOverrules()
        {
            ThFanModelOverruleManager.Instance.UnRegister();
        }

        private static void DocumentManager_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document != null)
            {
                if (e.Document.PopModelSelectionDialogVisible())
                {
                    e.Document.ShowModelSelectionDialog();
                }
                else
                {
                    e.Document.HideModelSelectionDialog();
                }
            }
        }

        private static void DocumentManager_DocumentToBeDeactivated(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document != null)
            {
                e.Document.PushModelSelectionDialogVisible();
                e.Document.HideModelSelectionDialog();
            }
        }

        private static void DocumentManager_DocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document != null)
            {
                // 订阅Document事件
                SubscribeToDocumentEvents(e.Document);
            }
        }

        private static void DocumentManager_DocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document != null)
            {
                e.Document.CloseModelSelectionDialog();

                // 取消订阅Docuemnt事件
                UnSubscribeToDocumentEvents(e.Document);
            }
        }

        private static void Application_BeginDoubleClick(object sender, BeginDoubleClickEventArgs e)
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
                    //Find mapped custom command name
                    string cmd = _customCommands.GetCustomCommand(ids[0]);
                    if (!string.IsNullOrEmpty(cmd))
                    {
                        _selectedEntId = ids[0];
                        _customCmd = cmd;

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
        }

        private static void DocumentManager_DocumentLockModeChanged(object sender, DocumentLockModeChangedEventArgs e)
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

        private static void DocumentManager_DocumentLockModeChangeVetoed(object sender, DocumentLockModeChangeVetoedEventArgs e)
        {
            if (_runCustomCommand)
            {
                //Start custom command
                LaunchCustomCommand(Active.Editor);
            }
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
    }
}