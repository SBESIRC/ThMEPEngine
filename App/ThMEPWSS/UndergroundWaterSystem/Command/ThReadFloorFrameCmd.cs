using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Common;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.UndergroundWaterSystem.Model;

namespace ThMEPWSS.UndergroundWaterSystem.Command
{
    public class ThReadFloorFrameCmd : ThMEPBaseCommand, IDisposable
    {
        public List<ThFloorModel> FloorList { set; get; }
        public ThReadFloorFrameCmd()
        {
            ActionName = "读取楼层";
            CommandName = "THDXJSXT";
            FloorList = new List<ThFloorModel>();
        }
        public void Dispose()
        {
            //
        }
        public Polyline GetBoundary(ObjectId storeyId)
        {
            if (storeyId.IsErased || storeyId.IsNull || !storeyId.IsValid)
            {
                return new Polyline();
            }
            using (var acadDb = AcadDatabase.Use(storeyId.Database))
            {
                var br = acadDb.Element<BlockReference>(storeyId);
                return br.ToOBB(br.BlockTransform.PreMultiplyBy(Matrix3d.Identity));
            }
        }
        public override void SubExecute()
        {
            try
            {
                //
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (var database = AcadDatabase.Active())
                {
                    Common.Utils.FocusMainWindow();
                    //选取范围
                    var selectedArea = ThUndergroundWaterSystemUtils.SelectArea();
                    //选取楼层框线
                    if (selectedArea.Count != 0)
                    {
                        //选取楼层框线
                        var storeysRecEngine = new ThStoreysRecognitionEngine();//创建楼板识别引擎
                        storeysRecEngine.Recognize(database.Database, selectedArea);
                        if (storeysRecEngine.Elements.Count == 0)
                        {
                            return;
                        }
                        var readStoreyTypeStrings = new string[] { "标准层" , "非标层" ,"楼层"};
                        foreach (var floor in storeysRecEngine.Elements)
                        {
                            if(floor is ThStoreys f)
                            {
                                if(readStoreyTypeStrings.Contains(f.StoreyTypeString))
                                {
                                    var model = new ThFloorModel();
                                    model.FloorName = f.StoreyNumber;
                                    model.FloorArea = GetBoundary(f.ObjectId);
                                    FloorList.Add(model);
                                }
                            }
                        }
                        //按照楼层进行排序
                        FloorList = FloorList.OrderBy(o => o.FloorNumber()).ToList();
                        FloorList.Reverse();
                    }
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }
    }
}
