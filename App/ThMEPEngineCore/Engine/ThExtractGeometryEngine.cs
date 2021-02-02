using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPEngineCore.Engine
{
    public class ThExtractGeometryEngine : IDisposable
    {
        public List<Polyline> Obstructs { get; private set; }
        public List<Polyline> Spaces { get; private set; }
        public List<Polyline> Doors { get; private set; }
        public Dictionary<string, List<Polyline>> Equipments {get; private set;}
        private double ArcLength { get; set; }
        public ThExtractGeometryEngine()
        {
            Spaces = new List<Polyline>();
            Doors = new List<Polyline>();
            Obstructs = new List<Polyline>();
            Equipments = new Dictionary<string, List<Polyline>>();
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
                return doors;
            }
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
