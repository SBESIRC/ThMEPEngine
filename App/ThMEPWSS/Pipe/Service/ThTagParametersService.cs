using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.Pipe.Service
{
  public class ThTagParametersService
    {
        public static string GravityBuckettag = "DN100";
        public static string GravityBuckettag1 = "DN100";
        public static string SideBuckettag1 = "DN50";
        public static string SideBuckettag = "DN75";
        public static string BucketStyle = "重力型雨水斗";//"压力型雨水斗"
        public static string BucketStyle1 = "重力型雨水斗";//"压力型雨水斗"
        public static double KaTFpipe = 100;
        public static double BalconyFpipe = 100;
        public static double ToiletWpipe = 100;
        public static double ToiletTpipe = 100;
        public static double Npipe = 100;
        public static double RoofRainpipe = 100;
        public static double Rainpipe = 100;
        public static string sourceFloor = "标准层35";
        public static int ScaleFactor = 1;
        public static bool IsCaisson = false;
        public static bool IsSeparation = false;
        public static string PipeLayer = "";
        public static int FloorValue = 100;
        public static List<Tuple<string, bool>> targetFloors =new List<Tuple<string, bool>>();
        public static List<BlockReference> blockCollection = new List<BlockReference>();
        public static Point3dCollection framePoints = new Point3dCollection();
        public static Point3dCollection ToiletWells=new Point3dCollection();
       public void Read()
        {
            //targetFloors.Add(Tuple.Create("标准层1", true));
        }
    }
}
