using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System.Collections.Generic;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.ParkingStall.Model;
using ThMEPLighting.ServiceModels;

namespace ThMEPLighting.ParkingStall.Business.Block
{

    public class BlockInsertor
    {
        private List<LightPlaceInfo> m_LightPlaceInfos;
        private ThMEPOriginTransformer m_OriginTransformer;
        public BlockInsertor(List<LightPlaceInfo> lightPlaceInfos, ThMEPOriginTransformer originTransformer)
        {
            m_LightPlaceInfos = lightPlaceInfos;
            m_OriginTransformer = originTransformer;
        }

        /// <summary>
        /// 插入块
        /// </summary>
        /// <param name="insertPts"></param>
        /// <param name="sensorType"></param>
        public static void MakeBlockInsert(List<LightPlaceInfo> lightPlaceInfos, ThMEPOriginTransformer originTransformer =null)
        {
            var blockInsertor = new BlockInsertor(lightPlaceInfos, originTransformer);

            blockInsertor.Do();
        }

        public void Do()
        {
            double scaleNum = ThParkingStallService.Instance.BlockScale;
            Scale3d scale = new Scale3d(scaleNum, scaleNum, scaleNum);
            // 导入块信息
            using (var db = AcadDatabase.Active())
            {
                db.Database.ImportModel(ParkingStallCommon.PARK_LIGHT_BLOCK_NAME, ParkingStallCommon.PARK_LIGHT_LAYER);
                foreach (var lightInfo in m_LightPlaceInfos)
                {
                    foreach (var point in lightInfo.InsertLightPosisions) 
                    {
                        var position = point;
                        if (null != m_OriginTransformer)
                            position = m_OriginTransformer.Reset(position);
                        var id = db.Database.InsertModel(
                            ParkingStallCommon.PARK_LIGHT_LAYER,
                            ParkingStallCommon.PARK_LIGHT_BLOCK_NAME,
                            position,
                            scale,
                            lightInfo.Angle);
                        if (null == id || !id.IsValid)
                            continue;
                        lightInfo.InsertLightBlockIds.Add(id);
                    }
                    
                }
            }
        }
    }
}
