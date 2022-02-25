using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.DrainageSystemDiagram;
using ThMEPWSS.SprinklerPiping.Engine;
using ThMEPWSS.SprinklerPiping.Model;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Engine;
using ThMEPWSS.TowerSeparation.TowerExtract;

namespace ThMEPWSS.SprinklerPiping.Engine
{
    class SprinklerSceneDivisionEngine
    {
        public static void SceneDivision(SprinklerPipingParameter parameter)
        {
            Polyline frame = parameter.frame;
            List<SprinklerPoint> sprinklerPoints = parameter.sprinklerPoints;
            List<Polyline> parkingRows = parameter.parkingRows;
            List<Polyline> rooms = parameter.dataQuery.ArchitectureWallList;
            bool isParallel = parameter.isParallel;

            var objs = new DBObjectCollection();
            sprinklerPoints.ForEach(x => objs.Add(new DBPoint(x.pos)));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);

            //塔楼
            //var towerExtractor = new TowerExtractor();
            //var towers = towerExtractor.Extractor(parameter.dataQuery.ShearWallList, frame);  
            //foreach (var tower in towers)
            //{

            //    var towerPts = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(tower).Cast<DBPoint>().Select(x => x.Position).ToList();

            //    foreach (var pt in towerPts)
            //    {
            //        if (parameter.ptDic[pt].scene != Scenes.Others) continue;
            //        parameter.ptDic[pt].scene = Scenes.Tower;
            //    }
            //}

            //小房间
            //foreach (var room in rooms)
            //{
            //    if (room.Area < parameter.roomArea)
            //    {
            //        //var objs = new DBObjectCollection();
            //        //sprinklerPoints.ForEach(x => objs.Add(new DBPoint(x.pos)));
            //        //ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            //        var roomPts = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room).Cast<DBPoint>().Select(x => x.Position).ToList();

            //        foreach (var pt in roomPts)
            //        {
            //            if (parameter.ptDic[pt].scene != Scenes.Others) continue;
            //            parameter.ptDic[pt].scene = Scenes.SmallRoom;
            //        }
            //    }
            //}

            //车位排
            foreach (var parkingRow in parkingRows)
            {
                //var objs = new DBObjectCollection();
                //sprinklerPoints.ForEach(x => objs.Add(new DBPoint(x.pos)));
                //ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var parkingPts = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(parkingRow).Cast<DBPoint>().Select(x => x.Position).ToList();

                foreach (var pt in parkingPts)
                {
                    parameter.ptDic[pt].scene = Scenes.Parking;
                    parameter.parkingCnt++;
                }
            }


            //foreach (var pt in sprinklerPoints)
            //{
            //    if (pt.scene != Scenes.Others) continue;
            //    //车位排
            //    //foreach (var parkingRow in parkingRows)
            //    //{
            //    //    if (parkingRow.Contains(pt.pos))
            //    //    {
            //    //        Line lane = new Line(parkingRow.GetPoint3dAt(0), parkingRow.GetPoint3dAt(1));
            //    //        double branchAngle = (lane.Angle + Math.PI / 2) % Math.PI;

            //    //        if ((branchAngle == pt.ucsAngle) ^ isParallel)
            //    //        {
            //    //            pt.branchDir = 0;
            //    //        }
            //    //        else
            //    //        {
            //    //            pt.branchDir = 1;
            //    //        }
            //    //        pt.scene = Scenes.Parking;
            //    //        parameter.parkingCnt++;
            //    //        break;
            //    //    }
            //    //}
            //    //if (pt.scene != Scenes.Others) continue;
            //    //TODO:小房间和塔楼划分


            //    //走廊
            //    //TODO: 非坡道走廊和坡道走廊也许需要分开（非坡道可以连长支干管）
            //    List<SprinklerPoint> xdirPts = new List<SprinklerPoint>();
            //    List<SprinklerPoint> ydirPts = new List<SprinklerPoint>();

            //    //不算自己
            //    int corridorPtUpperThr = 6;
            //    int corridorPtLowerThr = 3;

            //    int xcnt = 0, ycnt = 0;
            //    SprinklerPoint curPt = pt.downNeighbor;
            //    while (curPt != null && ycnt < corridorPtUpperThr)
            //    {
            //        ycnt++;
            //        ydirPts.Add(curPt);
            //        curPt = curPt.downNeighbor;
            //    }
            //    curPt = pt.upNeighbor;
            //    while(curPt != null && ycnt < corridorPtUpperThr)
            //    {
            //        ycnt++;
            //        ydirPts.Add(curPt);
            //        curPt = curPt.upNeighbor;
            //    }
            //    curPt = pt.leftNeighbor;
            //    while(curPt != null && xcnt < corridorPtUpperThr)
            //    {
            //        xcnt++;
            //        xdirPts.Add(curPt);
            //        curPt = curPt.leftNeighbor;
            //    }
            //    curPt = pt.rightNeighbor;
            //    while (curPt != null && xcnt < corridorPtUpperThr)
            //    {
            //        xcnt++;
            //        xdirPts.Add(curPt);
            //        curPt = curPt.rightNeighbor;
            //    }
            //    if(xcnt < corridorPtLowerThr && ycnt == corridorPtUpperThr)
            //    {
            //        Vector3d verDir;
            //        Point3d pt1, pt2;
            //        double dist;
            //        if (xcnt == 0)
            //        {
            //            verDir = pt.pos - ((pt.upNeighbor == null) ? pt.downNeighbor.pos : pt.upNeighbor.pos);
            //            pt1 = frame.GetClosestPointTo(pt.pos, verDir.RotateBy(Math.PI / 2, Vector3d.ZAxis), false);
            //            pt2 = frame.GetClosestPointTo(pt.pos, verDir.RotateBy(Math.PI / 2 * 3, Vector3d.ZAxis), false);
            //            dist = pt1.DistanceTo(pt2);
            //        }
            //        else
            //        {
            //            verDir = pt.pos - ((pt.rightNeighbor == null) ? pt.leftNeighbor.pos : pt.rightNeighbor.pos);
            //            pt1 = frame.GetClosestPointTo(pt.pos, verDir, false);
            //            pt2 = frame.GetClosestPointTo(pt.pos, verDir.RotateBy(Math.PI, Vector3d.ZAxis), false);
            //            dist = pt1.DistanceTo(pt2);
            //        }
            //        if(dist <= 8000) pt.scene = Scenes.NarrowCorridor;
            //        else pt.scene = Scenes.Corridor;
            //        pt.branchDir = 0;
            //        break;
            //    }
            //    else if(xcnt == corridorPtUpperThr && ycnt < corridorPtLowerThr)
            //    {
            //        Vector3d verDir;
            //        Point3d pt1, pt2;
            //        double dist;
            //        if (ycnt == 0)
            //        {
            //            verDir = pt.pos - ((pt.rightNeighbor == null) ? pt.leftNeighbor.pos : pt.rightNeighbor.pos);
            //            pt1 = frame.GetClosestPointTo(pt.pos, verDir.RotateBy(Math.PI / 2, Vector3d.ZAxis), false);
            //            pt2 = frame.GetClosestPointTo(pt.pos, verDir.RotateBy(Math.PI / 2 * 3, Vector3d.ZAxis), false);
            //            dist = pt1.DistanceTo(pt2);
            //        }
            //        else
            //        {
            //            verDir = pt.pos - ((pt.upNeighbor == null) ? pt.downNeighbor.pos : pt.upNeighbor.pos);
            //            pt1 = frame.GetClosestPointTo(pt.pos, verDir, false);
            //            pt2 = frame.GetClosestPointTo(pt.pos, verDir.RotateBy(Math.PI, Vector3d.ZAxis), false);
            //            dist = pt1.DistanceTo(pt2);
            //        }
            //        if (dist <= 8000) pt.scene = Scenes.NarrowCorridor;
            //        else pt.scene = Scenes.Corridor;
            //        pt.branchDir = 1;
            //        break;
            //    }
            //}
            return;
        }
    }
}
