using System;
using System.Collections.Generic;
using ThMEPEngineCore.Command;
using TianHua.Mep.UI.UI;
using TianHua.Mep.UI.ViewModel;
using Autodesk.AutoCAD.ApplicationServices;
using acadApp = Autodesk.AutoCAD.ApplicationServices;

namespace TianHua.Mep.UI.Command
{
    public class ThExtractRoomConfigCmd : ThMEPBaseCommand, IDisposable
    {
        private static ExtractRoomOutlineUI _uiExtractRoomOutline;
        private static Dictionary<string, ThExtractRoomOutlineVM> _documentVMDic;
        public ThExtractRoomConfigCmd()
        {
        }

        static ThExtractRoomConfigCmd()
        {
            _documentVMDic = new Dictionary<string, ThExtractRoomOutlineVM>();
        }

        public void Dispose()
        {
            //
        }

        private Document ActiveDoc => acadApp.Application.DocumentManager.MdiActiveDocument;

        public override void SubExecute()
        {
            if(ActiveDoc != null)
            {
                var vm = GetRoomOutlineVM(ActiveDoc.Name);
                if (vm==null)
                {
                    vm = new ThExtractRoomOutlineVM();
                    AddToDocumentVMDic(ActiveDoc.Name, vm);
                }                
                ShowUI(vm);
            }
        }

        public void DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if(ActiveDoc !=null)
            {
                var vm = GetRoomOutlineVM(e.Document.Name);
                if (vm == null)
                {
                    vm = new ThExtractRoomOutlineVM();
                    AddToDocumentVMDic(e.Document.Name, vm);
                }
                UpdateUI(vm);
            }            
        }

        public void DocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            if(e.Document!=null)
            {
                RemoveFromDocumentVMDic(e.Document.Name);
            }            
        }

        public void DocumentDestroyed(object sender, DocumentDestroyedEventArgs e)
        {
            if(acadApp.Application.DocumentManager.MdiActiveDocument == null)
            {
                CloseUI();
            }            
        }

        private ThExtractRoomOutlineVM GetRoomOutlineVM(string docName)
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

        private void AddToDocumentVMDic(string docName, ThExtractRoomOutlineVM vm)
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

        private void RemoveFromDocumentVMDic(string docName)
        {
            if (!string.IsNullOrEmpty(docName) && _documentVMDic.ContainsKey(docName))
            {
                _documentVMDic.Remove(docName);
            }
        }

        private void ShowUI(ThExtractRoomOutlineVM vm)
        {
            if (vm == null)
            {
                return;
            }
            if(_uiExtractRoomOutline != null && _uiExtractRoomOutline.IsLoaded)
            {
                _uiExtractRoomOutline.UpdateDataContext(vm);
            }
            else
            {
                _uiExtractRoomOutline = new ExtractRoomOutlineUI(vm);
                acadApp.Application.ShowModelessWindow(_uiExtractRoomOutline);
            }
        }

        private void UpdateUI(ThExtractRoomOutlineVM vm)
        {
            if (_uiExtractRoomOutline != null && _uiExtractRoomOutline.IsLoaded)
            {
                _uiExtractRoomOutline.UpdateDataContext(vm);
            }
        }

        private void CloseUI()
        {
            if (_uiExtractRoomOutline != null && _uiExtractRoomOutline.IsLoaded)
                _uiExtractRoomOutline.Close();
        }
    }
}
