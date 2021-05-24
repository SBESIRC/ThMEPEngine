using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPElectrical.SystemDiagram.Engine;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPElectrical.SystemDiagram.Model.WireCircuit;
using ThMEPElectrical.SystemDiagram.Service;

namespace ThMEPElectrical.Command
{
    public class ThAutoFireAlarmSystemCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var dataEngine = new ThAutoFireAlarmSystemRecognitionEngine())
            {
                var per = Active.Editor.GetEntity("\n选择一个防火分区(多段线):");
                var pts = new Point3dCollection();
                if (per.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    var frame = acadDatabase.Element<Entity>(per.ObjectId);
                    if (frame is Polyline polyLineframe)
                    {
                        pts = polyLineframe.TessellatePolylineWithArc(100).Vertices();
                    }
                    else
                    {
                        Active.Editor.WriteLine("选择的不是一个多段线!");
                        return;
                    }
                }
                else
                {
                    Active.Editor.WriteLine("并未正确选择!");
                    return;
                }
                //加载块集合配置文件白名单
                ThBlockConfigModel.Init();
                //获取该区域的所有所需块
                dataEngine.Recognize(acadDatabase.Database, pts);

                #region 填充进Model
                ThAutoFireAlarmSystemModel DataModel = new ThAutoFireAlarmSystemModel();
                //添加一个楼层信息
                DataModel.floors.Add(new ThFloorModel()
                {
                    FloorNumber = 1
                });
                //添加一个防火分区
                DataModel.floors[0].FireDistricts.Add(new ThFireDistrictModel
                {
                    FireDistrictName = "Select",
                    Data = new DataSummary()
                    {
                        BlockData = dataEngine.FillingBlockNameConfigModel()
                    }
                });
                //添加一个防火分区 第二层 test
                DataModel.floors[0].FireDistricts.Add(new ThFireDistrictModel
                {
                    FireDistrictName = "All1",
                    Data = new DataSummary()
                    {
                        BlockData = dataEngine.FillingBlockNameConfigModelAll1Test()
                    }
                });
                //添加一个防火分区 第三层 test
                DataModel.floors[0].FireDistricts.Add(new ThFireDistrictModel
                {
                    FireDistrictName = "All0",
                    Data = new DataSummary()
                    {
                        BlockData = dataEngine.FillingBlockNameConfigModelAll0Test()
                    }
                });
                #endregion

                //画
                DataModel.Draw();



            }
        }

        
    }
}
