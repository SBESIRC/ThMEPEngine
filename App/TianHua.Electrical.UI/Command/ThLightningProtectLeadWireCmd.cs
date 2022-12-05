using System;
using System.Collections.Generic;
using ThMEPEngineCore.Command;
using Autodesk.AutoCAD.ApplicationServices;
using acadApp = Autodesk.AutoCAD.ApplicationServices;
using TianHua.Electrical.UI.LightningProtectLeadWire;

namespace TianHua.Electrical.UI.Command
{
    public class ThLightningProtectLeadWireCmd : ThMEPBaseCommand, IDisposable
    {
        private static LightningProtectLeadWireUI _uiLightningProtectLeadWire;
        private static Dictionary<string, ThLightningProtectLeadWireVM> _documentVMDic;
        public ThLightningProtectLeadWireCmd()
        {
        }

        static ThLightningProtectLeadWireCmd()
        {
            _documentVMDic = new Dictionary<string, ThLightningProtectLeadWireVM>();
        }

        public void Dispose()
        {
            //
        }

        private static Document ActiveDoc => acadApp.Application.DocumentManager.MdiActiveDocument;

        public override void SubExecute()
        {
            if(ActiveDoc != null)
            {
                var vm = GetViewModel(ActiveDoc.Name);
                if (vm==null)
                {
                    vm = new ThLightningProtectLeadWireVM();
                    AddToDocumentVMDic(ActiveDoc.Name, vm);
                }                
                ShowUI(vm);
            }
        }

        public static void DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if(ActiveDoc !=null)
            {
                var vm = GetViewModel(e.Document.Name);
                if (vm == null)
                {
                    vm = new ThLightningProtectLeadWireVM();
                    AddToDocumentVMDic(e.Document.Name, vm);
                }
                UpdateUI(vm);
            }            
        }

        public static void DocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            if(e.Document!=null)
            {
                RemoveFromDocumentVMDic(e.Document.Name);
            }            
        }

        public static void DocumentDestroyed(object sender, DocumentDestroyedEventArgs e)
        {
            if(acadApp.Application.DocumentManager.MdiActiveDocument == null)
            {
                CloseUI();
            }            
        }

        private static ThLightningProtectLeadWireVM GetViewModel(string docName)
        {
            if(!string.IsNullOrEmpty(docName) && _documentVMDic.ContainsKey(docName))
            {
                return _documentVMDic[docName];
            }
            else
            {
                return null;
            }
        }

        private static void AddToDocumentVMDic(string docName, ThLightningProtectLeadWireVM vm)
        {
            if(string.IsNullOrEmpty(docName) || vm == null)
            {
                return;
            }
            if(_documentVMDic.ContainsKey(docName))
            {
                _documentVMDic[docName] = vm;
            }
            else
            {
                _documentVMDic.Add(docName, vm);
            }
        }

        private static void RemoveFromDocumentVMDic(string docName)
        {
            if (!string.IsNullOrEmpty(docName) && _documentVMDic.ContainsKey(docName))
            {
                _documentVMDic.Remove(docName);
            }
        }

        private void ShowUI(ThLightningProtectLeadWireVM vm)
        {
            if (vm == null)
            {
                return;
            }
            if(_uiLightningProtectLeadWire != null && _uiLightningProtectLeadWire.IsLoaded)
            {
                _uiLightningProtectLeadWire.UpdateDataContext(vm);
            }
            else
            {
                _uiLightningProtectLeadWire = new LightningProtectLeadWireUI(vm);
                acadApp.Application.ShowModelessWindow(_uiLightningProtectLeadWire);
            }
        }

        private static void UpdateUI(ThLightningProtectLeadWireVM vm)
        {
            if (_uiLightningProtectLeadWire != null && _uiLightningProtectLeadWire.IsLoaded)
            {
                _uiLightningProtectLeadWire.UpdateDataContext(vm);
            }
        }

        private static void CloseUI()
        {
            if (_uiLightningProtectLeadWire != null && _uiLightningProtectLeadWire.IsLoaded)
                _uiLightningProtectLeadWire.Close();
        }
    }
}
