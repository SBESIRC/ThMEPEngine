using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tianhua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI.EventMonitor
{
    class ObjectSelectEvent
    {
        public static void DocumentManager_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document == null)
            {
            }
            else
            {
                e.Document.ImpliedSelectionChanged += Doc_ImpliedSelctionChanged;
            }
        }
        public static void Doc_ImpliedSelctionChanged(object sender, EventArgs e) 
        {
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
    }
}
