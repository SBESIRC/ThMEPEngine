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
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.Sprinkler.Data;
using ThMEPWSS.SprinklerDim.Service;

namespace ThMEPWSS.SprinklerDim.Data
{
    public class ThSprinklerDimDataProcessService
    {
        private double LineTol = 1;
        //----input
        public ThMEPOriginTransformer Transformer { get; set; } = new ThMEPOriginTransformer();
        public List<ThIfcFlowSegment> TchPipeData { private get; set; } = new List<ThIfcFlowSegment>();
        public List<Curve> LinePipeData { private get; set; } = new List<Curve>();
        public List<Entity> LinePipeTextData { private get; set; } = new List<Entity>();
        public List<Point3d> SprinklerPt { get; set; } = new List<Point3d>();
        public List<ThExtractorBase> InputExtractors { get; set; }
        public List<Curve> AxisCurvesData { get; set; } = new List<Curve>();

        public List<ThIfcRoom> RoomIFCData { get; set; } = new List<ThIfcRoom>();

        //---private

        //----output
        public List<Line> Pipe { get; private set; } = new List<Line>();
        public List<Polyline> PipeTextPl { get; private set; } = new List<Polyline>();
        public List<Polyline> Column { get; set; } = new List<Polyline>();
        public List<Polyline> Wall { get; set; } = new List<Polyline>(); //mpolygon //polyline
        public List<MPolygon> Room { get; set; } = new List<MPolygon>(); //mpolygon //polyline
        public List<Line> AxisCurves { get; set; } = new List<Line>();
        public List<Entity> PreviousData { get; set; } = new List<Entity>();
        public void ProcessData()
        {
            ProcessArchitechData();
            ProcessAxisCurve();
            RemoveDuplicateSprinklerPt();
            CreateTchPipe();
            CreateTchPipeText();
            ProcessLinePipe();
            ProcessLinePipeText();
            Transform();
            ProjectOntoXYPlane();
        }


        public void CreateTchPipe()
        {
            var line = TchPipeData.Select(o => o.Outline).OfType<Line>().Where(x => x.Length >= LineTol);
            Pipe.AddRange(line);
        }

        public void CreateTchPipeText()
        {
            var textGeom = TchPipeData.Select(o => o.Outline).OfType<DBText>().Select(x => x.TextOBB()).ToList();
            PipeTextPl.AddRange(textGeom);
        }
        public void RemoveDuplicateSprinklerPt()
        {
            SprinklerPt = SprinklerPt.Distinct().ToList();
        }

        public void ProcessArchitechData()
        {
            var wallData = new List<Entity>(); //mpolygon //polyline
            var roomData = new List<Entity>(); //mpolygon //polyline

            if (InputExtractors != null && InputExtractors.Count() > 0)
            {
                var architectureWallExtractor = InputExtractors.Where(o => o is ThSprinklerArchitectureWallExtractor).First() as ThSprinklerArchitectureWallExtractor;
                var shearWallExtractor = InputExtractors.Where(o => o is ThSprinklerShearWallExtractor).First() as ThSprinklerShearWallExtractor;
                var columnExtractor = InputExtractors.Where(o => o is ThSprinklerColumnExtractor).First() as ThSprinklerColumnExtractor;
                //var roomExtractor = InputExtractors.Where(o => o is ThSprinklerRoomExtractor).First() as ThSprinklerRoomExtractor;

                wallData.AddRange(architectureWallExtractor.Walls);
                wallData.AddRange(shearWallExtractor.Walls);
                Column.AddRange(columnExtractor.Columns);
                RoomIFCData.ForEach(x => roomData.Add(x.Boundary as Entity));

                Wall.AddRange(wallData.OfType<Polyline>());
                Wall.AddRange(wallData.OfType<MPolygon>().Select(x => x.Shell()));
                Wall.AddRange(wallData.OfType<MPolygon>().SelectMany(x => x.Holes()));

                Room.AddRange(roomData.OfType<Polyline>().Select(x => ThMPolygonTool.CreateMPolygon(x)));
                Room.AddRange(roomData.OfType<MPolygon>());

            }
        }

        public void ProcessLinePipe()
        {
            foreach (var item in LinePipeData)
            {
                if (item is Line l)
                {
                    Pipe.Add(l);
                }
                else if (item is Polyline pl)
                {
                    Pipe.AddRange(PolylineToLine(pl));
                }
            }
        }

        public void ProcessLinePipeText()
        {
            foreach (var item in LinePipeTextData)
            {
                Polyline geom = null;
                if (item is DBText dbtext)
                {
                    geom = dbtext.TextOBB();
                }
                else if (item is MText mtext)
                {
                    geom = mtext.TextOBB();
                }
                if (geom != null)
                {
                    PipeTextPl.Add(geom);
                }
            }
        }


        public void ProcessAxisCurve()
        {
            foreach (var item in AxisCurvesData)
            {
                if (item is Line l)
                {
                    AxisCurves.Add(l);
                }
                else if (item is Polyline pl)
                {
                    AxisCurves.AddRange(PolylineToLine(pl));
                }
            }
        }

        public static List<Line> PolylineToLine(Polyline pl)
        {
            var returnL = new List<Line>();
            var nCount = pl.NumberOfVertices - 1;
            if (pl.Closed == true)
            {
                nCount = nCount + 1;
            }
            for (int i = 0; i < nCount; i++)
            {
                returnL.Add(new Line(pl.GetPoint3dAt(i % pl.NumberOfVertices), pl.GetPoint3dAt((i + 1) % pl.NumberOfVertices)));
            }

            return returnL;
        }


        public void ProjectOntoXYPlane()
        {
            Pipe.ForEach(x => x.ProjectOntoXYPlane());
            PipeTextPl.ForEach(x => x.ProjectOntoXYPlane());
            SprinklerPt = SprinklerPt.Select(x => new Point3d(x.X, x.Y, 0)).ToList();
            Column.ForEach(x => x.ProjectOntoXYPlane());
            Wall.ForEach(x => x.ProjectOntoXYPlane());
            Room.ForEach(x => x.ProjectOntoXYPlane());
            AxisCurves.ForEach(x => x.ProjectOntoXYPlane());
        }
        public void Transform()
        {
            Pipe.ForEach(x => Transformer.Transform(x));
            PipeTextPl.ForEach(x => Transformer.Transform(x));
            SprinklerPt = SprinklerPt.Select(x => Transformer.Transform(x)).ToList();
            Column.ForEach(x => Transformer.Transform(x));
            Wall.ForEach(x => Transformer.Transform(x));
            Room.ForEach(x => Transformer.Transform(x));
            AxisCurves.ForEach(x => Transformer.Transform(x));

        }
        public void Print()
        {
            DrawUtils.ShowGeometry(Pipe, "l0Pipe", 140);
            DrawUtils.ShowGeometry(PipeTextPl, "l0pipeText", 140);
            SprinklerPt.ForEach(x => DrawUtils.ShowGeometry(x, "l0sprinkler", 3));

            DrawUtils.ShowGeometry(Wall, "l0wall", 1);
            DrawUtils.ShowGeometry(Column, "l0column", 3);
            Room.ForEach(x => DrawUtils.ShowGeometry(x, "l0room", 30));

            AxisCurves.ForEach(x => DrawUtils.ShowGeometry(x, "l0axis", 1));
        }

        public void CleanPreviousData()
        {
            PreviousData.ForEach(x => { x.UpgradeOpen(); x.Erase(); x.DowngradeOpen(); });

        }
    }
}
