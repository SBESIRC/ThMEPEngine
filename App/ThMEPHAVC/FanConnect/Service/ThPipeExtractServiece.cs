using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Service
{
    /// <summary>
    /// 管路路由生成器
    /// </summary>
    public class ThPipeExtractServiece
    {
        public Point3d PipeStartPt { set; get; }//水管起始点
        public List<ThFanCUModel> EquipModel { set; get; }//风机设备
        public List<Line> TrunkLines { set; get; }//干路线
        public List<Line> BranchLines { set; get; }//支干路
        public List<Polyline> ObstacleRooms { set; get; }//可穿越区域，但是必须垂直穿越且代价大(房间框线)
        public List<Polyline> ObstacleHoles { set; get; }//不可穿越区域
        /// <summary>
        /// 生成管路路由
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<Polyline> CreatePipePath(int type)
        {
            List<Polyline> retPline = null;
            switch (type)
            {
                case 0:
                    retPline = GetPipeTreeModel0();
                    break;
                case 1:
                    retPline = GetPipeTreeModel1();
                    break;
                default:
                    break;
            }

            return retPline;
        }
        private List<Polyline> GetPipePath0()
        {
            var retList = new List<Polyline>();
            var pathServiece = new ThCreatePathServiece();
            pathServiece.TrunkLines = TrunkLines;
            pathServiece.ObstacleRooms = ObstacleRooms;
            pathServiece.ObstacleHoles = ObstacleHoles;
            pathServiece.InitData();
            foreach (var equip in EquipModel)
            {
                var tmpPath = pathServiece.CreatePath(equip);
                if(tmpPath != null)
                {
                    retList.Add(tmpPath);
                }
            }
            return retList;
        }
        private List<Line> GetPipePath1()
        {
            var retList = new List<Line>();

            return retList;
        }
        private List<Polyline> GetPipeTreeModel0()
        {
            //将设备进行连接，得到管路路由
            var termLine = GetPipePath0();//支路
            return termLine;
        }
        private List<Polyline> GetPipeTreeModel1()
        {
            List<Polyline> retPLine = new List<Polyline>();
            //可以引用自己写类和方法
            return retPLine;
        }
    }
}
