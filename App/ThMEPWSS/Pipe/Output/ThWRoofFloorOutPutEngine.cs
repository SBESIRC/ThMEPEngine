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
   public class ThWRoofFloorOutPutEngine
    {
        public static void LayoutRoofFloor(ThWCompositeFloorRecognitionEngine FloorEngines, ThWRoofDeviceParameters parameters2, ThWRoofParameters parameters1, AcadDatabase acadDatabase)
        {
            foreach (var composite in FloorEngines.RoofFloors)
            {
                var basecircle1 = composite.BaseCircles[0].Boundary.GetCenter();
                parameters1.baseCenter1.Add(basecircle1);
                parameters1.r_boundary = composite.RoofFloor.Boundary as Polyline;
                parameters1.gravityWaterBucket1 = GetGravityWaterBuckets(composite.GravityWaterBuckets);
                parameters1.sideWaterBucket1 = GetSideWaterBuckets(composite.SideEntryWaterBuckets);
                parameters1.roofRainPipe1 = GetroofRainPipe(composite.RoofRainPipes);
                parameters1.roofRoofRainPipes = parameters1.roofRainPipe1;
                parameters1.engine1.Run(parameters1.gravityWaterBucket1, parameters1.sideWaterBucket1, parameters1.roofRainPipe1, parameters1.r_boundary);
                parameters2.waterbuckets2 = parameters1.engine1.SideWaterBucketCenter;
                GetCreateLines(parameters1.engine1.GravityWaterBucketCenter, parameters1.engine1.GravityWaterBucketTag).ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetCreateLines(parameters1.engine1.SideWaterBucketCenter, parameters1.engine1.SideWaterBucketTag).ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetCreateLines1(parameters1.engine1.GravityWaterBucketCenter, parameters1.engine1.GravityWaterBucketTag).ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetCreateLines1(parameters1.engine1.SideWaterBucketCenter, parameters1.engine1.SideWaterBucketTag).ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetListText(parameters1.engine1.GravityWaterBucketCenter, parameters1.engine1.GravityWaterBucketTag, "DN100").ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetListText(parameters1.engine1.SideWaterBucketCenter, parameters1.engine1.SideWaterBucketTag, "DN75").ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetListText1(parameters1.engine1.GravityWaterBucketCenter, parameters1.engine1.GravityWaterBucketTag, "重力型雨水斗").ForEach(o => acadDatabase.ModelSpace.Add(o));
                GetListText1(parameters1.engine1.SideWaterBucketCenter, parameters1.engine1.SideWaterBucketTag, "重力型雨水斗").ForEach(o => acadDatabase.ModelSpace.Add(o));
                for (int i = 0; i < composite.RoofRainPipes.Count; i++)
                {
                    acadDatabase.ModelSpace.Add((CreateCircle(composite.RoofRainPipes[i].Outline.GetCenter()))); ;
                }
                for (int i = 0; i < parameters1.engine1.Center_point.Count; i++)
                {
                    acadDatabase.ModelSpace.Add(CreateCircle(parameters1.engine1.Center_point[i]));
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
