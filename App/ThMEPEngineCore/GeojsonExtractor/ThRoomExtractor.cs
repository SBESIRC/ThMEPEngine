using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThRoomExtractor : ThExtractorBase, IPrint
    {
        public List<ThIfcRoom> Rooms { get; protected set; }
        public IRoomPrivacy iRoomPrivacy { get; set; }
        public double TESSELLATE_ARC_LENGTH { get; set; }
        public ThRoomExtractor()
        {
            Rooms = new List<ThIfcRoom>();
            TESSELLATE_ARC_LENGTH = 50.0;
            Category = BuiltInCategory.Room.ToString();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Rooms.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, o.Name);
                var privacy = CheckPrivate(o);
                if (privacy != Privacy.Unknown)
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.PrivacyPropertyName, privacy.ToString());
                }
                geometry.Boundary = o.Boundary;
                geos.Add(geometry);
            });
            return geos;
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                var outlineEngine = new ThAIRoomOutlineExtractionEngine();
                outlineEngine.ExtractFromMS(database);

                var markEngine = new ThAIRoomMarkExtractionEngine();
                markEngine.ExtractFromMS(database);

                var transformer = new ThMEPOriginTransformer();
                if (pts.Count>0)
                {
                   var center = pts.Envelope().CenterPoint();
                    transformer = new ThMEPOriginTransformer(center);
                }
                else
                {
                    transformer = new ThMEPOriginTransformer(
                        outlineEngine.Results.Select(o=>o.Geometry).ToCollection());
                }

                outlineEngine.Results.ForEach(o => transformer.Transform(o.Geometry));
                markEngine.Results.ForEach(o => transformer.Transform(o.Geometry));
                var newPts = transformer.Transform(pts);

                var outlineRecogEngine = new ThAIRoomOutlineRecognitionEngine();
                outlineRecogEngine.Recognize(outlineEngine.Results, newPts);

                var markRecogEngine = new ThAIRoomMarkRecognitionEngine();
                markRecogEngine.Recognize(markEngine.Results, newPts);

                var rooms = outlineRecogEngine.Elements.Cast<ThIfcRoom>().ToList();
                var marks = markRecogEngine.Elements.Cast<ThIfcTextNote>().ToList();

                var roomEngine = new ThRoomBuilderEngine();
                roomEngine.Build(rooms, marks);

                Rooms = rooms;

                Clean();

                Rooms.ForEach(o =>
                {
                    if (string.IsNullOrEmpty(o.Name) && o.Tags.Count > 0)
                    {
                        o.Name = string.Join(";", o.Tags.ToArray());
                    }
                });

                Rooms.ForEach(r => transformer.Reset(r.Boundary)); //还原
            }
            else
            {
                //TODO
            }
            if (FilterMode == FilterMode.Window)
            {
                var rooms = FilterWindowPolygon(pts, Rooms.Select(o => o.Boundary).ToList());
                Rooms = Rooms.Where(o => rooms.Contains(o.Boundary)).ToList();
            }
        }
        protected void Clean()
        {
            using (var instance = new ThCADCoreNTSArcTessellationLength(TESSELLATE_ARC_LENGTH))
            {
                var simplifier = new ThPolygonalElementSimplifier()
                {
                    TESSELLATEARCLENGTH = TESSELLATE_ARC_LENGTH,
                };
                for (int i = 0; i < Rooms.Count; i++)
                {
                    if (Rooms[i].Boundary is Polyline polyline)
                    {
                        var objs = new DBObjectCollection();
                        objs.Add(polyline);
                        simplifier.Tessellate(objs);
                        objs = simplifier.MakeValid(objs);
                        objs = simplifier.Normalize(objs);
                        if (objs.Count > 0)
                        {
                            Rooms[i].Boundary = objs.Cast<Polyline>().OrderByDescending(o => o.Area).First();
                        }
                        else
                        {
                            //
                        }
                    }
                    else if (Rooms[i].Boundary is MPolygon mPolygon)
                    {
                        var polygon = mPolygon.ToNTSPolygon();
                        Rooms[i].Boundary = polygon.ToDbMPolygon();
                    }
                }
            }
        }

        public void Print(Database database)
        {
            Rooms.Select(o => o.Boundary).Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }
        private Privacy CheckPrivate(ThIfcRoom room)
        {
            if (iRoomPrivacy != null)
            {
                return iRoomPrivacy.Judge(room);
            }
            return Privacy.Unknown;
        }

        public override List<Entity> GetEntities()
        {
            return Rooms.Select(o=>o.Boundary).ToList();
        }
    }
}
