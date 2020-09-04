using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.Block;
using Linq2Acad;

namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 根据插入点的位置和传感器类型进行插入工作
    /// </summary>
    public class BlockInsertor
    {
        public List<Point3d> InsertPts
        {
            get;
            private set;
        }

        public SensorType Sensor
        {
            get;
            private set;
        }

        public BlockInsertor(List<Point3d> insertPts, SensorType sensorType)
        {
            InsertPts = insertPts;
            Sensor = sensorType;
        }

        /// <summary>
        /// 插入块
        /// </summary>
        /// <param name="insertPts"></param>
        /// <param name="sensorType"></param>
        public static void MakeBlockInsert(List<Point3d> insertPts, SensorType sensorType)
        {
            var blockInsertor = new BlockInsertor(insertPts, sensorType);
            blockInsertor.Do();
        }

        public void Do()
        {
            // 导入块信息
            using (var db = AcadDatabase.Active())
            {
                if (Sensor == SensorType.SMOKESENSOR)
                {
                    db.Database.ImportModel(BlockInsertDBExtension.SMOKE_SENSOR_BLOCK_NAME);
                    db.Database.InsertModel(InsertPts, BlockInsertDBExtension.SMOKE_SENSOR_BLOCK_NAME, new Scale3d(100, 100, 100));
                }
                else
                {
                    db.Database.ImportModel(BlockInsertDBExtension.TEMPERATURE_SENSOR_BLOCK_NAME);
                    db.Database.InsertModel(InsertPts, BlockInsertDBExtension.TEMPERATURE_SENSOR_BLOCK_NAME, new Scale3d(100, 100, 100));
                }
            }
        }
    }
}
