using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using NFox.Cad;
using DotNetARX;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.AFASRegion;

using ThMEPLighting.IlluminationLighting.Interface;

namespace ThMEPLighting.IlluminationLighting.Data
{
    public class ThFaPlaceCoverageExtractor : ThExtractorBase, IPrint, ITransformer, IGroup, ISetStorey
    {
        #region input
        public List<ThIfcRoom> Rooms { get; set; } = new List<ThIfcRoom>();
        public List<ThIfcBeam> Beams { get; set; } = new List<ThIfcBeam>();
        public List<ThIfcColumn> Columns { get; set; } = new List<ThIfcColumn>();
        public List<ThIfcWall> Walls { get; set; } = new List<ThIfcWall>();
        public List<Polyline> Holes { get; set; } = new List<Polyline>();
        public bool ReferBeam { get; set; } = true;
        #endregion 


        public List<Entity> CanLayoutAreas { get; set; }
        public ThMEPOriginTransformer Transformer
        {
            get
            {
                return transformer;
            }
            set
            {
                transformer = value;
            }
        }

        public ThFaPlaceCoverageExtractor()
        {
            CanLayoutAreas = new List<Entity>();
            Category = "PlaceCoverage";
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            CanLayoutAreas.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var cmd = new AFASRegion();

            cmd.Rooms = Rooms;
            cmd.Beams = Beams.Cast<ThIfcBuildingElement>().ToList();
            cmd.Columns = Columns.Cast<ThIfcBuildingElement>().ToList();
            cmd.Walls = Walls.Cast<ThIfcBuildingElement>().ToList();
            cmd.Holes = Holes;
            cmd.BufferDistance = 500;
            cmd.ReferBeams = ReferBeam;

            //获取可布置区域
            var poly = pts.CreatePolyline();
            CanLayoutAreas = cmd.DivideRoomWithPlacementRegion(poly);
          //  CanLayoutAreas.ForEach(e => transformer.Transform(e)); //移动到原点，和之前所有的Extractor保持一致
        }

        public void Print(Database database)
        {
            CanLayoutAreas.CreateGroup(database, ColorIndex);
        }

        public void Reset()
        {
            Transformer.Reset(CanLayoutAreas.ToCollection());
        }

        public void Transform()
        {
            Transformer.Transform(CanLayoutAreas.ToCollection());
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
        }

        public void Set(List<ThStoreyInfo> storeyInfos)
        {
        }

        public ThStoreyInfo Query(Entity entity)
        {
            return null;
        }
    }
}
