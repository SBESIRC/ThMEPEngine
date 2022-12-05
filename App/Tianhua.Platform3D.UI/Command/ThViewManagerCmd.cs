using System;
using System.Collections.Generic;
using AcHelper.Commands;
using Autodesk.AutoCAD.ApplicationServices;
using acadApp = Autodesk.AutoCAD.ApplicationServices;
using Tianhua.Platform3D.UI.UI;
using Tianhua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI.Command
{
    public class ThViewManagerCmd :IAcadCommand, IDisposable
    {
        private static ViewManagerUI _uiViewManager;
        private static Dictionary<string, ViewManagerVM> _documentVMDic;
        static ThViewManagerCmd()
        {
            _documentVMDic = new Dictionary<string, ViewManagerVM>();
        }

        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            var activeDoc = acadApp.Application.DocumentManager.MdiActiveDocument;
            if (activeDoc != null)
            {
                var vm = GetViewModel(activeDoc.Name);
                if (vm == null)
                {
                    vm = new ViewManagerVM(activeDoc.Name);
                    AddToDocumentVMDic(activeDoc.Name, vm);
                }
                ShowUI(vm);
            }
        }

        public static void DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            var activeDoc = acadApp.Application.DocumentManager.MdiActiveDocument;
            if (activeDoc != null)
            {
                var vm = GetViewModel(e.Document.Name);
                if (vm == null)
                {
                    vm = new ViewManagerVM(e.Document.Name);
                    AddToDocumentVMDic(e.Document.Name, vm);
                }
                UpdateUI(vm);
            }
        }

        public static void DocumentToBeActivated(object sender, DocumentCollectionEventArgs e)
        {
            //
        }


        public static void DocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document != null)
            {
                RemoveFromDocumentVMDic(e.Document.Name);
            }
        }

        public static void DocumentDestroyed(object sender, DocumentDestroyedEventArgs e)
        {
            if (acadApp.Application.DocumentManager.MdiActiveDocument == null)
            {
                CloseUI();
            }
        }

        private static void AddToDocumentVMDic(string docName, ViewManagerVM vm)
        {
            if (string.IsNullOrEmpty(docName) || vm == null)
            {
                return;
            }
            if (_documentVMDic.ContainsKey(docName))
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

        private static ViewManagerVM GetViewModel(string docName)
        {
            if (!string.IsNullOrEmpty(docName) && _documentVMDic.ContainsKey(docName))
            {
                return _documentVMDic[docName];
            }
            else
            {
                return null;
            }
        }

        private static void UpdateUI(ViewManagerVM vm)
        {
            if (_uiViewManager != null && _uiViewManager.IsLoaded)
            {
                _uiViewManager.UpdateDataContext(vm);
            }
        }

        private static void CloseUI()
        {
            if (_uiViewManager != null && _uiViewManager.IsLoaded)
                _uiViewManager.Close();
        }

        private static void ShowUI(ViewManagerVM vm)
        {
            if (vm == null)
            {
                return;
            }
            if (_uiViewManager != null && _uiViewManager.IsLoaded)
            {
                _uiViewManager.UpdateDataContext(vm);
                _uiViewManager.Show();
            }
            else
            {
                _uiViewManager = new ViewManagerUI(vm);
                acadApp.Application.ShowModelessWindow(_uiViewManager);
            }
        }
    }
}
