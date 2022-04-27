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
        public List<BlockReference> TchValve { private get; set; } = new List<BlockReference>(); //天正阀
        public List<BlockReference> TchOpeningSign { get; set; } = new List<BlockReference>();
        public List<Spline> OpeningSign { private get; set; } = new List<Spline>(); //断管符号
        public Dictionary<string, List<string>> BlockNameDict { private get; set; } = new Dictionary<string, List<string>>();

        //----output
        public List<Line> HotPipeTopView { get; private set; }
        public List<Line> CoolPipeTopView { get; private set; }
        public List<Line> HotPipeAD { get; private set; }
        public List<Line> CoolPipeAD { get; private set; }
        public List<Line> VerticalPipe { get; private set; }//立管
        public List<ThSaniterayTerminal> Terminal { get; private set; }//末端洁具 热水器
        public List<ThValve> Valve { get; private set; } //截止阀,闸阀,止回阀,防污隔断阀,天正阀，天正断管,样条曲线
        public List<ThValve> Casing { get; private set; }//套管系统
        public List<ThValve> AngleValve { get; private set; }//给水角阀平面

        public ThDrainageADDataProcessService()
        {
            HotPipeTopView = new List<Line>();
            CoolPipeTopView = new List<Line>();
            HotPipeAD = new List<Line>();
            CoolPipeAD = new List<Line>();

            VerticalPipe = new List<Line>();
            Terminal = new List<ThSaniterayTerminal>();
            Valve = new List<ThValve>();
            Casing = new List<ThValve>();
            AngleValve = new List<ThValve>();
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
            Terminal.ForEach(x => DrawUtils.ShowGeometry(x.Boundary.GetCenter(), x.Type.ToString(), "l0terminalType", 30, hight: 50));

            Valve.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Valve", 210));
            Valve.ForEach(x => DrawUtils.ShowGeometry(x.InsertPt, x.Dir, "l0Valve", 210, lineWeightNum: 30, l: 150));
            Valve.ForEach(x => DrawUtils.ShowGeometry(x.InsertPt, x.Name, "l0ValveName", 210, hight: 50));

            AngleValve.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0AngleValve", 210));
            AngleValve.ForEach(x => DrawUtils.ShowGeometry(x.InsertPt, x.Dir, "l0AngleValve", 210, lineWeightNum: 30, l: 150));

            Casing.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Casing", 201));
            Casing.ForEach(x => DrawUtils.ShowGeometry(x.InsertPt, x.Dir, "l0Casing", 210, lineWeightNum: 30, l: 150));

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

                if (type == ThDrainageADCommon.TerminalType.WaterHeater)
                {
                    pl = CreateWaterHeater(blk);

                }
                if (type != ThDrainageADCommon.TerminalType.Unknow && pl.NumberOfVertices > 0)
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

        private static Polyline CreateWaterHeater(BlockReference blk)
        {
            var pl = new Polyline();
            pl.Closed = true;
            var tol = new Tolerance(1, 1);
            var obj = new DBObjectCollection();
            blk.Explode(obj);

            var hatch = obj.OfType<Hatch>().OrderByDescending(x => x.Area).First();

            var scale = blk.ScaleFactors.X;
            var r = 50 * scale;
            var lines = obj.OfType<Line>().ToList();
            var ptHatch = hatch.GeometricExtents.GetCenter();
            var connectLs = lines.Where(x => x.StartPoint.IsEqualTo(ptHatch, tol)).ToList();

            if (connectLs.Count() > 0)
            {
                var l = connectLs.First();
                var ldir = (l.EndPoint - l.StartPoint).GetNormal();
                var ldirP = ldir.RotateBy(90 * Math.PI / 180, Vector3d.ZAxis);

                var pt0 = l.StartPoint - ldir * r + ldirP * r;
                var pt1 = pt0 + ldir * r + ldir * l.Length;
                var pt2 = pt1 - ldirP * r * 2;
                var pt3 = pt2 - ldir * l.Length - ldir * r;

                pl.AddVertexAt(0, pt0.ToPoint2D(), 0, 0, 0);
                pl.AddVertexAt(1, pt1.ToPoint2D(), 0, 0, 0);
                pl.AddVertexAt(2, pt2.ToPoint2D(), 0, 0, 0);
                pl.AddVertexAt(3, pt3.ToPoint2D(), 0, 0, 0);
            }
            return pl;
        }

        public void BuildValve()
        {
            //valve:截止阀,闸阀,止回阀,防污隔断阀 天正阀，天正断管，样条曲线断管 angleVavle:给水角阀平面 casing套管
            foreach (var valve in ValveWaterHeater)
            {
                if (valve.Name == ThDrainageADCommon.BlkName_WaterHeater)
                {
                    continue;
                }

                var blk = valve.Outline as BlockReference;
                var dir = new Vector3d(1, 0, 0);
                if (valve.Name == ThDrainageADCommon.BlkName_Casing)
                {
                    dir = new Vector3d(0, -1, 0);
                }
                dir = dir.TransformBy(blk.BlockTransform).GetNormal();
                var pl = ThDrainageADTermianlService.GetVisibleOBB(blk);
                var scale = Math.Abs(blk.ScaleFactors.X);
                var valveModel = new ThValve()
                {
                    InsertPt = blk.Position,
                    Dir = dir,
                    Name = valve.Name,
                    Boundary = pl,
                    Scale = scale,
                };
                if (valve.Name == ThDrainageADCommon.BlkName_Casing)
                {
                    Casing.Add(valveModel);
                }
                else if (valve.Name == ThDrainageADCommon.BlkName_AngleValve)
                {
                    AngleValve.Add(valveModel);
                }
                else
                {
                    Valve.Add(valveModel);
                }

            }

            //天正阀 天正断管
            var tch = new List<BlockReference>();
            tch.AddRange(TchValve);
            tch.AddRange(TchOpeningSign);
            foreach (var blk in tch)
            {
                var dir = new Vector3d(1, 0, 0);
                dir = dir.TransformBy(blk.BlockTransform).GetNormal();
                var pl = ThDrainageADTermianlService.GetVisibleOBB(blk);
                var scale = Math.Abs(blk.ScaleFactors.X);

                var valveModel = new ThValve()
                {
                    InsertPt = blk.Position,
                    Dir = dir,
                    Name = blk.Name,
                    Boundary = pl,
                    Scale = scale,
                };
                Valve.Add(valveModel);
            }

            //样条断管
            foreach (var item in OpeningSign)
            {
                //以天正方向为准（计算方便）注意这个和最终print差了90度
                var pl = item.GeometricExtents.ToRectangle();
                var name = ThDrainageADCommon.BlkName_OpeningSign;
                var dir = GetValveDir(pl, false);
                var valveModel = new ThValve()
                {
                    Boundary = pl,
                    Name = name,
                    Dir = dir,
                    InsertPt = pl.GetCenter(),
                    Scale = 1,
                };
                Valve.Add(valveModel);
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
