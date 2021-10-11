using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPLighting.IlluminationLighting.Data
{
    class ThBlkExtractor : ThExtractorBase, ITransformer
    {
        public Dictionary<BlockReference, Polyline> Equipment { get; private set; } //key:origin blkreference, value: blk postition dbpoint
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
        public List<string> BlkNameList { get; set; }
        public ThBlkExtractor()
        {
            Category = BuiltInCategory.Distribution.ToString();
            Equipment = new Dictionary<BlockReference, Polyline>();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Equipment.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, o.Key.GetEffectiveName());
                geometry.Properties.Add(ThExtractorPropertyNameManager.HandlerPropertyName, o.Key.Handle.ToString());
                geometry.Boundary = o.Value;
                geos.Add(geometry);
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            foreach (var blkName in BlkNameList)
            {
                var extractService = new ThExtractBlockReferenceService()
                {
                    //  ElementLayer = this.ElementLayer,
                    BlockName = blkName,
                };
                extractService.Extract(database, pts);
              
                extractService.Blocks.ForEach(x =>
                {
                    var obb = x.ToOBB(x.BlockTransform);
                   
                    if (obb != null && obb.Area > 1.0)
                    {
                        //var bufferObb = obb.GetOffsetCurves(15).Cast<Polyline>().OrderByDescending(y => y.Area).FirstOrDefault();
                        var bufferObb = obb.GetOffsetClosePolyline(15);
                        if (bufferObb != null)
                        {
                            Equipment.Add(x, bufferObb);
                        }
                    }
                });
            }
            //不加就是原位置。加就是靠近远点。
            Transform();
        }

        public void Transform()
        {
            Transformer.Transform(Equipment.Values.ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(Equipment.Values.ToCollection());
        }

    }
}
