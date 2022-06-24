using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using Linq2Acad;
using ThMEPEngineCore;
using AcHelper;
using ThCADCore.NTS;
using ThParkingStall.Core.Tools;
using NetTopologySuite.Operation.Buffer;
using JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;
namespace ThMEPArchitecture.ParkingStallArrangement.PostProcess
{
    public static class ArrangementInfo
    {
        //获取区域内障碍物的外包框面积
        public static double GetObstacleArea(this SubArea subArea,double distance)
        {
            BufferParameters bufferParameters = new BufferParameters(8, EndCapStyle.Square, JoinStyle.Mitre, 5.0);
            var buffered = new MultiPolygon(subArea.Buildings.ToArray()).Buffer(distance, bufferParameters);
            var result = buffered.Union();
            if (result is Polygon poly) result = poly.RemoveHoles();
            else if (result is MultiPolygon mpoly)
            {
                result = new MultiPolygon(mpoly.Geometries.Cast<Polygon>().Select(p => p.RemoveHoles()).ToArray());
            }
            result = result.Buffer(-distance, bufferParameters);
            result = result.Intersection(subArea.Area);
#if DEBUG
            //foreach (var obj in result.ToDbObjects(true))
            //{
            //    if (obj is Entity ent)
            //    {
            //        ent.AddToCurrentSpace();
            //    }
            //}
#endif
            return result.Area;
        }
        //获取参数a（插入比）
        public static double GetAValue(this SubArea subArea, double distance = 3000)
        {
            var obstacleArea = subArea.GetObstacleArea(distance);
            return obstacleArea / subArea.Area.Area;
        }

        //public static double GetRValue(double a)
        //{
        //    var LisA = new List<double> { 0, 1.0/30.0, 1.0/15.0, 1.0/10.0, 1.0/6.0, 1.0/4.0, 1.0/3.0, 1.0/2.0 };
        //    var LisR = new List<double> { 25, 27, 28, 29, 31, 33, 36, 42 };
        //    double prop;
        //    for(int i = 0; i < LisA.Count-1; i++)
        //    {
        //        if(a >= LisA[i] && a < LisA[i + 1])
        //        {
        //            var lb = LisR[i];
        //            var ub = LisR[i + 1];
        //            prop = (a - LisA[i]) / (LisA[i + 1] - LisA[i]);
        //            var r = prop * (ub - lb) + lb;
        //            //Active.Editor.WriteMessage(r.ToString() + " \n");
        //            return r;
        //        }
        //    }
        //    var trans_a_start = Math.Atan(LisR.Last());
        //    var trans_a_end = Math.PI/2;
        //    prop = (a - LisA.Last()) / (1- LisA.Last());
        //    var trans_a = prop*(trans_a_end - trans_a_start) + trans_a_start;
        //    return Math.Tan(trans_a);

        //}
        public static DBText GetText( string strText, Point3d position,double height, string layer)
        {
            var dbText = new DBText();
            dbText.Layer = layer;
            dbText.TextString = strText;
            dbText.Position = position;
            dbText.Rotation = 0.0;
            dbText.Height = height;
            dbText.WidthFactor = 0.7;
            //dbText.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
            //dbText.HorizontalMode = TextHorizontalMode.TextCenter;
            //dbText.VerticalMode = TextVerticalMode.TextVerticalMid;
            //dbText.AlignmentPoint = position;
            return dbText;
        }
        public static DBText GetText(string strText, double x,double y, double height, string layer,int coloridx = 0)
        {
            var pt = new Point3d(x, y,0);
            var text = GetText(strText, pt, height, layer);
            text.ColorIndex = coloridx;
            return text;
        }
        public static void ShowText(this SubArea subArea, double distance = 3000,string layer = "AI-分区指标")
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(layer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 4);
            }

            var Center = new Point3d(subArea.Area.Centroid.X, subArea.Area.Centroid.Y,0) ;
            var heights = new List<double> { 2450,3000,1750,1750,1750};
            var positions = new List<Point3d>() ;
            double start = 5000 + Center.Y;
            foreach(var height in heights)
            {
                start -= (height + 1000);
                positions.Add(new Point3d(Center.X, start, 0)) ;
            }
            var mmtoM = 0.001 * 0.001;
            var lisText = new List<Entity>();

            var A = subArea.GetAValue(distance);
            var r = TableTools.GetRValue(A);
            var r_str = "参考指标： " + string.Format("{0:N1}", r);
            r_str += "m" + Convert.ToChar(0x00b2) + "/辆";
            var r_text = GetText(r_str, positions.First(), heights.First(), layer);
            r_text.ColorIndex = 0;
            lisText.Add(r_text);

            var R = subArea.Area.Area* mmtoM / subArea.Count;
            var R_str = "车均面积： " + string.Format("{0:N1}", R);
            R_str += "m" + Convert.ToChar(0x00b2) + "/辆";
            var R_text = GetText(R_str, positions[1], heights[1], layer);
            if (R < r) R_text.ColorIndex = 3;
            else R_text.ColorIndex = 1;
            lisText.Add(R_text);

            var g = subArea.Area.Area* mmtoM;
            var g_str = "区域面积： " + string.Format("{0:N1}", g);
            g_str += "m" + Convert.ToChar(0x00b2);
            var g_text = GetText(g_str, positions[2], heights[2], layer);
            g_text.ColorIndex = 0;
            lisText.Add(g_text);

            var n = subArea.Count;
            var n_str = "车位个数： " + n.ToString() + "个";
            var n_text = GetText(n_str, positions[3], heights[3], layer);
            n_text.ColorIndex = 0;
            lisText.Add(n_text);

            var A_str = "插入比： " + string.Format("{0:N2}", A);
            var A_text = GetText(A_str, positions[4], heights[4], layer);
            A_text.ColorIndex = 0;
            lisText.Add(A_text);
            lisText.ShowBlock(layer, layer, Center.X, Center.Y);
        }
    }
}
