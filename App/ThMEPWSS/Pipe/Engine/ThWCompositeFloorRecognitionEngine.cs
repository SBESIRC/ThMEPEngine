﻿using Linq2Acad;
using ThMEPWSS.Pipe.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Plumbing;
using ThMEPEngineCore.Engine;
using System.Linq;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWCompositeFloorRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWRoofDeviceFloorRoom> RoofDeviceFloors { get; set; }
        public List<ThWRoofFloorRoom> RoofFloors { get; set; }
        public List<ThWTopFloorRoom> TopFloors { get; set; }
        public List<ThWTopFloorRoom> NormalFloors { get; set; }
        public List<Curve> TagNameFrames { get; set; }
        public List<Curve> StairFrames { get; set; }
        public List<Curve> Columns { get; set; }
        public List<Curve> ShearWalls { get; set; }
        public List<Curve> InnerDoors { get; set; }
        public List<Curve> Devices { get; set; }
        public List<Curve> ArchitectureWalls { get; set; }
        public List<Curve> Windows { get; set; }
        public List<Curve> ElevationFrames { get; set; }
        public List<Curve> AxialCircleTags { get; set; }
        public List<Curve> AxialAxisTags { get; set; }
        public List<Curve> ExternalTags { get; set; }
        public List<Curve> Wells { get; set; }
        public List<Curve> DimensionTags { get; set; }
        public List<Curve> RainPipes { get; set; }
        public List<Curve> PositionTags { get; set; }
        public List<Curve> AllObstacles { get; set; }
        public List<string> Layers { get; set; }    
        public ThWCompositeFloorRecognitionEngine()
        {
            TagNameFrames = new List<Curve>();
            StairFrames = new List<Curve>();
            Columns = new List<Curve>();
            ShearWalls = new List<Curve>();
            InnerDoors = new List<Curve>();
            Devices = new List<Curve>();
            ArchitectureWalls = new List<Curve>();
            Windows = new List<Curve>();
            ElevationFrames = new List<Curve>();
            AxialCircleTags = new List<Curve>();
            AxialAxisTags = new List<Curve>();
            ExternalTags = new List<Curve>();
            Wells = new List<Curve>();
            DimensionTags = new List<Curve>();
            RainPipes = new List<Curve>();
            PositionTags = new List<Curve>();
            AllObstacles= new List<Curve>();
            Layers = new List<string>();
            RoofDeviceFloors = new List<ThWRoofDeviceFloorRoom>();
            RoofFloors = new List<ThWRoofFloorRoom>();
            TopFloors = new List<ThWTopFloorRoom>();
            NormalFloors= new List<ThWTopFloorRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var GravityWaterBuckets = GetgravityWaterBuckets(database, pts);
                var SideEntryWaterBuckets = GetsideEntryWaterBuckets(database, pts);
                var RoofRainPipes = GetroofRainPipes(database, pts);
                var RoofDeviceEngine = new ThWRoofDeviceFloorRecognitionEngine()
                {   
                GravityWaterBuckets = GravityWaterBuckets,
                SideEntryWaterBuckets = SideEntryWaterBuckets,
                RoofRainPipes = RoofRainPipes
                };               
                RoofDeviceEngine.Recognize(database, pts);             
                RoofDeviceFloors = RoofDeviceEngine.Rooms;
                RoofDeviceEngine.TagNameFrames.ForEach(o => TagNameFrames.Add(o));
                RoofDeviceEngine.StairFrames.ForEach(o => StairFrames.Add(o));
                RoofDeviceEngine.Columns.ForEach(o => Columns.Add(o));
                RoofDeviceEngine.ShearWalls.ForEach(o => ShearWalls.Add(o));
                RoofDeviceEngine.InnerDoors.ForEach(o => InnerDoors.Add(o));
                RoofDeviceEngine.Devices.ForEach(o => Devices.Add(o));
                RoofDeviceEngine.ArchitectureWalls.ForEach(o => ArchitectureWalls.Add(o));
                RoofDeviceEngine.Windows.ForEach(o => Windows.Add(o));
                RoofDeviceEngine.ElevationFrames.ForEach(o => ElevationFrames.Add(o));
                RoofDeviceEngine.AxialCircleTags.ForEach(o => AxialCircleTags.Add(o));
                RoofDeviceEngine.AxialAxisTags.ForEach(o => AxialAxisTags.Add(o));
                RoofDeviceEngine.ExternalTags.ForEach(o => ExternalTags.Add(o));
                RoofDeviceEngine.Wells.ForEach(o => Wells.Add(o));
                RoofDeviceEngine.DimensionTags.ForEach(o => DimensionTags.Add(o));
                RoofDeviceEngine.RainPipes.ForEach(o => RainPipes.Add(o));
                RoofDeviceEngine.PositionTags.ForEach(o => PositionTags.Add(o));
                RoofDeviceEngine.AllObstacles.ForEach(o => AllObstacles.Add(o));
                RoofDeviceEngine.Layers.ForEach(o => Layers.Add(o));
                var RoofEngine = new ThWRoofFloorRecognitionEngine()
                {
                   gravityWaterBuckets= GravityWaterBuckets,
                   sideEntryWaterBuckets= SideEntryWaterBuckets,
                   roofRainPipes= RoofRainPipes
                };            
                RoofEngine.Recognize(database, pts);
                RoofFloors = RoofEngine.Rooms;
                var FirstEngine = new ThWTopFloorRecognitionEngine()
                {
                    Spaces= RoofDeviceEngine.Spaces
                };      
                FirstEngine.Recognize(database, pts);
                TopFloors = FirstEngine.Rooms;
                if (TopFloors.Count > 0)
                {
                    for (int i = 1;i< TopFloors.Count; i++)
                    {
                        NormalFloors.Add(TopFloors[i]);
                    }
                }
            }
        }
        private List<ThIfcGravityWaterBucket> GetgravityWaterBuckets(Database database, Point3dCollection pts)
        {
            using (ThGravityWaterBucketRecognitionEngine gravityWaterBucket = new ThGravityWaterBucketRecognitionEngine())
            {
                gravityWaterBucket.Recognize(database, pts);
                return gravityWaterBucket.Elements.Cast<ThIfcGravityWaterBucket>().ToList();
            }
        }
        private List<ThIfcSideEntryWaterBucket> GetsideEntryWaterBuckets(Database database, Point3dCollection pts)
        {
            using (ThSideEntryWaterBucketRecognitionEngine sideEntryWaterBucketEngine = new ThSideEntryWaterBucketRecognitionEngine())
            {
                sideEntryWaterBucketEngine.Recognize(database, pts);
                return sideEntryWaterBucketEngine.Elements.Cast<ThIfcSideEntryWaterBucket>().ToList();
            }
        }
        private List<ThWRoofRainPipe> GetroofRainPipes(Database database, Point3dCollection pts)
        {
            using (ThWRoofRainPipeRecognitionEngine roofRainPipesEngine = new ThWRoofRainPipeRecognitionEngine())
            {
                roofRainPipesEngine.Recognize(database, pts);
                return roofRainPipesEngine.Elements.Cast<ThWRoofRainPipe>().ToList();
            }
        }
    }
}
