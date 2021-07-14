using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Uitl;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class PipeLineList
    {
        public static List<Line> GetPipeLineList(Point3dCollection selectArea)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var fireHydrantSysIn = new FireHydrantSystemIn();//输入参数
                var lineList = new List<Line>();//管段列表
                var pointList = new List<Point3dEx>();//点集

                var ptVisit = new Dictionary<Point3dEx, bool>();//访问标志

                var pipeEngine = new ThExtractHYDTPipeService();//提取供水管
                var dbObjs = pipeEngine.Extract(acadDatabase.Database, selectArea);
                PipeLine.AddPipeLine(dbObjs, ref fireHydrantSysIn, ref pointList, ref lineList);

                var valveEngine = new ThExtractValveService();//提取蝶阀
                var valveDB = valveEngine.Extract(acadDatabase.Database, selectArea);
                fireHydrantSysIn.ValveIsBkReference = valveEngine.IsBkReference;
                var valveList = new List<Line>();
                PipeLine.AddValveLine(valveDB, ref fireHydrantSysIn, ref pointList, ref lineList, ref valveList);

                PipeLine.PipeLineSplit(ref lineList, pointList);//管线打断

                return lineList;
            }
        }
        
        public static void PipeLineAutoConnect(ref List<Line> lineList)
        {
            var GLineSegList = new List<GLineSegment>();//line 转 GLineSegment
            foreach (var l in lineList)
            {
                var GLineSeg = new GLineSegment(l.StartPoint.X, l.StartPoint.Y, l.EndPoint.X, l.EndPoint.Y);
                GLineSegList.Add(GLineSeg);
            }

            var GLineConnectList = GeoFac.AutoConn(GLineSegList, null, 1000, 1);//打断部分 自动连接
            foreach (var l in GLineConnectList)
            {
                GLineSegList.Add(l);
            }

            lineList = new List<Line>();//GLineSegment 转 line
            foreach (var gl in GLineSegList)
            {
                var pt1 = new Point3d(gl.StartPoint.X, gl.StartPoint.Y, 0);
                var pt2 = new Point3d(gl.EndPoint.X, gl.EndPoint.Y, 0);
                var line = new Line(pt1, pt2);
                lineList.Add(line);
            }
        }

        public static void RemoveFalsePipe(ref List<Line> lineList, List<Point3dEx> hydrantPosition)
        {
            foreach (var line in lineList.ToArray())//删除两个点都是端点的线段
            {
                if (PtInPtList.PtIsTermLine(line, hydrantPosition))
                {
                    lineList.Remove(line);
                }
            }
        }
    }
}
