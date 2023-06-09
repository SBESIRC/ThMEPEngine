﻿using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Tools;
using ThMEPWSS.Pipe.Service;
using static ThMEPWSS.Command.ThPipeCreateCmd;

namespace ThMEPWSS.Pipe.Layout
{
    public class ThWRoofFloorOutPutEngine
    {
        public static void LayoutRoofFloor(ThWCompositeFloorRecognitionEngine FloorEngines, ThWRoofDeviceParameters parameters2, ThWRoofParameters parameters1, AcadDatabase acadDatabase,int scaleFactor,string W_RAIN_NOTE1)
        {
            foreach (var composite in FloorEngines.RoofFloors)
            {
                var basecircle1 = composite.BaseCircles[0].Boundary.GetCenter();
                parameters1.baseCenter1.Add(basecircle1);
                parameters1.r_boundary = composite.Space.Boundary as Polyline;
                parameters1.gravityWaterBucket1 = ThWPipeOutputFunction.GetGravityWaterBuckets(composite.GravityWaterBuckets);
                parameters1.sideWaterBucket1 = ThWPipeOutputFunction.GetSideWaterBuckets(composite.SideEntryWaterBuckets);
                parameters1.roofRainPipe1 = ThWPipeOutputFunction.GetroofRainPipe(composite.RoofRainPipes);
                parameters1.roofRoofRainPipes = parameters1.roofRainPipe1;
                parameters1.engine1.Run(parameters1.gravityWaterBucket1, parameters1.sideWaterBucket1, parameters1.roofRainPipe1, parameters1.r_boundary, scaleFactor);
                parameters2.waterbuckets2 = parameters1.engine1.SideWaterBucketCenter;
                ThWPipeOutputFunction.GetCreateLines(parameters1.engine1.GravityWaterBucketCenter, parameters1.engine1.GravityWaterBucketTag, W_RAIN_NOTE1).ForEach(o => parameters1.roofEntity.Add(o));
                ThWPipeOutputFunction.GetCreateLines(parameters1.engine1.SideWaterBucketCenter, parameters1.engine1.SideWaterBucketTag, W_RAIN_NOTE1).ForEach(o => parameters1.roofEntity.Add(o));
                ThWPipeOutputFunction.GetCreateLines1(parameters1.engine1.GravityWaterBucketCenter, parameters1.engine1.GravityWaterBucketTag, W_RAIN_NOTE1, ThTagParametersService.BucketStyle1, scaleFactor).ForEach(o => parameters1.roofEntity.Add(o));
                ThWPipeOutputFunction.GetCreateLines1(parameters1.engine1.SideWaterBucketCenter, parameters1.engine1.SideWaterBucketTag, W_RAIN_NOTE1, "侧入式雨水斗", scaleFactor).ForEach(o => parameters1.roofEntity.Add(o));
                ThWPipeOutputFunction.GetListText(parameters1.engine1.GravityWaterBucketCenter, parameters1.engine1.GravityWaterBucketTag, ThTagParametersService.GravityBuckettag1, scaleFactor, W_RAIN_NOTE1, acadDatabase.Database).ForEach(o => parameters1.roofEntity.Add(o));
                ThWPipeOutputFunction.GetListText(parameters1.engine1.SideWaterBucketCenter, parameters1.engine1.SideWaterBucketTag, ThTagParametersService.SideBuckettag, scaleFactor, W_RAIN_NOTE1, acadDatabase.Database).ForEach(o => parameters1.roofEntity.Add(o));
                ThWPipeOutputFunction.GetListText1(parameters1.engine1.GravityWaterBucketCenter, parameters1.engine1.GravityWaterBucketTag, ThTagParametersService.BucketStyle1, scaleFactor, W_RAIN_NOTE1, acadDatabase.Database).ForEach(o => parameters1.roofEntity.Add(o));
                ThWPipeOutputFunction.GetListText1(parameters1.engine1.SideWaterBucketCenter, parameters1.engine1.SideWaterBucketTag, "侧入式雨水斗", scaleFactor, W_RAIN_NOTE1, acadDatabase.Database).ForEach(o => parameters1.roofEntity.Add(o));
                for (int i = 0; i < composite.RoofRainPipes.Count; i++)
                {
                    parameters1.roofEntity.Add((ThWPipeOutputFunction.CreateCircle(composite.RoofRainPipes[i].Outline.GetCenter()))); ;
                }
                for (int i = 0; i < parameters1.engine1.Center_point.Count; i++)
                {
                    parameters1.roofEntity.Add(ThWPipeOutputFunction.CreateCircle(parameters1.engine1.Center_point[i]));
                }
            }
        }     
    }
}
