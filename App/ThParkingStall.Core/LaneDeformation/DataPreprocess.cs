using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.LaneDeformation;

using NetTopologySuite.Operation.OverlayNG;



namespace ThParkingStall.Core.LaneDeformation
{
    public class DataPreprocess
    {

        public VehicleLaneData rawData;

        public List<Polygon> originalFreeAreaList = new List<Polygon>();

        public DataPreprocess(VehicleLaneData data) 
        {
            RawData.rawData = data;
            rawData = data;

        }
        public void Pipeline()
        {
            GetOriginalFreeAreaTest();
            //GetOriginalFreeArea();

            BuildFreeArea();
        }

        public void GetOriginalFreeArea()
        {
            List<Geometry> geometries = new List<Geometry>();
            for (int i = 0; i < rawData.VehicleLanes.Count; i++)
            {
                geometries.Add(rawData.VehicleLanes[i].LaneObb);

                if (i == 70|| i==89) continue;
                for (int j = 0; j < rawData.VehicleLanes[i].ParkingPlaceBlockList.Count; j++)
                {
                    geometries.Add(rawData.VehicleLanes[i].ParkingPlaceBlockList[j].ParkingPlaceBlockObb);
                }
            }

            geometries.AddRange(LaneDeformationParas.Blocks);
            GeometryCollection differenceObjs = new GeometryCollection(geometries.ToArray());
            var freeAreeResult = OverlayNGRobust.Overlay(LaneDeformationParas.Boundary, differenceObjs, NetTopologySuite.Operation.Overlay.SpatialFunction.Difference);


            
            if (freeAreeResult is Polygon a)
            {
                originalFreeAreaList.Add(a);
            }
            else if (freeAreeResult is GeometryCollection collection)
            {
                foreach (var geo in collection.Geometries)
                {
                    if (geo is Polygon pl)
                    {
                        originalFreeAreaList.Add(pl);
                    }
                }
            }
        }


        public void GetOriginalFreeAreaTest()
        {

            Geometry tmpG = LaneDeformationParas.Boundary;

            List<Geometry> geometries = new List<Geometry>();
            for (int i = 0; i < rawData.VehicleLanes.Count; i++)
            {
                if (i == 70 || i == 89) 
                {
                    int stop = 0;
                    continue;
                }

                tmpG = OverlayNGRobust.Overlay(tmpG, rawData.VehicleLanes[i].LaneObb.Buffer(0.1,PolygonUtils.MitreParam), NetTopologySuite.Operation.Overlay.SpatialFunction.Difference);
                for (int j = 0; j < rawData.VehicleLanes[i].ParkingPlaceBlockList.Count; j++)
                {
                    geometries.Add(rawData.VehicleLanes[i].ParkingPlaceBlockList[j].ParkingPlaceBlockObb);
                    tmpG = OverlayNGRobust.Overlay(tmpG, rawData.VehicleLanes[i].ParkingPlaceBlockList[j].ParkingPlaceBlockObb.Buffer(0.1, PolygonUtils.MitreParam), NetTopologySuite.Operation.Overlay.SpatialFunction.Difference);
                }
            }

            for (int i = 0; i < VehicleLane.Blocks.Count; i++) 
            {
                tmpG = OverlayNGRobust.Overlay(tmpG, VehicleLane.Blocks[i].Buffer(0.1, PolygonUtils.MitreParam), NetTopologySuite.Operation.Overlay.SpatialFunction.Difference);
            }

            //geometries.AddRange(LaneDeformationParas.Blocks);
            //GeometryCollection differenceObjs = new GeometryCollection(geometries.ToArray());
            //var freeAreeResult = OverlayNGRobust.Overlay(LaneDeformationParas.Boundary, differenceObjs, NetTopologySuite.Operation.Overlay.SpatialFunction.Difference);


            //List<Polygon> originalFreeAreaList = new List<Polygon>();
            //if (freeAreeResult is Polygon a)
            //{
            //    originalFreeAreaList.Add(a);
            //}
            //else if (freeAreeResult is GeometryCollection collection)
            //{
            //    foreach (var geo in collection.Geometries)
            //    {
            //        if (geo is Polygon pl)
            //        {
            //            originalFreeAreaList.Add(pl);
            //        }
            //    }
            //}


            
            if (tmpG is GeometryCollection collection)
            {
                foreach (var e in collection)
                {
                    if (e is Polygon)
                    {
                        List<Polygon> polygons = PolygonUtils.ClearBufferHelper((Polygon)e, -100,100);
                        //LDOutput.DrawTmpOutPut0.OriginalFreeAreaList.Add((Polygon)e);
                        LDOutput.DrawTmpOutPut0.OriginalFreeAreaList.AddRange(polygons);
                    }
                }
            }
            else if (tmpG is Polygon)
            {
                //LDOutput.DrawTmpOutPut0.OriginalFreeAreaList.Add((Polygon)tmpG);
                List<Polygon> polygons = PolygonUtils.ClearBufferHelper((Polygon)tmpG, -100, 100);
                LDOutput.DrawTmpOutPut0.OriginalFreeAreaList.AddRange(polygons);
            }

            originalFreeAreaList = LDOutput.DrawTmpOutPut0.OriginalFreeAreaList;
        }

        public void BuildFreeArea() 
        {
            Vector2D testVector = new Vector2D(0, 1);
            BuildFreeArea buildFreeBlock = new BuildFreeArea(originalFreeAreaList,testVector);
            buildFreeBlock.Pipeline();
            ProcessedData.FreeBlockList = buildFreeBlock.FreeAreaRecsList;

            List<Polygon> freeAreaRecsToDraw = new List<Polygon>();
            for (int i = 0; i < buildFreeBlock.FreeAreaRecsList.Count; i++) 
            {
                for (int j = 0; j < buildFreeBlock.FreeAreaRecsList[i].Count; j++) 
                {
                    freeAreaRecsToDraw.Add(buildFreeBlock.FreeAreaRecsList[i][j].Obb);
                }
            }
            LDOutput.DrawTmpOutPut0.FreeAreaRecs = freeAreaRecsToDraw;
        }
    }
}
