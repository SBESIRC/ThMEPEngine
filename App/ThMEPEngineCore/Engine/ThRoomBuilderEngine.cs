using System;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThRoomBuilderEngine : IDisposable
    {
        public List<string> RoomBoundaryLayerFilter { get; set; }
        public List<string> RoomMarkLayerFilter { get; set; }
        public ThRoomBuilderEngine()
        {
            RoomBoundaryLayerFilter = new List<string>();
            RoomMarkLayerFilter = new List<string>();
        }
        public void Dispose()
        {            
        }

        public List<ThIfcRoom> BuildFromMS(Database db,Point3dCollection pts)
        {
            // Room 和 Mark 来源于本地
            var roomEngine = new ThWRoomRecognitionEngine()
            {
                LayerFilter = this.RoomBoundaryLayerFilter,
            };
            roomEngine.RecognizeMS(db, pts);
            var rooms = roomEngine.Elements.Cast<ThIfcRoom>().ToList();
            var markEngine = new ThRoomMarkRecognitionEngine()
            {
                LayerFilter = this.RoomMarkLayerFilter,
            };
            markEngine.RecognizeMS(db, pts);
            var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();
            Build(rooms, marks);
            return rooms;
        }

        public List<ThIfcRoom> BuildFromXRef(Database db, Point3dCollection pts)
        {
            // Room 和 Mark 来源于外参
            var roomEngine = new ThWRoomRecognitionEngine()
            {
                LayerFilter = this.RoomBoundaryLayerFilter,
            };
            roomEngine.Recognize(db, pts);
            var rooms = roomEngine.Elements.Cast<ThIfcRoom>().ToList();
            var markEngine = new ThRoomMarkRecognitionEngine()
            {
                LayerFilter = this.RoomMarkLayerFilter,
            };
            markEngine.Recognize(db, pts);
            var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();
            Build(rooms, marks);
            return rooms;
        }

        public void Build(List<ThIfcRoom> rooms, List<ThIfcTextNote> marks)
        {
            SpaceMatchText(BuildTextContainers(marks, rooms));
        }

        private Dictionary<ThIfcTextNote, List<ThIfcRoom>> BuildTextContainers(
            List<ThIfcTextNote> textNotes, List<ThIfcRoom> rooms)
        {
            var textContainer = new Dictionary<ThIfcTextNote, List<ThIfcRoom>>();
            textNotes.ForEach(m =>
            {
                Point3d textCenterPt = ThGeometryTool.GetMidPt(
                     m.Geometry.GetPoint3dAt(0),
                     m.Geometry.GetPoint3dAt(2));
                var containers = SelectTextIntersectPolygon(rooms.Select(o => o.Boundary).ToList(), m.Geometry);
                containers = containers.Where(n => n is Polyline polyline && polyline.Contains(textCenterPt)).ToList();
                if (containers.Count > 0)
                {
                    var containerRooms = rooms.Where(o => containers.Contains(o.Boundary)).ToList();
                    textContainer.Add(m, containerRooms);
                }
            });
            return textContainer;
        }

        private List<Curve> SelectTextIntersectPolygon(List<Curve> curves, Polyline textOBB)
        {
            return curves.Where(o =>
            {
                if (o is Polyline polyline)
                {
                    var relation = new ThCADCoreNTSRelate(polyline, textOBB);
                    return relation.IsOverlaps || relation.IsCovers || relation.IsContains;
                }
                return false;
            }).ToList();
        }

        private void SpaceMatchText(Dictionary<ThIfcTextNote, List<ThIfcRoom>> textContainer)
        {
            textContainer.ForEach(o =>
            {
                if (o.Value.Count > 0)
                {
                    var smallestAreaRoom = o.Value.OrderBy(v=>(v.Boundary as Polyline).Area).First();
                    if(smallestAreaRoom.Tags.IndexOf(o.Key.Text)<0)
                    {
                        smallestAreaRoom.Tags.Add(o.Key.Text);
                    }                    
                }
            });
        }
    }
}
