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
    internal class ThDrainageADDataQueryService
    {
        //----input
        public List<ThIfcVirticalPipe> VerticalPipeData { get; set; } = new List<ThIfcVirticalPipe>();
        public List<ThIfcFlowSegment> HorizontalPipe { get; set; } = new List<ThIfcFlowSegment>();
        public Point3dCollection SelectPtsTopView { get; set; }
        public Point3dCollection SelectPtsAD { get; set; }
        public List<ThIfcDistributionFlowElement> SanitaryTerminal { get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<ThIfcDistributionFlowElement> AngleValveWaterHeater { get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<Entity> TchValve { get; set; } = new List<Entity>();
        public List<Entity> StartPt { get; set; } = new List<Entity>();
        public Dictionary<string, List<string>> BlockNameDict { get; set; } = new Dictionary<string, List<string>>();
        //----output
        public List<Line> HotPipeTopView { get; set; }
        public List<Line> CoolPipeTopView { get; set; }
        public List<Line> HotPipeAD { get; set; }
        public List<Line> CoolPipeAD { get; set; }
        public List<Line> VerticalPipe { get; set; }
        public List<ThSaniterayTerminal> Terminal { get; set; }
        public List<ThIfcDistributionFlowElement> Valve { get; set; }

        public ThDrainageADDataQueryService()
        {
            HotPipeTopView = new List<Line>();
            CoolPipeTopView = new List<Line>();
            HotPipeAD = new List<Line>();
            CoolPipeAD = new List<Line>();

            VerticalPipe = new List<Line>();
            Terminal = new List<ThSaniterayTerminal>();
            Valve = new List<ThIfcDistributionFlowElement>();
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
            VerticalPipeData.ForEach(x => DrawUtils.ShowGeometry((x.Outline as DBPoint).Position, "l0VerticalPipe", 140, r: 25));
            DrawUtils.ShowGeometry(HotPipeTopView, "l0HotPipeTopView", 230);
            DrawUtils.ShowGeometry(CoolPipeTopView, "l0CoolPipeTopView", 140);
            DrawUtils.ShowGeometry(HotPipeAD, "l0HotPipeAD", 230);
            DrawUtils.ShowGeometry(CoolPipeAD, "l0CoolPipeAD", 140);

            Terminal.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0terminal", 30));
            Terminal.ForEach(x => DrawUtils.ShowGeometry(x.Boundary.GetCenter(), x.Name, "l0terminal", 30, hight: 50));
            Valve.ForEach(x => DrawUtils.ShowGeometry(ThDrainageADTermianlService.GetVisibleOBB(x.Outline as BlockReference), "l0angleValveWaterHeater", 30));
            TchValve.ForEach(x => DrawUtils.ShowGeometry(x.GeometricExtents.ToRectangle(), "l0TchValve", 30));
            StartPt.ForEach(x => DrawUtils.ShowGeometry(x.GeometricExtents.ToRectangle().GetCenter(), "l0Start", 1, r: 30));


        }


        public Point3d GetStartPt()
        {
            var pt = new Point3d();
            pt = StartPt.First().GeometricExtents.ToRectangle().GetCenter();
            return pt;
        }

        public void BuildTermianlMode()
        {
            var terminalData = new List<ThIfcDistributionFlowElement>();
            terminalData.AddRange(AngleValveWaterHeater);
            terminalData.AddRange(SanitaryTerminal);

            foreach (var item in terminalData)
            {
                var name = item.Name;
                if (name != ThDrainageADCommon.BlkName_AngleValve)
                {
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
                else
                {
                    Valve.Add(item);
                }
            }
        }
    }
}
