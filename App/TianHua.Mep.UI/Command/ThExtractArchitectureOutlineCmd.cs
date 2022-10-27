using System;
using System.Collections.Generic;
using ThMEPEngineCore.Command;
using TianHua.Mep.UI.UI;
using TianHua.Mep.UI.ViewModel;
using Autodesk.AutoCAD.ApplicationServices;
using acadApp = Autodesk.AutoCAD.ApplicationServices;

namespace TianHua.Mep.UI.Command
{
    public class ThExtractArchitectureOutlineCmd : ThMEPBaseCommand, IDisposable
    {
        private static ExtractArchitectureOutlineUI _uiExtractArchOutline;
        private static Dictionary<string, ThExtractArchitectureOutlineVM> _documentVMDic;
        public ThExtractArchitectureOutlineCmd()
        {
            CommandName = "";
            ActionName = "";
        }

        static ThExtractArchitectureOutlineCmd()
        {
            _documentVMDic = new Dictionary<string, ThExtractArchitectureOutlineVM>();
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
                var vm = GetArchitectureOutlineVM(ActiveDoc.Name);
                if (vm==null)
                {
                    vm = new ThExtractArchitectureOutlineVM();
                    AddToDocumentVMDic(ActiveDoc.Name, vm);
                }                
                ShowUI(vm);
            }
        }

        public void DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if(ActiveDoc !=null)
            {
                var vm = GetArchitectureOutlineVM(e.Document.Name);
                if (vm == null)
                {
                    vm = new ThExtractArchitectureOutlineVM();
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

        private ThExtractArchitectureOutlineVM GetArchitectureOutlineVM(string docName)
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

        private void AddToDocumentVMDic(string docName, ThExtractArchitectureOutlineVM vm)
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

        private void ShowUI(ThExtractArchitectureOutlineVM vm)
        {
            if (vm == null)
            {
                return;
            }
            if(_uiExtractArchOutline != null && _uiExtractArchOutline.IsLoaded)
            {
                _uiExtractArchOutline.UpdateDataContext(vm);
            }
            else
            {
                _uiExtractArchOutline = new ExtractArchitectureOutlineUI(vm);
                acadApp.Application.ShowModelessWindow(_uiExtractArchOutline);
            }
        }

        private void UpdateUI(ThExtractArchitectureOutlineVM vm)
        {
            if (_uiExtractArchOutline != null && _uiExtractArchOutline.IsLoaded)
            {
                _uiExtractArchOutline.UpdateDataContext(vm);
            }
        }

        private void CloseUI()
        {
            if (_uiExtractArchOutline != null && _uiExtractArchOutline.IsLoaded)
                _uiExtractArchOutline.Close();
        }
    }
}
