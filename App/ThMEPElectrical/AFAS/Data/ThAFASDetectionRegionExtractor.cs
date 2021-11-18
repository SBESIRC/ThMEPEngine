using NFox.Cad;
using DotNetARX;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.AFASRegion;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPElectrical.AFAS.Interface;

namespace ThMEPElectrical.AFAS.Data
{
    public class ThAFASDetectionRegionExtractor : ThExtractorBase, IPrint, ITransformer, IGroup, ISetStorey
    {
        #region input
        public List<ThIfcRoom> Rooms { get; set; } = new List<ThIfcRoom>();
        public List<ThIfcBeam> Beams { get; set; } = new List<ThIfcBeam>();
        public List<ThIfcColumn> Columns { get; set; } = new List<ThIfcColumn>();
        public List<ThIfcWall> Walls { get; set; } = new List<ThIfcWall>();
        public List<Polyline> Holes { get; set; } = new List<Polyline>();

        public double WallThickness = 100;
        #endregion 


        public List<Entity> DetectionRegion { get; set; }
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

        public ThAFASDetectionRegionExtractor()
        {
            DetectionRegion = new List<Entity>();
            Category = "DetectionRegion";
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var tol = 100;//10cm2

            var geos = new List<ThGeometry>();
            DetectionRegion.ForEach(o =>
            {
                if (o.GetArea() > tol)
                {
                    var geometry = new ThGeometry();
                    geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                    geometry.Boundary = o;
                    geos.Add(geometry);
                }

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

            cmd.WallThickness = WallThickness;

            //获取探测区域
            var poly = pts.CreatePolyline();
            DetectionRegion = cmd.DivideRoomWithDetectionRegion (poly);
            //  CanLayoutAreas.ForEach(e => transformer.Transform(e)); //移动到原点，和之前所有的Extractor保持一致
        }

        public void Print(Database database)
        {
            DetectionRegion.CreateGroup(database, ColorIndex);
        }

        public void Reset()
        {
            Transformer.Reset(DetectionRegion.ToCollection());
        }

        public void Transform()
        {
            Transformer.Transform(DetectionRegion.ToCollection());
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
