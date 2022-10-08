using System.Linq;
using System.Windows.Input;

using AcHelper;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using CommunityToolkit.Mvvm.Input;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

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
                using (var cmd = new ThSprinklerCheckCmd { CommandName = "THPTJH", ActionName = "校核" })
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

        public void SelectAll(string layerNum)
        {
            var layerName = LayerNumToLayerName(layerNum);
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
                        // 接着将模型添加到PickFirst选择集
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
                        // 接着将模型添加到PickFirst选择集
                        Active.Editor.SetImpliedSelection(objs);
                    }
                }
            }
        }

        public void Cancel(string layerNum)
        {
            var layerName = LayerNumToLayerName(layerNum);
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

                        var set = Active.Editor.SelectImplied();
                        if (set.Status == PromptStatus.OK)
                        {
                            var setObjs = set.Value.GetObjectIds();
                            var results = setObjs.Where(obj => !objs.Contains(obj)).ToArray();
                            // 首先清空现有的PickFirst选择集
                            Active.Editor.SetImpliedSelection(new ObjectId[0]);
                            // 接着将模型添加到PickFirst选择集
                            Active.Editor.SetImpliedSelection(results);
                        }
                        else
                        {
                            Active.Editor.SetImpliedSelection(new ObjectId[0]);
                        }
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

                        var set = Active.Editor.SelectImplied();
                        if (set.Status == PromptStatus.OK)
                        {
                            var setObjs = set.Value.GetObjectIds();
                            var results = setObjs.Where(obj => !objs.Contains(obj)).ToArray();
                            // 首先清空现有的PickFirst选择集
                            Active.Editor.SetImpliedSelection(new ObjectId[0]);
                            // 接着将模型添加到PickFirst选择集
                            Active.Editor.SetImpliedSelection(results);
                        }
                        else
                        {
                            Active.Editor.SetImpliedSelection(new ObjectId[0]);
                        }
                    }
                }
            }
        }

        private string LayerNumToLayerName(string layerNum)
        {
            var layerName = "";
            switch (layerNum)
            {
                case "1":
                    layerName = ThWSSCommon.Blind_Zone_LayerName;
                    break;
                case "2":
                    layerName = ThWSSCommon.From_Boundary_So_Far_LayerName;
                    break;
                case "3":
                    layerName = ThWSSCommon.Room_Checker_LayerName;
                    break;
                case "4":
                    layerName = ThWSSCommon.Parking_Stall_Checker_LayerName;
                    break;
                case "5":
                    layerName = ThWSSCommon.Mechanical_Parking_Stall_Checker_LayerName;
                    break;
                case "6":
                    layerName = ThWSSCommon.Sprinkler_Distance_LayerName;
                    break;
                case "7":
                    layerName = ThWSSCommon.From_Boundary_So_Close_LayerName;
                    break;
                case "8":
                    layerName = ThWSSCommon.Distance_Form_Beam_LayerName;
                    break;
                case "9":
                    layerName = ThWSSCommon.Beam_Checker_LayerName;
                    break;
                case "10":
                    layerName = ThWSSCommon.Pipe_Checker_LayerName;
                    break;
                case "11":
                    layerName = ThWSSCommon.Duct_Checker_LayerName;
                    break;
                case "12":
                    layerName = ThWSSCommon.Sprinkler_So_Dense_LayerName;
                    break;
            }
            return layerName;
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
