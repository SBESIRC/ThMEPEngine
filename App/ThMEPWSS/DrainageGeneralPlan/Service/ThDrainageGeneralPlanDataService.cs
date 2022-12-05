using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.DrainageGeneralPlan.Utils;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThCADCore.NTS;
using NFox.Cad;
using ThMEPEngineCore.CAD;
using ThMEPWSS.SprinklerConnect.Service;

namespace ThMEPWSS.DrainageGeneralPlan.Service
{
    public class ThDrainageGeneralPlanDataService
    {
        /// <summary>
        /// 对主干线进行分组,对main做index
        /// </summary>
        /// <param name="mainP"></param>
        /// <param name="outP"></param>
        public static void Group(List<Line> mainP,List<Polyline> outP, Dictionary<Line, List<Polyline>> mainToOut, Dictionary<Polyline, List<Line>> outToMain)
        {
            //包含重复
            for(int i=0; i < mainP.Count; i++)
            {
                var line = mainP[i].ToPolyline();//转成pl
                var mainIndex = new ThCADCoreNTSSpatialIndex(outP.ToCollection());//main索引
                var mBuffer = line.BufferPL(ThDrainageGeneralPlanCommon.OutMainRange)[0] as Polyline;
                var nearLine = mainIndex.SelectCrossingPolygon(mBuffer);
                //mainToOut[]
                for(int j=0; j < nearLine.Count; j++)
                {
                    var pl = nearLine[j] as Polyline;
                    if (!outToMain.ContainsKey(pl))
                    {
                        var temp = new List<Line>();
                        temp.Add(mainP[i]);
                        outToMain[pl] = temp;
                    }
                    else
                        outToMain[pl].Add(mainP[i]);
                }
            }

            //挑选
            foreach(var i in outToMain)
            {
                if (i.Value.Count > 1)//匹配到多个main
                {
                    //double angleO = unifyAngle(i.Key.Angle);
                    double dis = 100;
                    int index = 0;
                    for(int j = 0; j < i.Value.Count; j++)
                    {
                        var p=i.Value[j];
                        double angleM= unifyAngle(p.Angle);

                    }
                }
            }
        }
    
        private double getDis(Polyline o,Line m)
        {
            double dis=100;
            double angleM = unifyAngle(m.Angle);
            if (o.NumberOfVertices == 2)//是直线
            {
                var line = o.ToLines()[0];
                double angleO=unifyAngle(line.Angle);
                if (Math.Abs(angleM - angleO)<ThDrainageGeneralPlanCommon.AngleRange)
                {
                    dis = -1;
                }
                else
                {
                    var pt1 = m.GetClosestPointTo(line.StartPoint, true);
                    var dis1 = line.StartPoint.DistanceTo(pt1);

                    var pt2 = m.GetClosestPointTo(line.EndPoint, true);
                    var dis2= line.EndPoint.DistanceTo(pt2);

                    //var qq= m.GetDistAtPoint(pt1);
                    dis = dis1 < dis2 ? dis1 : dis2;
                }
            }
            else//是多段线
            {

            }
                return dis;

        }
        /// <summary>
        /// 统一角度
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
    
        private static double unifyAngle(double radians)
        {
            double d= radians * 180 / Math.PI;
            d= Math.Round(d, 0);
            return d>180?d-180:d;
        }
    }
}
