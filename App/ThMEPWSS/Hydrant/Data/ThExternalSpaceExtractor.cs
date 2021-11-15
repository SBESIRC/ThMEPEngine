using System;
using NFox.Cad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPWSS.Hydrant.Service;
using ThMEPEngineCore.Interface;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.IO;
using ThMEPWSS.OutsideFrameRecognition;

namespace ThMEPWSS.Hydrant.Data
{
    public class ThExternalSpaceExtractor : ThExtractorBase, IPrint
    {
        /// <summary>
        /// 由建筑轮廓往外括形成的的空间
        /// </summary>
        public List<Entity> ExternalSpaces { get; set; }
        public double OffsetDis { get; set; }
        private List<Polyline> outsideFrames { get; set; }

        public ThExternalSpaceExtractor(List<Polyline> outsideFrames)
        {
            Category = BuiltInCategory.ExternalSpace.ToString();
            // 根据生成的房间外框线来构件ExternalSpace
            OffsetDis = 100000;
            ExternalSpaces = new List<Entity>();
            var mPolygon = ThRecogniseOutsideFrame.CreatMpolygonFromBoundaryToInfinity(outsideFrames, OffsetDis);
            ExternalSpaces.Add(mPolygon);
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            ExternalSpaces.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Print(Database database)
        {
            ExternalSpaces.CreateGroup(database, ColorIndex);
        }
    }
}
