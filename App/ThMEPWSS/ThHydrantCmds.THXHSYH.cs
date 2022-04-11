using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using AcHelper;
using Linq2Acad;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.Common;
using ThMEPWSS.HydrantLayout.Command;
using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Data;
using ThMEPWSS.HydrantLayout.Model;

namespace ThMEPWSS
{
    public partial class ThHydrantCmds
    {

        [CommandMethod("TIANHUACAD", "-THXHSYH", CommandFlags.Modal)]
        public void THHydrantLayoutNoUI()
        {

            var hintObject = new Dictionary<string, (string, string)>()
                        {{"0",("0","消火栓")},
                        {"1",("1","灭火器")},
                        {"2",("2","消火栓 & 灭火器")},
                        };
            var layoutObject = ThMEPWSSUtils.SettingSelection("\n优化对象", hintObject, "2");
            var radius = ThMEPWSSUtils.SettingInt("\n半径", 3000);


            var hintMode = new Dictionary<string, (string, string)>()
                        {{"0",("0","一字")},
                        {"1",("1","L字")},
                        {"2",("2","自由布置")},
                        };
            var layoutMode = ThMEPWSSUtils.SettingSelection("\n摆放方式", hintMode, "2");


            var avoidParking = ThMEPWSSUtils.SettingBoolean("\n车位是否阻挡开门", 1);

            HydrantLayoutSetting.Instance.LayoutObject = Convert.ToInt32(layoutObject);
            HydrantLayoutSetting.Instance.SearchRadius = radius;
            HydrantLayoutSetting.Instance.LayoutMode = Convert.ToInt32(layoutMode);
            HydrantLayoutSetting.Instance.AvoidParking = avoidParking;
            HydrantLayoutSetting.Instance.BlockNameDict = new Dictionary<string, List<string>>()
                                {
                                    { "集水井", new List<string>() { "A-Well-1" }},
                                    { "非机械车位", new List<string>() { "car0", "停车位4", "A-Parking-1", "C514C01F1", "car", "C614A45C8", "4213", "C6356253C", "车位5100", "bkcw", "C0A575437", "停车位2" } }
                                };
            using (var cmd = new ThHydrantLayoutCmd())
            {
                cmd.Execute();
            }
        }


        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "THHydrantData", CommandFlags.Modal)]
        public void THHydrantData()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //画框，提数据，转数据
                var selectPts = ThSelectFrameUtil.GetFrame();
                if (selectPts.Count == 0)
                {
                    return;
                }

                //var transformer = ThHydrantUtil.GetTransformer(selectPts);
                var transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));

                var BlockNameDict = new Dictionary<string, List<string>>() {
                                        {"集水井", new List<string>() { "A-Well-1" }},
                                        {"非机械车位", new List<string>() { "car0", "停车位4", "A-Parking-1", "C514C01F1", "car", "C614A45C8", "4213", "C6356253C", "车位5100", "bkcw", "C0A575437" , "停车位2" } } };

                var dataFactory = new ThHydrantLayoutDataFactory()
                {
                    Transformer = transformer,
                    BlockNameDict = BlockNameDict,
                };
                dataFactory.GetElements(acadDatabase.Database, selectPts);

                var dataQuery = new ThHydrantLayoutDataQueryService()
                {
                    VerticalPipe = dataFactory.VerticalPipe,
                    Hydrant = dataFactory.Hydrant,
                    InputExtractors = dataFactory.Extractors,
                    Car = dataFactory.Car,
                    Well = dataFactory.Well,
                };

                dataQuery.ProcessArchitechData();
                dataQuery.ProcessHydrant();
                dataQuery.Transform(transformer);
                dataQuery.ProjectOntoXYPlane();
                dataQuery.Print();



                //dataQuery.Reset(transformer);
                //dataQuery.Print();
                //dataQuery.Clean();

                //TestRoomWallColumn(dataQuery);
            }
        }

        private void TestRoomWallColumn(ThHydrantLayoutDataQueryService dataQuery)
        {

            var room = dataQuery.Room;
            var wall = dataQuery.Wall;
            var column = dataQuery.Column;
            ///////////房间和墙线柱子相交。这里只用第一个room试了一下
            var obj = new DBObjectCollection();
            wall.ForEach(x => obj.Add(x));
            column.ForEach(x => obj.Add(x));
            var mr = room.OfType<MPolygon>().ToList();
            var differ = mr[0].DifferenceMP(obj);
            differ.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l0mroom"));
            //////////////////

            ///////做圆。hard code了
            var c = new Circle(new Point3d(434675.7626, 789952.2563, 0), Vector3d.ZAxis, 3000);
            var cp = c.Tessellate(100);//这里是把圆变成polyline
            DrawUtils.ShowGeometry(cp, "l0c");

            //房间丢去空间索引用，用圆找空间索引
            var spindex = new ThCADCoreNTSSpatialIndex(differ);
            var selectRoom = spindex.SelectCrossingPolygon(cp);

            //找出来的空间索引的房间和圆做相交得到我们画的那个”绿色“部分
            var greedPart = cp.IntersectionMP(selectRoom, true);
            greedPart.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l0green"));
            selectRoom.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l0selectRoom"));
        }



    }
}
