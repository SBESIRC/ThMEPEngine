using System;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public class ThRoomBuilderEngine : IDisposable
    {
        public void Dispose()
        {            
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
