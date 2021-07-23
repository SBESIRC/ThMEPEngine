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

namespace ThMEPWSS.Hydrant.Data
{
    public class ThExternalSpaceExtractor : ThExtractorBase, IPrint, IClean
    {
        /// <summary>
        /// 由建筑轮廓往外括形成的的空间
        /// </summary>
        public List<Entity> ExternalSpaces { get; set; }
        public double OffsetDis { get; set; }
        public double TESSELLATE_ARC_LENGTH { get; set; }
        public ThExternalSpaceExtractor()
        {
            OffsetDis = 100000;
            TESSELLATE_ARC_LENGTH = 500.0;
            ExternalSpaces = new List<Entity>();
            Category = BuiltInCategory.ExternalSpace.ToString();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var results = new DBObjectCollection();
            if (UseDb3Engine)
            {
                // 通过计算或指定的规则获取
                // TODO
            }
            else
            {
                IArchitectureOutlineData datas = new ThQueryArchitectureOutlineService()
                {
                    ElementLayer = ElementLayer,
                };
                var outlines = datas.Query(database, pts);
                results = Clean(outlines.ToCollection());
            }

            IBuffer iBuffer = new ThNTSBufferService();
            results.Cast<Entity>().ForEach(o =>
            {
                var bufferEnt = iBuffer.Buffer(o, OffsetDis);
                if (bufferEnt is Polyline shell)
                {
                    var mPolygon = ThMPolygonTool.CreateMPolygon(shell, new List<Curve> { o.Clone() as Polyline });
                    ExternalSpaces.Add(mPolygon);
                }
                else if (bufferEnt is MPolygon mPolygon)
                {
                    throw new NotSupportedException();
                }
            });
            if (FilterMode == FilterMode.Window)
            {
                ExternalSpaces = FilterWindowPolygon(pts, ExternalSpaces);
            }
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

        public DBObjectCollection Clean(DBObjectCollection objs)
        {
            var simplifier = new ThElementSimplifier()
            {
                TESSELLATE_ARC_LENGTH = TESSELLATE_ARC_LENGTH,
            };
            var results = simplifier.Tessellate(objs);
            results = simplifier.Simplify(objs);
            results = simplifier.Normalize(objs);
            return results;
        }
    }
}
