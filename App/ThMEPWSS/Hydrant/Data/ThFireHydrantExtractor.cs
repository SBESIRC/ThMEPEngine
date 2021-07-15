using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.Engine;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.Hydrant.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.Hydrant.Data
{
    public class ThFireHydrantExtractor : ThExtractorBase, IPrint
    {
        /// <summary>
        /// 点距离房间边线的最远距离
        /// </summary>
        private double MaxDistanceToRoom = 200.0;
        public List<DBPoint> FireHydrants { get; set; }
        private List<ThIfcRoom> Rooms { get; set; }  
        private Dictionary<DBPoint, Polyline> HydrantOutline { get; set; }
        public ThFireHydrantExtractor()
        {
            FireHydrants = new List<DBPoint>();
            Rooms = new List<ThIfcRoom>();
            Category = BuiltInCategory.Equipment.ToString();
            HydrantOutline = new Dictionary<DBPoint, Polyline>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            var vistor = new ThFireHydrantExtractionVisitor()
            {
                BlkNames = new List<string>() { "室内消火栓平面" },
            };
            //后期再做远距离移动
            var hydrantExtractor = new ThFireHydrantRecognitionEngine(vistor);
            hydrantExtractor.Recognize(database, pts);
            hydrantExtractor.RecognizeMS(database, pts);
            hydrantExtractor.Elements.ForEach(o =>
            {
                var obb = o.Outline as Polyline;
                var center = GetCenter(obb);
                HydrantOutline.Add(new DBPoint(center), obb);
            });      
            FireHydrants = HydrantOutline.Select(o => o.Key).ToList();
            if(FilterMode == FilterMode.Window)
            {
                FireHydrants = FilterWindowPolygon(pts, FireHydrants.Cast<Entity>().ToList()).Cast<DBPoint>().ToList();
            }
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            FireHydrants.ForEach(e =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "FireHydrant");
                geometry.Boundary = e;
                geos.Add(geometry);
            });
            return geos;
        }

        private Point3d GetCenter(Polyline rec)
        {
            return rec.GetPoint3dAt(0).GetMidPt(rec.GetPoint3dAt(2));
        }

        public void Print(Database database)
        {
            FireHydrants
                .Select(o=>new Circle(o.Position,Vector3d.ZAxis,5.0))
                .Cast<Entity>().ToList()
                .CreateGroup(database, ColorIndex);
        }
        public void AdjustFireHydrantPosition(List<ThIfcRoom> rooms)
        {
            var roomInnerPts = PointInRoom(rooms); //获取在房间内的点           
            var isoldatedHydrants = FireHydrants.Where(o => !roomInnerPts.Contains(o)).ToList(); // 获取不在房间内的点
            isoldatedHydrants.ForEach(o =>
            {
                var obb = HydrantOutline[o];
                var width = GetRectangleWidth(obb);
                var disDic = DistancToRoom(o.Position, rooms, width/2.0);
                if(disDic.Count>0)
                {
                    var closestRoom = disDic.OrderBy(m => m.Value).First().Key;
                    var newPt = MovePtToRoom(o.Position, closestRoom);
                    roomInnerPts.Add(new DBPoint(newPt));
                }
            });
            FireHydrants = roomInnerPts; //更新消火栓点位
        }
        private List<DBPoint> PointInRoom(List<ThIfcRoom> rooms)
        {
            var results = new List<DBPoint>();
            //获取在房间内的点
            FireHydrants.ForEach(o =>
            {
                foreach (var room in rooms)
                {
                    if (room.Boundary.IsContains(o.Position))
                    {
                        results.Add(o);
                        break;
                    }
                }
            });
            return results;
        }
        private Dictionary<Entity,double> DistancToRoom(Point3d pt, List<ThIfcRoom> rooms,double width)
        {
            var result = new Dictionary<Entity, double>();
            rooms.ForEach(r =>
            {
                var dis = r.Boundary.ToNTSPolygon().Dictance(pt);
                if(dis<= width+MaxDistanceToRoom)
                {
                    result.Add(r.Boundary, dis);
                }
            });
            return result;
        }
        private Point3d MovePtToRoom(Point3d pt, Entity room)
        {
            var closePt = ThCADCoreNTSDistance.GetClosePoint(room.ToNTSPolygon(), pt);
            var vec = pt.GetVectorTo(closePt);
            int increAng = 5; 
            int count = 360 / increAng;
            for (int i = 5; i <= 10; i++)
            {
                var radius = vec.Length + i;
                for (int j = 0; j < count; j++)
                {
                    var rotateVec = vec.RotateBy(ThAuxiliaryUtils.AngToRad(j * increAng), Vector3d.ZAxis).GetNormal();
                    var extendPt = pt + rotateVec.MultiplyBy(radius);
                    if (room.IsContains(extendPt))
                    {
                        return extendPt;
                    }
                }
            }
            return pt;
        }
        private double GetRectangleWidth(Polyline rectangle)
        {
            var lines = rectangle.ToLines();
            lines = lines.Where(o => o.Length > 1.0).OrderBy(o => o.Length).ToList();
            return lines.Count > 0 ? lines[0].Length : 0.0;
        }
    }
}
