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
using Dreambuild.AutoCAD;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.IO;

using ThMEPWSS.Common;
using ThMEPWSS.DrainageADPrivate.Data;
using ThMEPWSS.DrainageADPrivate.Cmd;
using ThMEPWSS.DrainageADPrivate;

namespace ThMEPWSS
{
    public partial class ThDrainageADPrivateCmds
    {
        [CommandMethod("TIANHUACAD", "ThDrainageADP", CommandFlags.Modal)]
        public void ThDrainageADP()
        {
            //using (var cmd = new ThHydrantLayoutCmd())
            //{
            //    cmd.Execute();
            //}
        }

        [CommandMethod("TIANHUACAD", "ThDrainageADPNoUI", CommandFlags.Modal)]
        public void ThDrainageADPNoUI()
        {

            //var hintObject = new Dictionary<string, (string, string)>()
            //            {{"0",("0","消火栓")},
            //            {"1",("1","灭火器")},
            //            {"2",("2","消火栓 & 灭火器")},
            //            };
            //var layoutObject = ThHydrantUtil.SettingSelection("\n优化对象", hintObject, "2");
            //var radius = ThHydrantUtil.SettingInt("\n半径", 3000);


            //var hintMode = new Dictionary<string, (string, string)>()
            //            {{"0",("0","一字")},
            //            {"1",("1","L字")},
            //            {"2",("2","自由布置")},
            //            };
            //var layoutMode = ThHydrantUtil.SettingSelection("\n摆放方式", hintMode, "2");

            //HydrantLayoutSetting.Instance.LayoutObject = Convert.ToInt32(layoutObject);
            //HydrantLayoutSetting.Instance.SearchRadius = radius;
            //HydrantLayoutSetting.Instance.LayoutMode = Convert.ToInt32(layoutMode);

            using (var cmd = new ThDrainageADPCmd())
            {
                cmd.Execute();
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThDrainageADPData", CommandFlags.Modal)]
        public void ThDrainageADPData()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //画框，提数据，转数据
                var selectPtsTop = ThSelectFrameUtil.SelectFramePointCollection("框选俯视", "框选俯视");
                if (selectPtsTop.Count == 0)
                {
                    return;
                }

                var selectPtsAD = ThSelectFrameUtil.SelectFramePointCollection("框选轴侧", "框选轴侧");
                if (selectPtsAD.Count == 0)
                {
                    return;
                }
                var selectPts = new Point3dCollection();
                selectPtsTop.Cast<Point3d>().ForEach(x => selectPts.Add(x));
                selectPtsAD.Cast<Point3d>().ForEach(x => selectPts.Add(x));

                var transformer = ThMEPWSSUtils.GetTransformer(selectPts);

                var BlockNameDict = new Dictionary<string, List<string>>() {
                                        { "拖把池", new List<string>() { }},
                                        { "洗衣机", new List<string>() {  "sdr ter t"} } ,
                                        { "阳台洗手盆",new List<string> { } },
                                        { "厨房洗涤盆", new List<string>() { "edrcgergc", } } ,
                                        { "坐便器", new List<string>() { "fdtes" } } ,
                                        { "单盆洗手台", new List<string>() { "EWRYTY","xishoupen" } } ,
                                        { "双盆洗手台", new List<string>() { } } ,
                                        { "淋浴器", new List<string>() { "QADAD", "WF" } } ,
                                        { "浴缸", new List<string>() { } } ,

                };

                var dataFactory = new ThDrainageADPrivateDataFactory()
                {
                    Transformer = transformer,
                    BlockNameDict = BlockNameDict,
                    BlockNameValve = new List<string> { ThDrainageADCommon.BlkName_WaterHeater, ThDrainageADCommon.BlkName_AngleValve },
                    BlockNameTchValve = ThDrainageADCommon.BlkName_TchValve,

                };

                dataFactory.GetElements(acadDatabase.Database, selectPts);

                var dataQuery = new ThDrainageADDataQueryService()
                {
                    BlockNameDict = BlockNameDict,
                    VerticalPipeData = dataFactory.VerticalPipe,
                    HorizontalPipe = dataFactory.TCHPipe,
                    SelectPtsTopView = selectPtsTop,
                    SelectPtsAD = selectPtsAD,
                    SanitaryTerminal = dataFactory.SanitaryTerminal,
                    AngleValveWaterHeater = dataFactory.AngleValveWaterHeater,
                    TchValve = dataFactory.TchValve,
                    StartPt = dataFactory.StartPt,
                };

                //dataQuery.Transform(transformer);
                dataQuery.SaperateTopViewAD();
                dataQuery.CreateVerticalPipe();
                dataQuery.BuildTermianlMode();
                dataQuery.Print();
                //dataQuery.Reset(transformer);




            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThDrainageTrans", CommandFlags.Modal)]
        public void ThDrainageTrans()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var linex = new Line(new Point3d(1000, 1000, 3000), new Point3d(3000, 1000, 3000));
                var liney = new Line(new Point3d(1000, 1000, 3000), new Point3d(1000, 3000, 3000));
                var linez = new Line(new Point3d(1000, 1000, 3000), new Point3d(1000, 1000, 0));

                DrawUtils.ShowGeometry(linex, "l0test", 1);
                DrawUtils.ShowGeometry(liney, "l0test", 3);
                DrawUtils.ShowGeometry(linez, "l0test", 5);

                var linex2 = linex.Clone() as Line;
                var liney2 = liney.Clone() as Line;
                var linez2 = linez.Clone() as Line;

                //var matrix = Matrix3d.Rotation(Math.PI / 2, -Vector3d.XAxis, new Point3d(0, 0, 0)) *
                //             Matrix3d.Rotation(Math.PI / 4, Vector3d.ZAxis, new Point3d(0, 0, 0)) *
                //             Matrix3d.Displacement(new Vector3d(5000, 0, 0));


                //var matrix = Matrix3d.Rotation(Math.PI / 2, -Vector3d.XAxis, new Point3d(0, 0, 0)) *
                //Matrix3d.Rotation(Math.PI / 4, Vector3d.ZAxis, new Point3d(0, 0, 0));

                var plane = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));

                var rotationMatrix = Matrix3d.Rotation(Math.PI / 4, -Vector3d.XAxis, new Point3d(0, 0, 0));

                var shearMatrix = new Matrix3d(new double[] {
                                        1, 0.5, 0, 0,
                                        0, 1, 0, 0,
                                        0, 0, 1, 0,
                                        0.0, 0.0, 0.0, 1.0
                                    });

                var matrix = shearMatrix;





                linex2.TransformBy(matrix);
                liney2.TransformBy(matrix);
                linez2.TransformBy(matrix);


                linex2 = linex2.GetOrthoProjectedCurve(plane) as Line;
                liney2 = liney2.GetOrthoProjectedCurve(plane) as Line;
                linez2 = linez2.GetOrthoProjectedCurve(plane) as Line;

                DrawUtils.ShowGeometry(linex2, "l1test", 1);
                DrawUtils.ShowGeometry(liney2, "l1test", 3);
                DrawUtils.ShowGeometry(linez2, "l1test", 5);

            }
        }




    }
}
