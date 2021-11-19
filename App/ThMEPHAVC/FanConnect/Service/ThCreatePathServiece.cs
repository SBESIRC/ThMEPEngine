using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPHVAC.FanConnect.Command;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThCreatePathServiece
    {
        public List<Line> TrunkLines { set; get; }//干路线
        public List<Line> BranchLines { set; get; }//支干路
        public List<Polyline> EquipmentObbs { set; get; }//可穿越区域，但是必须垂直连接且代价大(设备框)
        public List<Polyline> ObstacleRooms { set; get; }//可穿越区域，但是必须垂直穿越且代价大(房间框线)
        public List<Polyline> ObstacleHoles { set; get; }//不可穿越区域
        public void InitData()
        {

        }
        public Polyline CreatePath(ThFanCUModel model)
        {
            var retLine = new Polyline();
            //选择两条距离设备最近的线
            var nearLines = ThFanConnectUtils.GetNearbyLine(model.FanPoint, TrunkLines, 2);

            var pathList = new List<Polyline>();
            foreach (var l in nearLines)
            {
                var tmpPath = CreatePath(model, l);
                if (tmpPath != null)
                {
                    pathList.Add(tmpPath);
                }
            }

            //从pathList里面，挑选一条
            return retLine;
        }

        public Polyline CreatePath(ThFanCUModel model, Line line)
        {
            var retLine = new Polyline();
            //根据model的类型，先走一步
            var stepPt = TakeStep(model.FanObb, model.FanPoint,500);
            //根据model位置和line，构建一个框frame
            var frame = ThFanConnectUtils.CreateMapFrame(line, stepPt,10000);
            //提取frame里面的hole和room

            //使用A*算法，跑出路径
            return retLine;
        }

        /// <summary>
        /// 往前走一步
        /// </summary>
        /// <param name="pl"></param>
        /// <param name="pt"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public Point3d TakeStep(Polyline pl, Point3d pt, double step)
        {
            var retPt = new Point3d();
            var lines = (pl.Buffer(step)[0] as Polyline).ToLines();
            double minDist = double.MaxValue;
            foreach(var l in lines)
            {
                double tmpDist = ThFanConnectUtils.DistanceToPoint(l, pt);
                if(tmpDist < minDist)
                {
                    minDist = tmpDist;
                    retPt = l.GetClosestPointTo(pt, false);
                }
            }
            return retPt;
        }
    }
}
