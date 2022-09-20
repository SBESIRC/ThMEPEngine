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
        private static Dictionary<Document, ThExtractRoomOutlineVM> _documentVMDic;
        public ThExtractRoomConfigCmd()
        {
        }

        static ThExtractRoomConfigCmd()
        {
            _documentVMDic = new Dictionary<Document, ThExtractRoomOutlineVM>();
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            if(acadApp.Application.DocumentManager.MdiActiveDocument!=null)
            {
                var activeDoc = acadApp.Application.DocumentManager.MdiActiveDocument;
                var vm = GetRoomOutlineVM(activeDoc);
                if (vm==null)
                {
                    vm = new ThExtractRoomOutlineVM();
                    AddToDocumentVMDic(activeDoc, vm);
                }                
                ShowUI(vm);
            }
        }

        public void DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if (acadApp.Application.DocumentManager.MdiActiveDocument != null)
            {
                var vm = GetRoomOutlineVM(e.Document);
                if (vm == null)
                {
                    vm = new ThExtractRoomOutlineVM();
                    AddToDocumentVMDic(e.Document, vm);
                }
                UpdateUI(vm);
            }
        }

        public void DocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            RemoveFromDocumentVMDic(e.Document);
        }

        public void DocumentDestroyed(object sender, DocumentDestroyedEventArgs e)
        {
            if(acadApp.Application.DocumentManager.MdiActiveDocument == null)
            {
                CloseUI();
            }            
        }

        private ThExtractRoomOutlineVM GetRoomOutlineVM(Document doc)
        {
            if(doc!=null && _documentVMDic.ContainsKey(doc))
            {
                return _documentVMDic[doc];
            }
            else
            {
                return null;
            }
        }

        private void AddToDocumentVMDic(Document doc, ThExtractRoomOutlineVM vm)
        {
            if(doc ==null || vm == null)
            {
                return;
            }
            if(_documentVMDic.ContainsKey(doc))
            {
                _documentVMDic[doc] = vm;
            }
            else
            {
                _documentVMDic.Add(doc, vm);
            }
        }

        private void RemoveFromDocumentVMDic(Document doc)
        {
            if (doc!=null && _documentVMDic.ContainsKey(doc))
            {
                _documentVMDic.Remove(doc);
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
