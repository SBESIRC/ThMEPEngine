using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    class RoofCollisionCheck
    {
        List<Polyline> _allRailings;
        List<FloorFramed> _floorFrameds;
        double _warningLength = 400;
        double _warningLineWidth = 100;
        string _warningLineLayer = "W-辅助";
        double _railingOutExtentDis = 100;
        public RoofCollisionCheck(List<FloorFramed> floorFrameds, List<Polyline> allRailings)
        {
            _allRailings = allRailings;
            _floorFrameds = floorFrameds;
        }
        public List<CreateBasicElement> GetCheckResults(List<CreateBlockInfo> checkBlocks)
        {
            var createBasics = new List<CreateBasicElement>();
            if (null == _allRailings || _allRailings.Count < 1 || _floorFrameds == null || _floorFrameds.Count < 1 || checkBlocks == null || checkBlocks.Count < 1)
                return createBasics;
            foreach (var floor in _floorFrameds)
            {
                var thisFloorCheck = checkBlocks.Where(c => c.floorId == floor.floorUid).ToList();
                if (thisFloorCheck == null || thisFloorCheck.Count < 1)
                    continue;
                var thisFloorRailings = FloorRailings(floor);
                if (thisFloorRailings.Count < 1)
                    continue;
                foreach (var item in thisFloorCheck)
                {
                    bool isColl = false;
                    foreach (var railing in thisFloorRailings)
                    {
                        isColl = railing.Contains(item.createPoint) || (railing.Buffer(_railingOutExtentDis)[0] as Polyline).Contains(item.createPoint);
                        if (isColl)
                            break;
                    }
                    if (isColl)
                    {
                        var pl = WarningPLine(item.createPoint);
                        createBasics.Add(new CreateBasicElement(floor.floorUid, pl, _warningLineLayer, "", "",Color.FromRgb(255,0,0)));
                    }
                }
            }
            return createBasics;
        }
        List<Polyline> FloorRailings(FloorFramed floorFramed)
        {
            var railings = new List<Polyline>();
            if (null == _allRailings || _allRailings.Count < 1)
                return railings;
            var plGeo = floorFramed.outPolyline.ToNTSPolygon();
            foreach (var item in _allRailings)
            {
                var itemGeo = item.ToNTSPolygon();
                if (plGeo.Intersects(itemGeo) || plGeo.Crosses(itemGeo))
                    railings.Add(item);
            }
            return railings;
        }
        Polyline WarningPLine(Point3d centerPoint)
        {
            var xOffSet = Vector3d.XAxis.MultiplyBy(_warningLength / 2);
            var yOffSet = Vector3d.YAxis.MultiplyBy(_warningLength / 2);
            var topRight = centerPoint + xOffSet + yOffSet;
            var topLeft = centerPoint + yOffSet - xOffSet;
            var bottomLeft = centerPoint - xOffSet - yOffSet;
            var bottomRight = centerPoint - yOffSet + xOffSet;
            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, new Point2d(topLeft.X, topLeft.Y), 0, 0, 0);
            polyline.AddVertexAt(1, new Point2d(topRight.X, topRight.Y), 0, 0, 0);
            polyline.AddVertexAt(2, new Point2d(bottomRight.X, bottomRight.Y), 0, 0, 0);
            polyline.AddVertexAt(3, new Point2d(bottomLeft.X, bottomLeft.Y), 0, 0, 0);
            polyline.ConstantWidth = _warningLineWidth;
            return polyline;
        }
    } 
}