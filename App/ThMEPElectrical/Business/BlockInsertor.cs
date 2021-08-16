using Linq2Acad;
using ThMEPElectrical.CAD;
using ThMEPElectrical.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

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
        private double m_scale;
        private double m_angle;
        public BlockInsertor(List<Point3d> insertPts, SensorType sensorType, double angle = 0,double scale=100)
        {
            InsertPts = insertPts;
            Sensor = sensorType;
            m_angle = angle;
            m_scale = scale;
        }

        /// <summary>
        /// 插入块
        /// </summary>
        /// <param name="insertPts"></param>
        /// <param name="sensorType"></param>
        public static void MakeBlockInsert(List<Point3d> insertPts, SensorType sensorType, double angle = 0, double scale = 100)
        {
            var blockInsertor = new BlockInsertor(insertPts, sensorType, angle, scale);

            blockInsertor.Do();
        }

        public void Do()
        {
            // 导入块信息
            using (var db = AcadDatabase.Active())
            {
                if (Sensor == SensorType.SMOKESENSOR)
                {
                    db.Database.ImportModel(ThMEPCommon.SMOKE_SENSOR_BLOCK_NAME, ThMEPCommon.SENSORLAYERNMAE);
                    InsertPts.ForEach(o =>
                    {
                        db.Database.InsertModel(
                            ThMEPCommon.SENSORLAYERNMAE, 
                            ThMEPCommon.SMOKE_SENSOR_BLOCK_NAME, o, new Scale3d(m_scale), m_angle);
                    });
                }
                else
                {
                    db.Database.ImportModel(ThMEPCommon.TEMPERATURE_SENSOR_BLOCK_NAME, ThMEPCommon.SENSORLAYERNMAE);
                    InsertPts.ForEach(o =>
                    {
                        db.Database.InsertModel(
                            ThMEPCommon.SENSORLAYERNMAE, 
                            ThMEPCommon.TEMPERATURE_SENSOR_BLOCK_NAME, o, new Scale3d(m_scale), m_angle);
                    });
                }
            }
        }
    }
}
