using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThCreatePipeService
    {
        public double PipeWidth { set; get; }//水管宽度(2管制默认值为200，4管制默认值为400)
        public Point3d PipeStartPt { set; get; }//水管起始点
        public List<ThFanCUModel> EquipModel { set; get; }//风机设备
        public List<Line> TrunkLines { set; get; }//干路线
        public List<Line> BranchLines { get; }//支干路
        public List<Polyline> ObstacleRooms { get; }//可穿越区域，但是必须垂直穿越且代价大(房间框线)
        public List<Polyline> ObstacleHoles { get; }//不可穿越区域
        public ThCreatePipeService()
        {
            ObstacleRooms = new List<Polyline>();
            ObstacleHoles = new List<Polyline>();
        }
        public void AddObstacleRoom(Entity room)
        {
            if (room != null)
            {
                if (room is MPolygon)
                {
                    var pg = room as MPolygon;
                    var loops = pg.Loops();
                    ObstacleRooms.AddRange(loops);
                }
                else if (room is Polyline)
                {
                    ObstacleRooms.Add(room as Polyline);
                }
            }
        }
        public void AddObstacleHole(Entity hole)
        {
            if (hole != null)
            {
                if (hole is Polyline)
                {
//                    var tmpHole = (hole as Polyline).Buffer(PipeWidth)[0] as Polyline;
                    ObstacleHoles.Add(hole as Polyline);
                }
            }
        }
        public List<Polyline> CreatePipeLine(int type)
        {
            var pipePathServiece = new ThPipeExtractService
            {
                PipeStartPt = PipeStartPt,
                EquipModel = EquipModel,
                TrunkLines = TrunkLines,
                BranchLines = BranchLines,
                ObstacleRooms = ObstacleRooms,
                ObstacleHoles = ObstacleHoles
            };
            var retPLine = pipePathServiece.CreatePipePath(type);

            return retPLine;
        }
    }
}
