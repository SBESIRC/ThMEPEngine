using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPElectrical.AFAS.Service;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.Interface;


namespace ThMEPElectrical.AFAS.Data
{
    public class ThAFASBusinessDataSetFactory : ThMEPDataSetFactory
    {
        #region input
        public List<string> BlkNameList { get; set; }
        public List<ThExtractorBase> InputExtractors { get; set; }
        #endregion

        private List<ThGeometry> Geos { get; set; }

        public ThAFASBusinessDataSetFactory()
        {
            Geos = new List<ThGeometry>();

        }
        public void SetTransformer(ThMEPOriginTransformer Transformer)
        {
            this.Transformer = Transformer;
        }
        protected override void GetElements(Database database, Point3dCollection collection)
        {
            ThAFASEStoreyExtractor storeyExtractor = null;
            ThAFASFireCompartmentExtractor fireApartExtractor = null;

            var blkExtractor = new ThFireAlarmBlkExtractor()
            {
                Transformer = Transformer,
                BlkNameList = this.BlkNameList, //add needed all blk name string 
            };

            blkExtractor.Extract(database, collection);

            /////楼层框线。防火分区//////
            if (InputExtractors != null)
            {
                storeyExtractor = InputExtractors.Where(o => o is ThAFASEStoreyExtractor).FirstOrDefault() as ThAFASEStoreyExtractor;
                fireApartExtractor = InputExtractors.Where(o => o is ThAFASFireCompartmentExtractor).FirstOrDefault() as ThAFASFireCompartmentExtractor;
                if (storeyExtractor != null)
                {
                    fireApartExtractor.Transform();
                    storeyExtractor.Transform();
                    var storeyInfos = storeyExtractor.Storeys.Cast<ThStoreyInfo>().ToList();
                    var fireApartIds = fireApartExtractor.FireApartIds;

                    blkExtractor.Set(storeyInfos);
                    blkExtractor.Group(fireApartIds);
                }
            }

            //收集数据
            Geos.AddRange(blkExtractor.BuildGeometries());

            // 移回原位
            if (storeyExtractor != null)
            {
                fireApartExtractor.Reset();
                storeyExtractor.Reset();
            }

            blkExtractor.Reset();
            Geos.ProjectOntoXYPlane();
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
