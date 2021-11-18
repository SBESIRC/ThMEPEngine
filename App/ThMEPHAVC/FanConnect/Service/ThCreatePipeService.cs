using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThCreatePipeService
    {
        public Point3d PipeStartPt { set; get; }//水管起始点
        public List<Point3d> EquipPoint { get; }//设备连接点
        public List<Line> TrunkLines { get; }//干路线
        public List<Line> BranchLines { get; }//支干路
        public List<Polyline> EquipmentObbs { get; }//可穿越区域，但是必须垂直连接且代价大(设备框)
        public List<Polyline> ObstacleRooms { get; }//可穿越区域，但是必须垂直穿越且代价大(房间框线)
        public List<Polyline> ObstacleHoles { get; }//不可穿越区域
        public ThCreatePipeService()
        {
            EquipPoint = new List<Point3d>();
            TrunkLines = new List<Line>();
            BranchLines = new List<Line>();
            EquipmentObbs = new List<Polyline>();
            ObstacleRooms = new List<Polyline>();
            ObstacleHoles = new List<Polyline>();
        }
        public void AddEquipPoint(Point3d pt)
        {
            EquipPoint.Add(pt);
        }
        public void AddEquipmentObbs(Entity room)
        {
            if (room != null)
            {
                if (room is Polyline)
                {
                    ObstacleRooms.Add(room as Polyline);
                }
            }
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
                    ObstacleHoles.Add(hole as Polyline);
                }
            }
        }
        public ThFanTreeModel<ThFanPipeModel> CreatePipeLine(int type)
        {
            var pipePathServiece = new ThPipeExtractServiece();
            pipePathServiece.PipeStartPt = PipeStartPt;

            pipePathServiece.EquipPoint = EquipPoint;
            pipePathServiece.TrunkLines = TrunkLines;
            pipePathServiece.BranchLines = BranchLines;
            pipePathServiece.EquipmentObbs = EquipmentObbs;
            pipePathServiece.ObstacleRooms = ObstacleRooms;
            pipePathServiece.ObstacleHoles = ObstacleHoles;
            var treeModel = pipePathServiece.CreatePipePath(type);

            return treeModel;
        }
    }
}
