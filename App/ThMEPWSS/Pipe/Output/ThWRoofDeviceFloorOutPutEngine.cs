using Linq2Acad;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Engine;
using static ThMEPWSS.ThPipeCmds;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Layout
{
    public class ThWLayoutRoofDeviceFloorEngine
    {
        public static void LayoutRoofDeviceFloor(ThWCompositeFloorRecognitionEngine FloorEngines, ThWRoofDeviceParameters parameters2, AcadDatabase acadDatabase)
        {
            foreach (var composite in FloorEngines.RoofDeviceFloors)
            {
                var basecircle0 = composite.BaseCircles[0].Boundary.GetCenter();
                parameters2.baseCenter0.Add(basecircle0);
                parameters2.d_boundary = composite.RoofDeviceFloor.Boundary as Polyline;
                parameters2.gravityWaterBucket = GetGravityWaterBuckets(composite.GravityWaterBuckets);
                parameters2.sideWaterBucket = GetSideWaterBuckets(composite.SideEntryWaterBuckets);
                parameters2.roofRainPipe = GetroofRainPipe(composite.RoofRainPipes);
                parameters2.engine.Run(parameters2.gravityWaterBucket, parameters2.sideWaterBucket, parameters2.roofRainPipe, parameters2.d_boundary);
                parameters2.waterbuckets1 = parameters2.engine.SideWaterBucketCenter;
                GetListText(parameters2.engine.GravityWaterBucketCenter, parameters2.engine.GravityWaterBucketTag, "DN100").ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetListText(parameters2.engine.SideWaterBucketCenter, parameters2.engine.SideWaterBucketTag, "DN75").ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetListText1(parameters2.engine.GravityWaterBucketCenter, parameters2.engine.GravityWaterBucketTag, "重力型雨水斗").ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetListText1(parameters2.engine.SideWaterBucketCenter, parameters2.engine.SideWaterBucketTag, "侧入式雨水斗").ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetCreateLines(parameters2.engine.GravityWaterBucketCenter, parameters2.engine.GravityWaterBucketTag).ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetCreateLines1(parameters2.engine.GravityWaterBucketCenter, parameters2.engine.GravityWaterBucketTag).ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetCreateLines(parameters2.engine.SideWaterBucketCenter, parameters2.engine.SideWaterBucketTag).ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetCreateLines1(parameters2.engine.SideWaterBucketCenter, parameters2.engine.SideWaterBucketTag).ForEach(o => acadDatabase.ModelSpace.Add(o));
                for (int i = 0; i < parameters2.engine.Center_point.Count; i++)
                {
                    acadDatabase.ModelSpace.Add(CreateCircle(parameters2.engine.Center_point[i]));
                }
            }
        }
        private static List<BlockReference> GetGravityWaterBuckets(List<ThIfcGravityWaterBucket> GravityWaterBuckets)
        {
            var gravityWaterBucket = new List<BlockReference>();
            foreach (var gravity in GravityWaterBuckets)
            {
                BlockReference block = null;
                block = gravity.Outline as BlockReference;
                gravityWaterBucket.Add(block);
            }
            return gravityWaterBucket;
        }
        private static List<BlockReference> GetSideWaterBuckets(List<ThIfcSideEntryWaterBucket> GravityWaterBuckets)
        {
            var gravityWaterBucket = new List<BlockReference>();
            foreach (var gravity in GravityWaterBuckets)
            {
                BlockReference block = null;
                block = gravity.Outline as BlockReference;
                gravityWaterBucket.Add(block);
            }
            return gravityWaterBucket;
        }
        private static List<Polyline> GetroofRainPipe(List<ThIfcRoofRainPipe> RoofRainPipes)
        {
            var roofRainPipe = new List<Polyline>();
            foreach (var pipe in RoofRainPipes)
            {
                Polyline block = null;
                block = pipe.Outline as Polyline;
                roofRainPipe.Add(block);
            }
            return roofRainPipe;
        }
        private static List<DBText> GetListText(Point3dCollection points, Point3dCollection points1, string s)
        {
            var texts = new List<DBText>();
            for (int i = 0; i < points.Count; i++)
            {
                texts.Add(TaggingBuckettext(points1[4 * i + 2], s));
            }
            return texts;
        }
        private static List<DBText> GetListText1(Point3dCollection points, Point3dCollection points1, string s)
        {
            var texts = new List<DBText>();
            for (int i = 0; i < points.Count; i++)
            {
                texts.Add(TaggingBuckettext(points1[4 * i + 3], s));
            }
            return texts;
        }
        private static DBText TaggingBuckettext(Point3d tag, string s)
        {
            return new DBText()
            {
                Height = 200,
                Position = tag,
                TextString = s,
                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
            };
        }
    }
}
