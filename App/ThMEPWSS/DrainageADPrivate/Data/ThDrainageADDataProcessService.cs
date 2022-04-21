using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Diagnostics;
using ThMEPWSS.Sprinkler.Data;
using ThMEPWSS.ViewModel;

using ThMEPWSS.DrainageADPrivate.Model;
using ThMEPWSS.DrainageADPrivate.Service;

namespace ThMEPWSS.DrainageADPrivate.Data
{
    internal class ThDrainageADDataProcessService
    {
        //----input
        public List<ThIfcVirticalPipe> VerticalPipeData { private get; set; } = new List<ThIfcVirticalPipe>();
        public List<ThIfcFlowSegment> HorizontalPipe { private get; set; } = new List<ThIfcFlowSegment>();
        public Point3dCollection SelectPtsTopView { private get; set; }
        public Point3dCollection SelectPtsAD { private get; set; }
        public List<ThIfcDistributionFlowElement> SanitaryTerminal { private get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<ThIfcDistributionFlowElement> ValveWaterHeater { private get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<Entity> TchValve { private get; set; } = new List<Entity>(); //天正阀
        public List<Entity> OpeningSignData { private get; set; } = new List<Entity>(); //断管符号
        public Dictionary<string, List<string>> BlockNameDict { private get; set; } = new Dictionary<string, List<string>>();

        //----output
        public List<Line> HotPipeTopView { get; private set; }
        public List<Line> CoolPipeTopView { get; private set; }
        public List<Line> HotPipeAD { get; private set; }
        public List<Line> CoolPipeAD { get; private set; }
        public List<Line> VerticalPipe { get; private set; }//立管
        public List<ThSaniterayTerminal> Terminal { get; private set; }//末端 热水器
        public List<ThValve> Valve { get; private set; } //给水角阀平面,截止阀,闸阀,止回阀,防污隔断阀,,天正截止阀
        public List<ThValve> OpeningSign { get; private set; }//断管,样条曲线
        public List<ThValve> Casing { get; private set; }//套管系统

        public ThDrainageADDataProcessService()
        {
            HotPipeTopView = new List<Line>();
            CoolPipeTopView = new List<Line>();
            HotPipeAD = new List<Line>();
            CoolPipeAD = new List<Line>();

            VerticalPipe = new List<Line>();
            Terminal = new List<ThSaniterayTerminal>();
            Valve = new List<ThValve>();
            OpeningSign = new List<ThValve>();
            Casing = new List<ThValve>();
        }

        public void SaperateTopViewAD()
        {

            var collection = HorizontalPipe.Select(o => o.Outline).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(collection);

            var pipeTopView = spatialIndex.SelectCrossingPolygon(SelectPtsTopView);
            HorizontalPipe.Where(o => pipeTopView.Contains(o.Outline)).ToList().ForEach(x =>
            {
                if (x.Outline.Layer == ThDrainageADCommon.Layer_HotPipe)
                {
                    HotPipeTopView.Add(x.Outline as Line);
                }
                else if (x.Outline.Layer == ThDrainageADCommon.Layer_CoolPipe)
                {
                    CoolPipeTopView.Add(x.Outline as Line);
                }

            });

            if (SelectPtsAD == null)
            {
                return;
            }
            var ADPipes = spatialIndex.SelectCrossingPolygon(SelectPtsAD);
            HorizontalPipe.Where(o => ADPipes.Contains(o.Outline)).ToList().ForEach(x =>
            {
                if (x.Outline.Layer == ThDrainageADCommon.Layer_HotPipe)
                {
                    HotPipeAD.Add(x.Outline as Line);
                }
                else if (x.Outline.Layer == ThDrainageADCommon.Layer_CoolPipe)
                {
                    CoolPipeAD.Add(x.Outline as Line);
                }
            });

        }

        public void CreateVerticalPipe()
        {
            foreach (var pipe in VerticalPipeData)
            {
                var entity = pipe.Data;
                var pipeParameters = ThOPMTools.GetOPMProperties(entity.Id);
                var start = Convert.ToDouble(pipeParameters["起点标高"]);
                var end = Convert.ToDouble(pipeParameters["终点标高"]);

                var pt = (pipe.Outline as DBPoint).Position;


                var pts = new Point3d(pt.X, pt.Y, start);
                var pte = new Point3d(pt.X, pt.Y, end);

                var trueVertical = new Line(pts, pte);

                VerticalPipe.Add(trueVertical);
            }
        }
        public void Transform(ThMEPOriginTransformer transformer)
        {
            VerticalPipeData.ForEach(x => transformer.Transform(x.Outline));
            HorizontalPipe.ForEach(x => transformer.Transform(x.Outline));

            SanitaryTerminal.ForEach(x => transformer.Transform(x.Outline));

        }

        public void Reset(ThMEPOriginTransformer transformer)
        {
            VerticalPipeData.ForEach(x => transformer.Reset(x.Outline));
            HorizontalPipe.ForEach(x => transformer.Reset(x.Outline));

            HotPipeTopView.ForEach(x => transformer.Reset(x));
            CoolPipeTopView.ForEach(x => transformer.Reset(x));
            HotPipeAD.ForEach(x => transformer.Reset(x));
            CoolPipeAD.ForEach(x => transformer.Reset(x));


        }

        public void Print()
        {
            DrawUtils.ShowGeometry(HotPipeTopView, "l0HotPipeTopView", 230);
            DrawUtils.ShowGeometry(CoolPipeTopView, "l0CoolPipeTopView", 140);
            DrawUtils.ShowGeometry(HotPipeAD, "l0HotPipeAD", 230);
            DrawUtils.ShowGeometry(CoolPipeAD, "l0CoolPipeAD", 140);
            VerticalPipe.ForEach(x => DrawUtils.ShowGeometry(x.StartPoint, "l0VerticalPipe", 140, r: 25));

            Terminal.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0terminal", 30));
            Terminal.ForEach(x => DrawUtils.ShowGeometry(x.Boundary.GetCenter(), x.Type.ToString(), "l0terminal", 30, hight: 50));

            Valve.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Valve", 210));
            Valve.ForEach(x => DrawUtils.ShowGeometry(x.Boundary.GetCenter(), x.Name, "l0Valve", 210, hight: 50));

            OpeningSign.ForEach(x => DrawUtils.ShowGeometry(x.CenterPt, "l0OpeningSign", 40, r: 30));
            Casing.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Casing", 201));

        }

        public void BuildTermianlMode()
        {
            var terminalData = new List<ThIfcDistributionFlowElement>();
            terminalData.AddRange(ValveWaterHeater.Where(x => x.Name == ThDrainageADCommon.BlkName_WaterHeater));
            terminalData.AddRange(SanitaryTerminal);

            foreach (var item in terminalData)
            {
                var name = item.Name;
                var blk = item.Outline as BlockReference;
                var pl = ThDrainageADTermianlService.GetVisibleOBB(blk);
                var type = ThDrainageADTermianlService.GetTerminalType(name, BlockNameDict);

                if (type != ThDrainageADCommon.TerminalType.Unknow)
                {
                    var terminal = new ThSaniterayTerminal()
                    {
                        Data = blk,
                        Boundary = pl,
                        Name = item.Name,
                        Type = type,
                    };
                    Terminal.Add(terminal);
                }
            }
        }

        public void BuildValve()
        {
            //给水角阀平面,截止阀,闸阀,止回阀,防污隔断阀 //分开阀和套管
            foreach (var valve in ValveWaterHeater)
            {
                if (valve.Name == ThDrainageADCommon.BlkName_WaterHeater ||
                    valve.Name == ThDrainageADCommon.BlkName_AngleValve)
                {
                    continue;
                }

                var blk = valve.Outline as BlockReference;
                var pl = ThDrainageADTermianlService.GetVisibleOBB(blk);
                var dir = new Vector3d();
                if (valve.Name == ThDrainageADCommon.BlkName_Casing)
                {
                    dir = new Vector3d(0, -1, 0);
                }
                else
                {
                    dir = new Vector3d(0, 1, 0);
                }
                dir = dir.RotateBy(blk.Rotation, Vector3d.ZAxis).GetNormal();

                var valveModel = new ThValve()
                {
                    InsertPt = blk.Position,
                    Boundary = pl,
                    Dir = dir,
                    Name = valve.Name,
                    CenterPt = pl.GetCenter(),
                };
                if (valve.Name == ThDrainageADCommon.BlkName_Casing)
                {
                    Casing.Add(valveModel);
                }
                else
                {
                    Valve.Add(valveModel);
                }

            }

            //天正阀
            foreach (var valve in TchValve)
            {
                var pl = valve.GeometricExtents.ToRectangle();
                var name = ThDrainageADCommon.BlkName_ShutoffValve;
                var dir = GetValveDir(pl, false);
                var valveModel = new ThValve()
                {
                    Boundary = pl,
                    Name = name,
                    Dir = dir,
                    InsertPt = pl.GetCenter(),
                    CenterPt = pl.GetCenter(),
                };
                Valve.Add(valveModel);
            }

            //天正断管,样条断管？？
            foreach (var valve in OpeningSignData)
            {
                var pl = valve.GeometricExtents.ToRectangle();
                var name = ThDrainageADCommon.BlkName_OpeningSign;
                //var dir = new Vector3d(1,0,0);
                //dir = dir.RotateBy(valve.Rotation, Vector3d.ZAxis).GetNormal();
                var dir = GetValveDir(pl, false);
                var valveModel = new ThValve()
                {
                    Boundary = pl,
                    Name = name,
                    Dir = dir,
                    InsertPt = pl.GetCenter(),
                    CenterPt = pl.GetCenter(),
                };
                OpeningSign.Add(valveModel);
            }
        }

        private static Vector3d GetValveDir(Polyline Boundary, bool useLong)
        {
            var vector1 = Boundary.GetPoint3dAt(1) - Boundary.GetPoint3dAt(0);
            var vector2 = Boundary.GetPoint3dAt(2) - Boundary.GetPoint3dAt(1);

            var vectorReturn = vector1.Length >= vector2.Length ? vector1 : vector2;

            if (useLong == false)
            {
                vectorReturn = vector1.Length >= vector2.Length ? vector2 : vector1;
            }

            vectorReturn = vectorReturn.GetNormal();
            return vectorReturn;
        }



    }
}
