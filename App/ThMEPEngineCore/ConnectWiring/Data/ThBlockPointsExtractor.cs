using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.ConnectWiring.Data
{
    class ThBlockPointsExtractor : ThExtractorBase
    {
        public List<Point3d> blockPts { get; set; }
        public List<BlockReference> resBlocks { get; set; }
        List<string> configBlockd;
        public ThBlockPointsExtractor(List<string> blockNames)
        {
            configBlockd = blockNames;
            Category = BuiltInCategory.WiringPosition.ToString();
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            blockPts.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Boundary = new DBPoint(o);
                geos.Add(geometry);
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filterlist = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.BlockName) == string.Join(",", configBlockd) &
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                var blocks = new List<Entity>();
                var status = Active.Editor.SelectAll(filterlist);
                if (status.Status == PromptStatus.OK)
                {
                    foreach (ObjectId obj in status.Value.GetObjectIds())
                    {
                        blocks.Add(acadDatabase.Element<Entity>(obj));
                    }
                }
                var pline = new Polyline()
                {
                    Closed = true,
                };
                pline.CreatePolyline(pts);
                resBlocks = new List<BlockReference>();
                blocks.Where(o =>
                {
                    var geoPts = o.GeometricExtents;
                    var position = new Point3d((geoPts.MinPoint.X + geoPts.MaxPoint.X) / 2, (geoPts.MinPoint.Y + geoPts.MaxPoint.Y) / 2, 0);
                    return pline.Contains(position);
                })
                .Cast<BlockReference>()
                .ForEachDbObject(o => resBlocks.Add(o));
            }
        }
    }
}
