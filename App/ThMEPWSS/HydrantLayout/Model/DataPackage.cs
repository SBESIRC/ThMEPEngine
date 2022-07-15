using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using ThCADCore.NTS;
using ThMEPWSS.HydrantLayout;

using ThMEPWSS.HydrantLayout.Model;
using ThMEPWSS.HydrantLayout.Engine;
using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Data;

using ThMEPEngineCore.Model.Hvac;

namespace ThMEPWSS.HydrantLayout.Model
{
    //全局输入
    class RawData
    {
        //输入属性
        public List<ThIfcVirticalPipe> VerticalPipe;
        public List<ThHydrantModel> HydrantModel;
        public List<Entity> Room; //mpolygon //polyline
        public List<Entity> Wall; //mpolygon //polyline
        public List<Polyline> Column;
        public List<Polyline> Door;
        public List<Polyline> FireProof;

        //output
        public List<Polyline> Car;
        public List<Polyline> Well;

        //默认构造函数
        public RawData(ThHydrantLayoutDataQueryService dataQuery)
        {
            HydrantModel = dataQuery.HydrantModel;
            VerticalPipe = dataQuery.VerticalPipe;

            Wall = dataQuery.Wall;
            Room = dataQuery.Room;
            Column = dataQuery.Column;
            FireProof = dataQuery.FireProof;
            Door = dataQuery.Door;

            Car = dataQuery.Car;
            Well = dataQuery.Well;
        }
    }


    //第一次分类后的输出
    class ProcessedData
    {
        //空间索引
        public static ThCADCoreNTSSpatialIndex ForbiddenIndex;
        public static ThCADCoreNTSSpatialIndex LeanWallIndex;
        public static ThCADCoreNTSSpatialIndex ParkingIndex;
        //public static ThCADCoreNTSSpatialIndex LineWallIndex;
        public static ThCADCoreNTSSpatialIndex EntityAggregationIndex;
        //输出的立管
        public List<ThIfcVirticalPipe> VerticalPipeOut;
        //立柱起点
        public List<ThHydrantModel> FireHydrant;
        public List<ThHydrantModel> FireExtinguisher;
        //public List<Point3d> FireHydrant;
        //public List<Point3d> FireExtinguisher;

        public ProcessedData()
        {

        }
    }

    


    class Info
    {
        //模式
        public static int Type = 2;
        public static int Mode = 2;
        public static bool AllowDoorInPaking = true;
        public static bool ColumnCenterOK = true;

        //搜索范围
        public static double OriginRadius = 3000;
        public static double Radius = 3500;
        public static double SearchRadius = 2700;

        //距离权重
        public static double DistanceWeight = 0.5;

        //消火栓实体形状数据
        public static double VPSide = 150;

        //虚假数据
        public static double LongSide = 800;
        public static double ShortSide = 200;
        //public static double DoorLongSide = 1200;
        //public static double DoorShortSide = 800;
        //public static double DoorOffset = 200;
       
        //灭火器实体形状数据
        public static double ExDoorSide = 500;  
        
        //其他
        public static double ColumnAreaBound = 8000000;
        public static double PakingBuffer = 30;
        public static double CollisionTolerance = 0.25;

        
        //确定车位旁边的立柱的地理环境
        public static double RangeLength = 3000;
        public static double BigScore = 80;
        public static double SmallScore = 50;

    }

    class TMPDATA 
    {
        public static double TmpVPSideLength = 200;
        public static double FireHydrantLength = 800;
    }
}
