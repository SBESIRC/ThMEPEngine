﻿using System;
using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawDim
    {
        public string scale;
        public string dimensionLayer;
        public ThDuctPortsDrawDim(string dimensionLayer, string scale)
        {
            this.scale = scale;
            this.dimensionLayer = dimensionLayer;
        }
        public void DrawDimension(List<EndlineInfo> infos, Point3d startPos)
        {
            foreach (var endline in infos)
            {
                InsertVerDimension(endline, startPos);
                foreach (var seg in endline.endlines.Values)
                    InsertDirDimension(seg, startPos);
            }
        }
        private void InsertVerDimension(EndlineInfo seg, Point3d startPos)
        {
            using (var db = AcadDatabase.Active())
            {
                if (seg.verAlignPoint.IsEqualTo(Point3d.Origin, new Tolerance(1e-3, 1e-3)))
                    return;
                // 最后一根管段上一定有风口
                var endSeg = seg.endlines.Values.LastOrDefault();
                if (endSeg.portNum != 0)
                {
                    var dirVec = ThMEPHVACService.GetEdgeDirection(endSeg.seg.l);
                    var lastPort = endSeg.portsInfo.First();
                    var p = lastPort.position + startPos.GetAsVector();
                    var layerId = db.Layers.ElementOrDefault(dimensionLayer).ObjectId;
                    var dim = CreateAlignDim(seg.verAlignPoint, p, dirVec, layerId);
                    db.ModelSpace.Add(dim);
                    dim.SetDatabaseDefaults();
                }
                else
                    throw new NotImplementedException("[CheckError]: 最末端管一定有风口");
            }
        }
        // 在一条endline上插dimision
        private void InsertDirDimension(EndlineSegInfo seg, Point3d startPos)
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                if (!seg.dirAlignPoint.IsEqualTo(Point3d.Origin, new Tolerance(1e-3, 1e-3)))
                    InsertDirWallPoint(seg);
                var disVec = startPos.GetAsVector();
                var dirVec = ThMEPHVACService.GetEdgeDirection(seg.seg.l);
                var verticalVec = GetDimensionVerticalVec(dirVec);
                var layerId = db.Layers.ElementOrDefault(dimensionLayer).ObjectId;
                for (int i = 1; i < seg.portsInfo.Count(); ++i)
                {
                    var portsInfo = seg.portsInfo;
                    var srtP = portsInfo[i - 1].position + disVec;
                    var endP = portsInfo[i].position + disVec;
                    var dim = CreateAlignDim(srtP, endP, verticalVec, layerId);
                    db.ModelSpace.Add(dim);
                    dim.SetDatabaseDefaults();
                }
            }
        }
        private AlignedDimension CreateAlignDim(Point3d p1, Point3d p2, Vector3d verticalVec, ObjectId layerId)
        {
            string style = ThMEPHVACService.GetDimStyle(scale);
            using (var adb = AcadDatabase.Active())
            {
                var id = Dreambuild.AutoCAD.DbHelper.GetDimstyleId(style, adb.Database);
                return new AlignedDimension
                {
                    XLine1Point = p1,
                    XLine2Point = p2,
                    DimensionText = "",
                    DimLinePoint = ThMEPHVACService.GetMidPoint(p1, p2) + verticalVec * 2000,
                    ColorIndex = 256,
                    DimensionStyle = id,
                    LayerId = layerId,
                    Linetype = "ByLayer"
                };
            }
        }
        private void InsertDirWallPoint(EndlineSegInfo seg)
        {
            if (!ThMEPHVACService.IsVertical(seg.seg.l))
            {
                //非垂直的按X排序
                InsertWallPointByX(seg, seg.dirAlignPoint);
            }
            else
            {
                //按Y排序
                InsertWallPointByY(seg, seg.dirAlignPoint);
            }
        }

        private void InsertWallPointByY(EndlineSegInfo endline, Point3d wallP)
        {
            var Ys = new List<double>();
            foreach (var port in endline.portsInfo)
                Ys.Add(port.position.Y);
            var ascending = Ys[0] < Ys[Ys.Count - 1];
            var falgY = wallP.Y;
            Ys.Add(falgY);
            if (ascending)
                Ys.Sort();
            else
                Ys.Sort((x, y) => -x.CompareTo(y));
            var idx = Ys.IndexOf(falgY);
            endline.portsInfo.Insert(idx, new PortInfo() { position = wallP, portAirVolume = -1 });
        }
        private void InsertWallPointByX(EndlineSegInfo endline, Point3d wallP)
        {
            var Xs = new List<double>();
            foreach (var port in endline.portsInfo)
                Xs.Add(port.position.X);
            var ascending = Xs[0] < Xs[Xs.Count - 1];
            var falgX = wallP.X;
            Xs.Add(falgX);
            if (ascending)
                Xs.Sort();
            else
                Xs.Sort((x, y) => -x.CompareTo(y));
            var idx = Xs.IndexOf(falgX);
            endline.portsInfo.Insert(idx, new PortInfo() { position = wallP, portAirVolume = -1 });
        }
        private Vector3d GetDimensionVerticalVec(Vector3d dirVec)
        {
            Vector3d verticalVec;
            if (Math.Abs(dirVec.X) < 1e-3)
            {
                verticalVec = (dirVec.Y > 0) ? ThMEPHVACService.GetRightVerticalVec(dirVec) :
                                               ThMEPHVACService.GetLeftVerticalVec(dirVec);
            }
            else if (dirVec.X > 0)
                verticalVec = ThMEPHVACService.GetRightVerticalVec(dirVec);
            else
                verticalVec = ThMEPHVACService.GetLeftVerticalVec(dirVec);
            return verticalVec;
        }
    }
}