using System.Windows.Input;

using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using DotNetARX;
using Dreambuild.AutoCAD;
using GalaSoft.MvvmLight.Command;
using Linq2Acad;
using NFox.Cad;

using ThMEPWSS.Command;
using ThMEPWSS.Sprinkler.Model;

namespace ThMEPWSS.ViewModel
{
    public class ThSprinklerCheckerVM
    {
        public ThSprinklerModel Parameter { get; set; }

        public ThSprinklerCheckerVM()
        {
            Parameter = new ThSprinklerModel();
        }
        public ICommand SprinklerCheckCmd => new RelayCommand(CheckClick);

        private void CheckClick()
        {
            if (CheckParameter())
            {
                SetFocusToDwgView();
                using (var cmd = new ThSprinklerCheckCmd { CommandName = "THPTJH", ActionName = "校核"})
                {
                    cmd.Execute();
                }
            }
        }
        private bool CheckParameter()
        {
            // ToDO
            return true;
        }

        private void OpenLayer(string layerName)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(layerName);
                acadDatabase.Database.UnLockLayer(layerName);
                acadDatabase.Database.UnOffLayer(layerName);
            }
        }

        public void SelectAll(string layerName, string layerNum)
        {
            using (var docLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                SetFocusToDwgView();
                if (layerNum != "8")
                {
                    var filter = OpFilter.Bulid(o => o.Dxf((int)DxfCode.LayerName) == layerName);
                    var elements = Active.Editor.SelectAll(filter);
                    if (elements.Status == PromptStatus.OK)
                    {
                        var objs = elements.Value.GetObjectIds();
                        OpenLayer(layerName);

                        // 首先清空现有的PickFirst选择集
                        Active.Editor.SetImpliedSelection(new ObjectId[0]);
                        // 接着讲模型添加到PickFirst选择集
                        Active.Editor.SetImpliedSelection(objs);
                    }
                }
                else
                {
                    var filter = OpFilter.Bulid(o => o.Dxf((int)DxfCode.LayerName) == layerName
                        | o.Dxf((int)DxfCode.LayerName) == ThWSSCommon.Layout_Area_LayerName);
                    var elements = Active.Editor.SelectAll(filter);
                    if (elements.Status == PromptStatus.OK)
                    {
                        var objs = elements.Value.GetObjectIds();
                        OpenLayer(layerName);
                        OpenLayer(ThWSSCommon.Layout_Area_LayerName);

                        // 首先清空现有的PickFirst选择集
                        Active.Editor.SetImpliedSelection(new ObjectId[0]);
                        // 接着讲模型添加到PickFirst选择集
                        Active.Editor.SetImpliedSelection(elements.Value.GetObjectIds());
                    }
                }
            }
        }

        private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
