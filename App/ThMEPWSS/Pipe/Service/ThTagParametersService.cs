using System;
using System.Collections.Generic;

namespace ThMEPWSS.Pipe.Service
{
  public class ThTagParametersService
    {
        public static string GravityBuckettag = "DN100";
        public static string SideBuckettag1 = "DN50";
        public static string SideBuckettag = "DN75";
        public static string BucketStyle = "重力型雨水斗";//"压力型雨水斗"
        public static double KaTFpipe = 100;
        public static double BalconyFpipe = 100;
        public static double ToiletWpipe = 100;
        public static double ToiletTpipe = 100;
        public static double Npipe = 100;
        public static double RoofRainpipe = 100;
        public static double Rainpipe = 100;
        public static string sourceFloor = "标准层35";
        public static List<Tuple<string, bool>> targetFloors =new List<Tuple<string, bool>>();
        public void Read()
        {
            targetFloors.Add(Tuple.Create("标准层1", true));
        }
    }
}
