using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public Polyline CreatePath(Point3d pt)
        {
            var retLine = new Polyline();
            return retLine;
        }
    }
}
