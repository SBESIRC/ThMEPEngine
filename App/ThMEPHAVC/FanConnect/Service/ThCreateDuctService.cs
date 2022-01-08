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
    class ThCreateDuctService
    {
        public double DuctWidth { set; get; }//风管宽度
        public Point3d StartPt { set; get; }//风管起始点
        public List<ThFanCUModel> EquipModel { set; get; }//风机设备
        public List<Line> TrunkLines { set; get; }//干路线
        public List<Line> BranchLines { get; }//支干路
        public List<Polyline> ObstacleRooms { get; }//可穿越区域，但是必须垂直穿越且代价大(房间框线)
        public List<Polyline> ObstacleHoles { get; }//不可穿越区域
        public ThCreateDuctService()
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
                    var tmpHole = (hole as Polyline).Buffer(DuctWidth)[0] as Polyline;
                    ObstacleHoles.Add(tmpHole);
                }
            }
        }
        public List<Polyline> CreatePipeLine(int type)
        {
            var pipePathServiece = new ThPipeExtractService
            {
                PipeStartPt = StartPt,
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
