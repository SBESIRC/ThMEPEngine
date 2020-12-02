﻿using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.CAD;
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

        private double m_angle;
        public BlockInsertor(List<Point3d> insertPts, SensorType sensorType, double angle = 0)
        {
            InsertPts = insertPts;
            Sensor = sensorType;
            m_angle = angle;
        }

        /// <summary>
        /// 插入块
        /// </summary>
        /// <param name="insertPts"></param>
        /// <param name="sensorType"></param>
        public static void MakeBlockInsert(List<Point3d> insertPts, SensorType sensorType, double angle = 0)
        {
            var blockInsertor = new BlockInsertor(insertPts, sensorType, angle);

            blockInsertor.Do();
        }

        public void Do()
        {
            // 导入块信息
            using (var db = AcadDatabase.Active())
            {
                if (Sensor == SensorType.SMOKESENSOR)
                {
                    db.Database.ImportModel(ThMEPCommon.SMOKE_SENSOR_BLOCK_NAME);
                    db.Database.InsertModel(InsertPts, ThMEPCommon.SMOKE_SENSOR_BLOCK_NAME, ThMEPCommon.BlockScale, m_angle);
                }
                else
                {
                    db.Database.ImportModel(ThMEPCommon.TEMPERATURE_SENSOR_BLOCK_NAME);
                    db.Database.InsertModel(InsertPts, ThMEPCommon.TEMPERATURE_SENSOR_BLOCK_NAME, ThMEPCommon.BlockScale, m_angle);
                }
            }
        }
    }
}
