using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;
using ThMEPLighting.ServiceModels;

namespace ThMEPLighting.ParkingStall.Business.Block
{

    public class BlockInsertor
    {
        private List<LightPlaceInfo> m_LightPlaceInfos;

        public BlockInsertor(List<LightPlaceInfo> lightPlaceInfos)
        {
            m_LightPlaceInfos = lightPlaceInfos;
        }

        /// <summary>
        /// 插入块
        /// </summary>
        /// <param name="insertPts"></param>
        /// <param name="sensorType"></param>
        public static void MakeBlockInsert(List<LightPlaceInfo> lightPlaceInfos)
        {
            var blockInsertor = new BlockInsertor(lightPlaceInfos);

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
                    lightInfo.InsertBlockId = db.Database.InsertModel(ParkingStallCommon.PARK_LIGHT_LAYER, ParkingStallCommon.PARK_LIGHT_BLOCK_NAME, lightInfo.Position, scale, lightInfo.Angle);
                }
            }
        }
    }
}
