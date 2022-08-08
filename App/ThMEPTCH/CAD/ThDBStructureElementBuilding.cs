using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.AFASRegion.Utls;
using ThMEPEngineCore.Engine;
using ThMEPTCH.TCHArchDataConvert.THStructureEntity;

namespace ThMEPTCH.CAD
{
    public class ThDBStructureElementBuilding
    {
        public List<THStructureEntity> BuildingFromMS(Database database)
        {
            var result = new List<THStructureEntity>();

            var engine = new ThDBStructureElementExtractionEngine();
            engine.ExtractFromMS(database);

            var beamLines = new List<Line>();
            var beamMarks = new List<THStructureDBText>();
            var slabLines = new List<Polyline>();
            var slabHatchLines = new List<Polyline>();
            var slabHoleHatchLines = new List<Polyline>();
            var slabBTHs = new List<THStructureSlabBTH>();
            foreach (var element in engine.Results)
            {
                if (element.Data is THStructureWall wall)
                {
                    result.Add(wall);
                }
                else if (element.Data is THStructureColumn column)
                {
                    result.Add(column);
                }
                else if (element.Data is THStructureBeam)
                {
                    beamLines.Add(element.Geometry as Line);
                }
                else if (element.Data is THStructureDBText text)
                {
                    if (text.TextType == DBTextType.BeamText)
                    {
                        if(beamMarks.Any(o => o.Point.DistanceTo(text.Point) < 10))
                        {
                            continue;
                        }
                        beamMarks.Add(text);
                    }
                }
                else if (element.Data is THStructureSlabPL slabPL)
                {
                    if (slabPL.slabPLType == SlabType.Slab)
                    {
                        slabLines.Add(slabPL.Outline);
                    }
                }
                else if (element.Data is THStructureSlabHatch slabHatch)
                {
                    if (slabHatch.slabPLType == SlabType.Slab)
                    {
                        slabHatchLines.Add(slabHatch.Outline);
                    }
                    else
                    {
                        slabHoleHatchLines.Add(slabHatch.Outline);
                    }
                }
                else if (element.Data is THStructureSlabBTH bth)
                {
                    slabBTHs.Add(bth);
                }
                else if (element.Data is THStructureSlab slab)
                {
                    //...
                }
                else
                {
                    //...
                }
            }
            

            var beams = BuildBeam(beamLines, beamMarks);

            var slabs = BuildSlab(beams, result.Select(o => o.Outline).ToList(), slabLines, slabHatchLines, slabHoleHatchLines, slabBTHs);

            //using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
            //{
            //    foreach (var beam in beams)
            //    {
            //        beam.Outline.ColorIndex = 2;
            //        acad.ModelSpace.Add(beam.Outline);
            //    }
            //}
            result.AddRange(beams);
            result.AddRange(slabs);

            return result;
        }

        private List<THStructureSlab> BuildSlab(List<THStructureBeam> beams, List<Polyline> structs, List<Polyline> SlabPLs, List<Polyline> slabHatchLines, List<Polyline> slabHoleHatchLines, List<THStructureSlabBTH> slabBTHs)
        {
            List<THStructureSlab> result = new List<THStructureSlab>();
            var OptimizeBeamLines = beams.SelectMany(o => CreateBeamLines(o)).SelectMany(o => CreateOptimizeBeamLine(o));
            var objs = OptimizeBeamLines.Union(structs).Union(SlabPLs).ToCollection();
            List<Polyline> areas = objs.PolygonsEx().Cast<Entity>().OfType<Polyline>().Where(o => o.Area > 100000).ToList();
            var inwardAreas = areas.SelectMany(o => o.Buffer(-10).OfType<Polyline>());
            var slabBTHDic = slabBTHs.ToDictionary(key => new DBPoint(key.Point), value => value);
            var slabBTHSpatialIndex = new ThCADCoreNTSSpatialIndex(slabBTHDic.Keys.ToCollection());

            List<Polyline> buildSpace = new List<Polyline>();
            var beamSpace = beams.Select(o => o.Outline);
            foreach (var polyline in inwardAreas)
            {
                if (polyline.Area <= 100)
                {
                    continue;
                }
                if (structs.Any(o => o.Contains(polyline)))
                {
                    //过滤掉墙，柱
                    continue;
                }
                else if (beamSpace.Any(o => o.Contains(polyline)))
                {
                    //过滤掉梁内区间
                    continue;
                }
                else if (slabHoleHatchLines.Any(o => o.Contains(polyline)))
                {
                    //过滤掉 Hatch'洞'
                    continue;
                }
                else
                {
                    var slabSpace = polyline.Buffer(10)[0] as Polyline;
                    var slab = new THStructureSlab();
                    slab.Outline = slabSpace;
                    if (slabHatchLines.Any(o => o.Contains(polyline)))
                    {
                        slab.RelativeBG = -0.50 * 1000;
                    }
                    else
                    {
                        slab.RelativeBG = 0;
                    }
                    var bthObjs = new DBObjectCollection();
                    if ((bthObjs = slabBTHSpatialIndex.SelectWindowPolygon(slabSpace)).Count > 0)
                    {
                        slab.Height = slabBTHDic[bthObjs[0] as DBPoint].Height;
                    }
                    else
                    {
                        slab.Height = 100;
                    }
                    result.Add(slab);
                }
            }
            return result;
        }
        private List<THStructureBeam> BuildBeam(List<Line> lines, List<THStructureDBText> beamMarks)
        {
            List<THStructureBeam> result = new List<THStructureBeam>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines.ToCollection());
            var beamInfoQueue = new Queue<THStructureDBText>(beamMarks.Where(o => o.Content.Contains('x')));
            var beamBGInfos = beamMarks.Where(o => o.Content.Contains("BG"));
            var beamBGInfoDic = beamBGInfos.ToDictionary(key => key.Point.TransformBy(Matrix3d.Displacement(key.Vector * 250)), value => value);
            var markedBeams = new List<Line>();
            while (beamInfoQueue.Any())
            {
                var beamInfo = beamInfoQueue.Dequeue();
                Matrix3d matrix = Matrix3d.Displacement(beamInfo.Vector * 150);
                var beamBGInfo = beamBGInfoDic.FirstOrDefault(o => o.Key.DistanceTo(beamInfo.Point) < 300).Value;
                var objs = new DBObjectCollection();
                if (beamBGInfo.IsNull())
                {
                    var pl = beamInfo.Outline.BufferPL(150)[0] as Polyline;
                    objs = spatialIndex.SelectCrossingPolygon(pl);
                }
                else
                {
                    var pl1 = beamInfo.Outline.BufferPL(150)[0] as Polyline;
                    var pl2 = beamBGInfo.Outline.BufferPL(150)[0] as Polyline;
                    objs = spatialIndex.SelectCrossingPolygon(pl1).Union(spatialIndex.SelectCrossingPolygon(pl2));
                }

                //计算标记线
                var markLines = objs.OfType<Line>().Where(o => !markedBeams.Contains(o) && o.CurveDirection().IsPerpendicularTo(beamInfo.Vector, new Tolerance(1e-2, 1e-2)));
                if (markLines.Any())
                {
                    if (markLines.Count() == 1)
                    {
                        var line = markLines.First();
                        var beamContents = beamInfo.Content.Split('x');
                        double.TryParse(beamContents[0],out double beamWidth);
                        double.TryParse(beamContents[1], out double beamHeight);
                        var pl = line.ExtendLine(-100).Buffer(beamWidth + 15);
                        var otherLineObjs = spatialIndex.SelectCrossingPolygon(pl);
                        var otherLine = otherLineObjs.OfType<Line>().Where(o => IsParallelLine(o, line, 1.0) && Math.Abs(o.Distance(line) - beamWidth) <= 10).OrderByDescending(o => o.Length).FirstOrDefault();
                        if (!otherLine.IsNull())
                        {
                            var miniRectangle = new DBObjectCollection() { line, otherLine }.GetMinimumRectangle();
                            markedBeams.Add(line);
                            markedBeams.Add(otherLine);
                            THStructureBeam structureBeam = new THStructureBeam();
                            structureBeam.Outline = miniRectangle;
                            structureBeam.Height = beamHeight;
                            structureBeam.RelativeBG = beamBGInfo.IsNull() ? 0.0 : (beamBGInfo.Content.Contains('-') ? -1 : 1) * double.Parse(beamBGInfo.Content.Substring(4, beamBGInfo.Content.Length - 5)) * 1000;
                            structureBeam.Uuid = line.Handle.Value;
                            result.Add(structureBeam);
                        }
                        else
                        {
                            markedBeams.Add(line);
                        }
                    }
                    else
                    {
                        beamInfoQueue.Enqueue(beamInfo);
                    }
                }

            }
            return result;
        }

        private bool IsParallelLine(Line a, Line b, double degreetol = 1)
        {
            double angle = Math.Abs(CreateVector(a).GetAngleTo(CreateVector(b)));
            return Math.Min(angle, Math.Abs(Math.PI - angle)) / Math.PI * 180 < degreetol;
        }
        private Vector3d CreateVector(Line line)
        {
            return CreateVector(line.StartPoint, line.EndPoint);
        }
        private Vector3d CreateVector(Point3d ps, Point3d pe)
        {
            return new Vector3d(pe.X - ps.X, pe.Y - ps.Y, pe.Z - ps.Z);
        }

        private List<Line> CreateBeamLines(THStructureBeam beam)
        {
            var lines = beam.Outline.GetAllLinesInPolyline();
            return lines.OrderByDescending(o => o.Length).Take(4).ToList();
        }

        private List<Entity> CreateOptimizeBeamLine(Line beamLine)
        {
            var result = new List<Entity>();
            var lineExtend = beamLine.ExtendLine(10.0);
            result.Add(lineExtend);
            result.AddRange(CreateHook(lineExtend));
            return result;
        }

        private List<Line> CreateHook(Line line)
        {
            var result = new List<Line>();
            var dir = line.LineDirection().RotateBy(Math.PI / 2.0, Vector3d.ZAxis);
            result.Add(new Line(line.StartPoint + dir * 5, line.StartPoint - dir * 5));
            result.Add(new Line(line.EndPoint + dir * 5, line.EndPoint - dir * 5));
            return result;
        }
    }
}
