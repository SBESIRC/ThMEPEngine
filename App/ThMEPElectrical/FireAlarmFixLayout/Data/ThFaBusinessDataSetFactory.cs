using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPElectrical.FireAlarm.Data;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.Data;
using ThMEPElectrical.AFAS.Interface;

namespace ThMEPElectrical.FireAlarm.Data
{
    class ThFaBusinessDataSetFactory : ThMEPDataSetFactory
    {
        #region input
        public List<string> BlkNameList { get; set; }
        public List<ThStoreyInfo> StoreyInfos { get; set; }
        public Dictionary<Entity, string> FireApartIds { get;  set; }

        #endregion

        private List<ThGeometry> Geos { get; set; }

        public ThFaBusinessDataSetFactory()
        {
            Geos = new List<ThGeometry>();
        }

        protected override void GetElements(Database database, Point3dCollection collection)
        {
            // ArchitectureWall、Shearwall、Column、Room、Hole
            UpdateTransformer(collection);

            var extractors = new List<ThExtractorBase>()
            {
                new ThFireAlarmBlkExtractor ()
                {
                    Transformer = Transformer ,
                    BlkNameList = this.BlkNameList, //add needed all blk name string 
                }
            };
            extractors.ForEach(o => o.Extract(database, collection));
            
            extractors.ForEach(o =>
            {
                if (o is ISetStorey iStorey)
                {
                    iStorey.Set(StoreyInfos);
                }
            });

            extractors.ForEach(o =>
            {
                if (o is IGroup group)
                {
                    group.Group(FireApartIds);
                }
            });

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

            Geos.MoveToXYPlane();
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
