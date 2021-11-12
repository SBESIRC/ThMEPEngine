using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using NFox.Cad;
using Linq2Acad;

using ThCADExtension;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;

using ThMEPLighting.IlluminationLighting.Common;

namespace ThMEPLighting.IlluminationLighting.Data
{
    class ThIlluminationLayoutBusinessDataSetFactory: ThMEPDataSetFactory
    {
        public List<string> BlkNameList { get; set; }
        private List<ThGeometry> Geos { get; set; }
        public ThIlluminationLayoutBusinessDataSetFactory()
        {
            Geos = new List<ThGeometry>();
        }
        protected override void GetElements(Database database, Point3dCollection collection)
        {
            // ArchitectureWall、Shearwall、Column、Room、Hole
            UpdateTransformer(collection);

            var extractors = new List<ThExtractorBase>()
            {
                new ThIlluminationBlkExtractor()
                {
                    Transformer = Transformer ,
                    BlkNameList = this.BlkNameList, //add needed all blk name string 
                }
            };

            extractors.ForEach(o => o.Extract(database, collection));
            //收集数据
            extractors.ForEach(o => Geos.AddRange(o.BuildGeometries()));
            // 移回原位
            extractors.ForEach(o =>
            {
                if (o is ITransformer iTransformer)
                {
                    iTransformer.Reset();
                }
            });

            ThIlluminationUtils.MoveToXYPlane(Geos);
        }

        protected override ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet()
            {
                Container = Geos,
            };
        }


    }
}
