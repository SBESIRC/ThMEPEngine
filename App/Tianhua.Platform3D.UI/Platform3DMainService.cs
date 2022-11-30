using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using Tianhua.Platform3D.UI.Interfaces;
using Tianhua.Platform3D.UI.UI;
using Tianhua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI
{
    class Platform3DMainService:IMultiDocument
    {
        PaletteSet mainPaletteSet = null;
        PlatformMainUI platformMainUI = null;
        public static Platform3DMainService Instace = new Platform3DMainService();
        Platform3DMainService() 
        {
            mainPaletteSet = new PaletteSet("天华结构三维设计");
            platformMainUI = new PlatformMainUI();
            mainPaletteSet.AddVisual("", platformMainUI);
            mainPaletteSet.KeepFocus = true;
            mainPaletteSet.Visible = true;
            mainPaletteSet.DockEnabled = DockSides.Left;
            mainPaletteSet.Dock = DockSides.Left;
            mainPaletteSet.Visible = false;
            mainPaletteSet.StateChanged += MainPaletteSet_StateChanged;
        }
        private void MainPaletteSet_StateChanged(object sender, PaletteSetStateEventArgs e)
        {
            if (e.NewState == StateEventIndex.Hide)
            {
                RemoveSelectEvent();
            }
            else if (e.NewState == StateEventIndex.Show) 
            {
                AddSelectEvent();
            }
        }
        public void ShowUI() 
        {
            if (mainPaletteSet.Visible)
                return;
            mainPaletteSet.Visible = true;
            MainUIShowInDocument();
        }
        public void HideUI() 
        {
            if (!mainPaletteSet.Visible)
                return;
            mainPaletteSet.Visible = false;
        }
        public string CurrentDocumentId()
        {
            var doc = Active.Document;
            return GetDocumentId(doc);
        }
        public string GetDocumentId(Document document) 
        {
            string docId = string.Empty;
            if (null == document)
                return docId;
            docId = document.UnmanagedObject.ToString();
            return docId;
        }
        private void AddSelectEvent() 
        {
            var doc = Active.Document;
            if (doc == null)
                return;
            doc.ImpliedSelectionChanged += Doc_ImpliedSelctionChanged;
        }
        private void RemoveSelectEvent()
        {
            var doc = Active.Document;
            if (doc == null)
                return;
            doc.ImpliedSelectionChanged -= Doc_ImpliedSelctionChanged;
        }
        private void Doc_ImpliedSelctionChanged(object sender, EventArgs e)
        {
            if (!mainPaletteSet.Visible)
                return;
            List<ObjectId> selectIds = new List<ObjectId>();
            var doc = sender as Document;
            if (null != doc && doc.IsActive)
            {
                if (null != doc.Editor)
                {
                    PromptSelectionResult pkf = doc.Editor.SelectImplied();
                    if (pkf.Status == PromptStatus.OK)
                    {
                        selectIds = pkf.Value.GetObjectIds().ToList();
                    }
                }
            }
            PropertiesViewModel.Instacne.SelectIds(selectIds);
        }
        public void DocumentActivated(DocumentCollectionEventArgs e) 
        {
            if (!mainPaletteSet.Visible)
                return;
            if (string.IsNullOrEmpty(CurrentDocumentId()))
                mainPaletteSet.Visible = false;
            platformMainUI.DocumentActivated(e);
            AddSelectEvent();
        }
        public void DocumentDestroyed(DocumentDestroyedEventArgs e) 
        {
            if (!mainPaletteSet.Visible)
                return;
            platformMainUI.DocumentDestroyed(e);
        }
        public void DocumentToBeActivated(DocumentCollectionEventArgs e) 
        {
            if (!mainPaletteSet.Visible)
                return;
            platformMainUI.DocumentToBeActivated(e);
        }
        public void DocumentToBeDestroyed(DocumentCollectionEventArgs e) 
        {
            if (!mainPaletteSet.Visible)
                return;
            platformMainUI.DocumentToBeDestroyed(e);
        }

        public void MainUIShowInDocument()
        {
            platformMainUI.MainUIShowInDocument();
        }
    }
}
