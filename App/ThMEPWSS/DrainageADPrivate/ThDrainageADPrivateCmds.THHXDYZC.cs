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
using ThMEPWSS.DrainageADPrivate.Model;
using ThMEPWSS.DrainageADPrivate.Service;
using ThMEPWSS.DrainageADPrivate;

namespace ThMEPWSS
{
    public partial class ThDrainageADPrivateCmds
    {
        [CommandMethod("TIANHUACAD", "-THHXDYZC", CommandFlags.Modal)]
        public void ThDrainageADPNoUI()
        {
            //户型大样轴侧
            var qL = ThMEPWSSUtils.SettingDouble("\nqL", 230.0);
            var m = ThMEPWSSUtils.SettingDouble("\nm", 3.5);
            var Kh = ThMEPWSSUtils.SettingDouble("\nKh", 1.5);

            var BlockNameDict = new Dictionary<string, List<string>>() {
                                        { "拖把池", new List<string>() { "A-Kitchen-9"}},
                                        { "洗衣机", new List<string>() {  "sdr ter t","A-Toilet-9"} } ,
                                        { "阳台洗手盆",new List<string> { } },
                                        { "厨房洗涤盆", new List<string>() { "edrcgergc", "A-Kitchen-4" } } ,
                                        { "坐便器", new List<string>() { "fdtes", "A-Toilet-5" } } ,
                                        { "单盆洗手台", new List<string>() { "EWRYTY","xishoupen", "A-Toilet-1" } } ,
                                        { "双盆洗手台", new List<string>() { } } ,
                                        { "淋浴器", new List<string>() { "QADAD", "WF" } } ,
                                        { "浴缸", new List<string>() { "A-Toilet-6" } } ,
                                        };

            ThDrainageADSetting.Instance.qL = qL;
            ThDrainageADSetting.Instance.m = m;
            ThDrainageADSetting.Instance.Kh = Kh;
            ThDrainageADSetting.Instance.BlockNameDict = BlockNameDict;

            using (var cmd = new ThDrainageADPCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "ThDrainageLayoutWaterHeater", CommandFlags.Modal)]
        public void ThDrainageTestInsertBlk()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThInsertOutputService.LayoutWaterHeater();
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


                var selectPts = new Point3dCollection();
                selectPtsTop.Cast<Point3d>().ForEach(x => selectPts.Add(x));

                var transformer = ThMEPWSSUtils.GetTransformer(selectPts);

                var blkNameValve = new List<string> { ThDrainageADCommon.BlkName_WaterHeater, ThDrainageADCommon.BlkName_AngleValve,
                                                        ThDrainageADCommon.BlkName_ShutoffValve, ThDrainageADCommon.BlkName_GateValve,
                                                        ThDrainageADCommon.BlkName_CheckValve, ThDrainageADCommon.BlkName_AntifoulingCutoffValve,
                                                        ThDrainageADCommon.BlkName_Casing,
                                                        };

                var BlockNameDict = new Dictionary<string, List<string>>() {
                                        { "拖把池", new List<string>() { "A-Kitchen-9"}},
                                        { "洗衣机", new List<string>() {  "sdr ter t","A-Toilet-9"} } ,
                                        { "阳台洗手盆",new List<string> { } },
                                        { "厨房洗涤盆", new List<string>() { "edrcgergc", "A-Kitchen-4" } } ,
                                        { "坐便器", new List<string>() { "fdtes", "A-Toilet-5" } } ,
                                        { "单盆洗手台", new List<string>() { "EWRYTY","xishoupen", "A-Toilet-1" } } ,
                                        { "双盆洗手台", new List<string>() { } } ,
                                        { "淋浴器", new List<string>() { "QADAD", "WF" } } ,
                                        { "浴缸", new List<string>() { "A-Toilet-6" } } ,

                };

                var dataFactory = new ThDrainageADPrivateDataFactory()
                {
                    Transformer = transformer,
                    BlockNameDict = BlockNameDict,
                    BlockNameValve = blkNameValve,
                    BlockNameTchValve = new List<string> { ThDrainageADCommon.BlkName_ShutoffValve_TchTag.ToUpper(), ThDrainageADCommon.BlkName_GateValve_TchTag.ToUpper(),
                                                        ThDrainageADCommon.BlkName_CheckValve_TchTag.ToUpper(), ThDrainageADCommon.BlkName_AntifoulingCutoffValve_TchTag.ToUpper(),
                                                        ThDrainageADCommon.BlkName_OpeningSign_TchTag.ToUpper(),ThDrainageADCommon .BlkName_WaterMeteValve_TchTag.ToUpper(),
                                                        },
                };

                dataFactory.GetElements(acadDatabase.Database, selectPts);

                var dataQuery = new ThDrainageADDataProcessService()
                {
                    BlockNameDict = BlockNameDict,
                    VerticalPipeData = dataFactory.VerticalPipe,
                    HorizontalPipe = dataFactory.TCHPipe,
                    SelectPtsTopView = selectPtsTop,
                    //SelectPtsAD = selectPtsAD,
                    SanitaryTerminal = dataFactory.SanitaryTerminal,
                    ValveWaterHeater = dataFactory.ValveWaterHeater,
                    TchValve = dataFactory.TchValve,
                    TchOpeningSign = dataFactory.TchOpeningSign,
                    OpeningSign = dataFactory.OpeningSign,
                };

                dataQuery.Transform(transformer);
                dataQuery.SaperateTopViewAD();
                dataQuery.CreateVerticalPipe();
                dataQuery.BuildTermianlMode();
                dataQuery.BuildValve();
                dataQuery.ProjectOntoXYPlane();
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
                var selectFrame = ThSelectFrameUtil.SelectFramePL("框选俯视", "框选俯视");
                if (selectFrame.Area < 100)
                {
                    return;
                }

                var selectPtPrint = ThSelectFrameUtil.SelectPoint("轴侧起点");
                if (selectPtPrint == Point3d.Origin)
                {
                    return;
                }

                var pipes = GetLines(selectFrame, acadDatabase, "W-WSUP-COOL-PIPE");

                var pipe = pipes.OfType<Line>().ToList();

                var engine = new ThTransformTopToADService();
                var pipeNew = new List<Line>();
                foreach (var p in pipe)
                {
                    var st = p.StartPoint;
                    var ed = p.EndPoint;
                    var stN = engine.TransformPt(st);
                    var edN = engine.TransformPt(ed);
                    var newL = new Line(stN, edN);
                    pipeNew.Add(newL);

                }

                var moveDir = selectPtPrint - pipeNew.First().StartPoint;
                var moveTrans = Matrix3d.Displacement(moveDir);
                pipeNew.ForEach(x => x.TransformBy(moveTrans));


                DrawUtils.ShowGeometry(pipeNew, "l0transLine", 30);

            }
        }

        private static List<Curve> GetLines(Polyline polyline, AcadDatabase acdb, string layer)
        {
            var objs = new DBObjectCollection();
            var laneLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == layer);

            List<Curve> laneList = laneLines.Select(x => x.WashClone()).ToList();

            laneList = laneList.Where(x => x != null).ToList();
            laneList.ForEach(x => objs.Add(x));

            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();

            //return sprayLines.SelectMany(x => polyline.Trim(x).Cast<Curve>().ToList()).ToList();
            return sprayLines;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThDrainageVpipeData", CommandFlags.Modal)]
        public void ThDrainageVpipeData()
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

                var recognize = new ThMEPEngineCore.Engine.ThDrainageVPipeRecognitionEngine()
                {

                };
                recognize.RecognizeMS(acadDatabase.Database, selectPtsTop);
                var result = recognize.Elements.OfType<ThMEPEngineCore.Model.Hvac.ThIfcVirticalPipe>().ToList();

                result.ForEach(x => DrawUtils.ShowGeometry((x.Outline as DBPoint).Position, "l0test", r: 50));
            }
        }

    }
}
