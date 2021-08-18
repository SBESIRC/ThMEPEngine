﻿using System;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;

namespace ThMEPEngineCore.Engine
{
    public class ThRoomBuilderEngine : IDisposable
    {
        public ThRoomBuilderEngine()
        {

        }
        public void Dispose()
        {            
        }

        public List<ThIfcRoom> BuildFromMS(Database db,Point3dCollection pts)
        {
            var roomEngine = new ThRoomOutlineRecognitionEngine();
            roomEngine.RecognizeMS(db, pts);
            var rooms = roomEngine.Elements.Cast<ThIfcRoom>().ToList();
            var markEngine = new ThRoomMarkRecognitionEngine();
            markEngine.RecognizeMS(db, pts);
            var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();
            Build(rooms, marks);            
            return rooms;
        }

        public virtual void Build(List<ThIfcRoom> rooms, List<ThIfcTextNote> marks)
        {
            BuildArea(rooms);
            SpaceMatchText(BuildTextContainers(marks, rooms));
        }

        protected List<ThIfcRoom> BuildArea(List<ThIfcRoom> rooms)
        {
            if(rooms.Count>0)
            {
                var objs = rooms.Select(o => o.Boundary).ToCollection();                
                objs = objs.BuildArea();
                objs = objs.FilterSmallArea(1.0);
                rooms.Clear();
                objs.Cast<Entity>().ForEach(o => rooms.Add(ThIfcRoom.Create(o)));
            }
            return rooms;
        }

        protected Dictionary<ThIfcTextNote, List<ThIfcRoom>> BuildTextContainers(
            List<ThIfcTextNote> textNotes, List<ThIfcRoom> rooms)
        {
            var textContainer = new Dictionary<ThIfcTextNote, List<ThIfcRoom>>();
            textNotes.ForEach(m =>
            {
                Point3d textCenterPt = ThGeometryTool.GetMidPt(
                     m.Geometry.GetPoint3dAt(0),
                     m.Geometry.GetPoint3dAt(2));
                var containers = SelectTextIntersectPolygon(rooms.Select(o => o.Boundary).ToList(), m.Geometry);
                var results = containers.Where(n => n is Polyline polyline && polyline.Contains(textCenterPt)).ToList();
                results.AddRange(containers.Where(n => n is MPolygon mPolygon && mPolygon.Contains(textCenterPt)));
                if (results.Count > 0)
                {
                    var containerRooms = rooms.Where(o => results.Contains(o.Boundary)).ToList();
                    textContainer.Add(m, containerRooms);
                }
            });
            return textContainer;
        }

        protected List<Entity> SelectTextIntersectPolygon(List<Entity> curves, Polyline textOBB)
        {
            return curves.Where(o =>
            {
                if (o is Polyline polyline)
                {
                    var relation = new ThCADCoreNTSRelate(polyline, textOBB);
                    return relation.IsOverlaps || relation.IsContains;
                }
                else if (o is MPolygon mPolygon)
                {
                    var relation = new ThCADCoreNTSRelate(mPolygon, textOBB);
                    return relation.IsOverlaps || relation.IsContains;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }).ToList();
        }

        protected void SpaceMatchText(Dictionary<ThIfcTextNote, List<ThIfcRoom>> textContainer)
        {
            textContainer.ForEach(o =>
            {
                if (o.Value.Count > 0)
                {
                    var smallestAreaRoom = o.Value.OrderBy(v=> RoomArea(v)).First();
                    if (smallestAreaRoom.Tags.IndexOf(o.Key.Text) < 0)
                    {
                        smallestAreaRoom.Tags.Add(o.Key.Text);
                    }
                }
            });
        }
        private double RoomArea(ThIfcRoom room)
        {
            if(room.Boundary is Polyline polyline)
            {
                return polyline.Area;
            }
            else if(room.Boundary is MPolygon mPolygon)
            {
                return mPolygon.Area;
            }    
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
