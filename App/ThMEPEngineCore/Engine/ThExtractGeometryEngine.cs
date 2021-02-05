using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Linq2Acad;
using NFox.Cad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThExtractGeometryEngine : IDisposable
    {
        public List<Polyline> Obstructs { get; private set; }
        public List<Polyline> Spaces { get; private set; }
        public List<Polyline> Doors { get; private set; }
        public Dictionary<string, List<Polyline>> Equipments {get; private set;}
        public Dictionary<Polyline, string> ConnectPorts { get; private set; }
        private double ArcLength { get; set; }
        public ThExtractGeometryEngine()
        {
            Spaces = new List<Polyline>();
            Doors = new List<Polyline>();
            Obstructs = new List<Polyline>();
            Equipments = new Dictionary<string, List<Polyline>>();
            ConnectPorts = new Dictionary<Polyline, string>();
        }
        public void Dispose()
        {            
        }
        public void Extract(Database database,double arcLength=50.0)
        {
            ArcLength = arcLength;
            Spaces = BuildSpaces(database);
            Doors = BuildDoors(database);
            Obstructs = BuildObstructs(database);
            Equipments = BuildEquipments(database);
            ConnectPorts = BuildConnectPorts(database);
        }
        private List<Polyline> BuildSpaces(Database HostDb)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                var spaces = new List<Polyline>();
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is Polyline polyline)
                    {
                        if(IsSpaceLayer(polyline.Layer))
                        {
                            var newPolyline = polyline.Clone() as Polyline;
                            spaces.Add(newPolyline.TessellatePolylineWithArc(ArcLength));
                        }
                    }
                }
                return spaces;
            }
        }
        private bool IsSpaceLayer(string layerName)
        {
            return layerName.ToUpper() == "AD-AREA-OUTL";
        }
        private Dictionary<Polyline, string> BuildConnectPorts(Database HostDb)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                var results = new Dictionary<Polyline, string>();
                var boundaries = new List<Polyline>();
                var texts = new List<Entity>();
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if(IsConnectPortLayer(ent.Layer))
                    {
                        if (ent is Polyline polyline)
                        {
                            var newPolyline = polyline.Clone() as Polyline;
                            boundaries.Add(newPolyline.TessellatePolylineWithArc(ArcLength));
                        }
                        else if(ent is DBText dbText)
                        {
                            texts.Add(dbText);
                        }
                        else if(ent is MText mText)
                        {
                            texts.Add(mText);
                        }
                    }                    
                }
                var textSpatialIndex = new ThCADCoreNTSSpatialIndex(texts.ToCollection());
                boundaries.ForEach(o =>
                {
                   var selObjs = textSpatialIndex.SelectCrossingPolygon(o);
                   foreach(var item in selObjs)
                    {
                        if(item is DBText dbText)
                        {
                            if(ValidateText(dbText.TextString))
                            {
                                results.Add(o, dbText.TextString);
                                break;
                            }                            
                        }
                        else if(item is MText mText)
                        {
                            if (ValidateText(mText.Contents))
                            {
                                results.Add(o, mText.Contents);
                                break;
                            }
                        }
                    }
                });
                return results;
            }
        }
        private bool ValidateText(string content)
        {
            string pattern = @"^[\d]+\s{0,}[A-Z]{1,}[\d]+";
            return Regex.IsMatch(content, pattern);
        }
        private bool IsConnectPortLayer(string layerName)
        {
            return layerName.ToUpper() == "连通";
        }
        private List<Polyline> BuildDoors(Database HostDb)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                var doors = new List<Polyline>();
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is Polyline polyline)
                    {
                        if (IsDoorLayer(polyline.Layer))
                        {
                            var newPolyline = polyline.Clone() as Polyline;
                            doors.Add(newPolyline.TessellatePolylineWithArc(ArcLength));
                        }
                    }
                }
                for (int i = 1; i <= doors.Count; i++)
                {
                    var obb = doors[i - 1].GetMinimumRectangle();
                    var rotatePts = DoorRotateAixPts(obb);
                    if (rotatePts.Count > 0)
                    {
                        var mt = Matrix3d.Rotation(Math.PI / 2.0 * Math.Pow(-1, i), Vector3d.ZAxis, rotatePts[0]);
                        doors[i - 1].TransformBy(mt);
                    }
                }
                return doors;
            }
        }
        private List<Point3d> DoorRotateAixPts(Polyline polyline)
        {
            var results = new List<Point3d>();
            var lines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                var segment = polyline.GetLineSegmentAt(i);
                if (segment.Length > 5.0)
                {
                    lines.Add(new Line(segment.StartPoint, segment.EndPoint));
                }
            }
            lines = lines.OrderBy(o => o.Length).ToList();
            results.Add(lines[0].StartPoint.GetMidPt(lines[0].EndPoint));
            results.Add(lines[1].StartPoint.GetMidPt(lines[1].EndPoint));
            return results;
        }
        private List<Polyline> BuildObstructs(Database HostDb)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                var obstructs = new List<Polyline>();
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is Polyline polyline)
                    {
                        if (IsColumnLayer(polyline.Layer))
                        {
                            var newPolyline = polyline.Clone() as Polyline;
                            obstructs.Add(newPolyline.TessellatePolylineWithArc(ArcLength));
                        }
                    }
                    else if (ent is Circle circle)
                    {
                        if (IsColumnLayer(circle.Layer))
                        {
                            obstructs.Add(circle.Tessellate(100));
                        }
                    }
                }
                return obstructs;
            }
        }
        private bool IsDoorLayer(string layerName)
        {
            return layerName == "门";
        }
        private Dictionary<string,List<Polyline>> BuildEquipments(Database HostDb)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                var spaces = new Dictionary<string, List<Polyline>>();
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is BlockReference br && br.Bounds!=null)
                    {
                        var name = br.GetEffectiveName();
                        if (IsFireHydrantBlkName(name))
                        {
                            var obb = GetBlockOBB(HostDb, br, br.BlockTransform);
                            if (spaces.ContainsKey(name))
                            {
                                spaces[name].Add(obb);
                            }
                            else
                            {
                                spaces.Add(name, new List<Polyline> { obb });
                            }
                        }
                    }
                }
                return spaces;
            }
        }
        private bool IsFireHydrantBlkName(string blkName)
        {
            string queryChars = "-新";
            int index = blkName.LastIndexOf(queryChars);
            return index>=0?index + queryChars.Length== blkName.Length:false;
        }
        private Polyline GetBlockOBB(Database database , BlockReference blockObj, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var btr = acadDatabase.Blocks.Element(blockObj.BlockTableRecord);
                var polyline = btr.GeometricExtents().ToRectangle().GetTransformedCopy(matrix) as Polyline;
                return polyline;
            }
        }
        private bool IsColumnLayer(string layerName)
        {
            return layerName.ToUpper() == "柱";
        }
    }
}
