using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Tools;
using ThMEPWSS.Pipe.Service;
using static ThMEPWSS.Command.ThPipeCreateCmd;

namespace ThMEPWSS.Pipe.Layout
{
    public class ThWLayoutRoofDeviceFloorEngine
    {
        public static void LayoutRoofDeviceFloor(ThWCompositeFloorRecognitionEngine FloorEngines, ThWRoofDeviceParameters parameters2, AcadDatabase acadDatabase,int scaleFactor,string W_RAIN_NOTE1)
        {
            foreach (var composite in FloorEngines.RoofTopFloors)
            {
                var basecircle0 = composite.BaseCircles[0].Boundary.GetCenter();
                parameters2.baseCenter0.Add(basecircle0);
                parameters2.d_boundary = composite.Space.Boundary as Polyline;
                parameters2.gravityWaterBucket = ThWPipeOutputFunction.GetGravityWaterBuckets(composite.GravityWaterBuckets);
                parameters2.sideWaterBucket = ThWPipeOutputFunction.GetSideWaterBuckets(composite.SideEntryWaterBuckets);
                parameters2.roofRainPipe = ThWPipeOutputFunction.GetroofRainPipe(composite.RoofRainPipes);
                parameters2.engine.Run(parameters2.gravityWaterBucket, parameters2.sideWaterBucket, parameters2.roofRainPipe, parameters2.d_boundary, scaleFactor);
                parameters2.waterbuckets1 = parameters2.engine.SideWaterBucketCenter;         
                ThWPipeOutputFunction.GetListText(parameters2.engine.GravityWaterBucketCenter, parameters2.engine.GravityWaterBucketTag, ThTagParametersService.GravityBuckettag, scaleFactor, W_RAIN_NOTE1, acadDatabase.Database).ForEach(o => parameters2.roofDeviceEntity.Add(o));
                ThWPipeOutputFunction.GetListText(parameters2.engine.SideWaterBucketCenter, parameters2.engine.SideWaterBucketTag, ThTagParametersService.SideBuckettag, scaleFactor, W_RAIN_NOTE1, acadDatabase.Database).ForEach(o => parameters2.roofDeviceEntity.Add(o));
                ThWPipeOutputFunction.GetListText1(parameters2.engine.GravityWaterBucketCenter, parameters2.engine.GravityWaterBucketTag, ThTagParametersService.BucketStyle, scaleFactor, W_RAIN_NOTE1, acadDatabase.Database).ForEach(o => parameters2.roofDeviceEntity.Add(o));
                ThWPipeOutputFunction.GetListText1(parameters2.engine.SideWaterBucketCenter, parameters2.engine.SideWaterBucketTag, "侧入式雨水斗", scaleFactor, W_RAIN_NOTE1, acadDatabase.Database).ForEach(o => parameters2.roofDeviceEntity.Add(o));
                ThWPipeOutputFunction.GetCreateLines(parameters2.engine.GravityWaterBucketCenter, parameters2.engine.GravityWaterBucketTag, W_RAIN_NOTE1).ForEach(o => parameters2.roofDeviceEntity.Add(o));
                ThWPipeOutputFunction.GetCreateLines1(parameters2.engine.GravityWaterBucketCenter, parameters2.engine.GravityWaterBucketTag, W_RAIN_NOTE1, ThTagParametersService.BucketStyle, scaleFactor).ForEach(o => parameters2.roofDeviceEntity.Add(o));
                ThWPipeOutputFunction.GetCreateLines(parameters2.engine.SideWaterBucketCenter, parameters2.engine.SideWaterBucketTag, W_RAIN_NOTE1).ForEach(o => parameters2.roofDeviceEntity.Add(o));
                ThWPipeOutputFunction.GetCreateLines1(parameters2.engine.SideWaterBucketCenter, parameters2.engine.SideWaterBucketTag, W_RAIN_NOTE1, "侧入式雨水斗", scaleFactor).ForEach(o => parameters2.roofDeviceEntity.Add(o));
                for (int i = 0; i < parameters2.engine.Center_point.Count; i++)
                {
                    parameters2.roofDeviceEntity.Add(ThWPipeOutputFunction.CreateCircle(parameters2.engine.Center_point[i]));
                }
            }
        }
       
    }
}
