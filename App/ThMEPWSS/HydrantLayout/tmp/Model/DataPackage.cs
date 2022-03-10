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

using ThMEPWSS.HydrantLayout.tmp.Model;
using ThMEPWSS.HydrantLayout.tmp.Engine;
using ThMEPWSS.HydrantLayout.Data;
using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Model;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPWSS.HydrantLayout.tmp.Model
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
        public static ThCADCoreNTSSpatialIndex LineWallIndex;
        
        //立柱起点
        public List<Point3d> FireHydrant;
        public List<Point3d> FireExtinguisher;

        
        public ProcessedData()
        {

        }
    }


    //模型偏移量
    //class ModeOffset 
    //{
    //    public static List<Vector3d> FireCenter = new List<Vector3d>();
    //}



    //圆形环境
    class CircleEnv 
    {
    
    
    }



    public class Info
    {
        //模式
        public static int Mode = 2;

        //搜索范围
        public static double SearchRange = 3000;
        public static double Radius = 3500;

        //实体形状数据 
        public static double LongSide = 800;
        public static double ShortSide = 200;
        public static double DoorLongSide = 1200;
        public static double DoorShortSide = 800;
        public static double DoorOffset = 200;
        public static double VPSide = 200;
        
        //其他
        public static double ColumnAreaBound = 6000;
    }


}
