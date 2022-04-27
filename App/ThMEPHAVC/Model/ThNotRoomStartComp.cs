using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Model
{
    public class ThNotRoomStartComp
    {
        public static void DrawEndLineEndComp(ref List<SegInfo> fanDucts, Point3d startPoint, PortParam portParam)
        {
            var rootDuct = SearchRootDuct(fanDucts);
            var insertP = rootDuct.l.EndPoint;
            var tor = new Tolerance(1.5, 1.5);
            var endP = ThMEPHVACService.GetOtherPoint(rootDuct.l, insertP, tor);
            var dirVec = (insertP - endP).GetNormal();
            InsertComp(rootDuct, dirVec, startPoint, insertP, portParam);
        }
        public static void InsertComp(SegInfo shadowRootDuct, Vector3d dirVec, Point3d startPoint, Point3d insertP, PortParam portParam)
        {
            var service = new ThDuctPortsDrawService(portParam.param.scenario, portParam.param.scale);
            var rootDuct = new SegInfo(shadowRootDuct);
            var mat = Matrix3d.Displacement(startPoint.GetAsVector());
            var roAngle = dirVec.GetAngleTo(-Vector3d.YAxis);
            ThMEPHVACService.GetWidthAndHeight(rootDuct.ductSize, out double w, out double h);
            if (portParam.endCompType == EndCompType.DownFlip45)
            {
                UpdateDownFlip45(rootDuct, h);
                insertP += startPoint.GetAsVector();
                roAngle += (Math.PI * 0.5);
                service.endCompService.InsertFlipDown45(insertP, w, h, -roAngle);
                var disVec = dirVec * 1000 + new Vector3d(0, -1000, 0);
                var markP = insertP + disVec;
                service.markService.InsertLeader(insertP, markP);
                service.textService.DrawTextInfo("45°下翻，加防虫网", portParam.param.scale, markP);
            }
            else if (portParam.endCompType == EndCompType.RainProofShutter)
            {
                UpdateRainProofShutter(mat, rootDuct, service);
                insertP += (startPoint.GetAsVector());
                service.portService.InsertPort(insertP, -roAngle, w, h, "外墙防雨百叶", rootDuct.airVolume);
            }
            else if (portParam.endCompType == EndCompType.VerticalPipe)
            {
                UpdateVerticalPipe(dirVec, h, ref insertP);
                insertP += (startPoint.GetAsVector());
                service.endCompService.InsertVerticalPipe(insertP, w, h, roAngle, rootDuct.airVolume);
            }
        }
        public static void UpdateVerticalPipe(Vector3d dirVec, double h, ref Point3d insertP)
        {
            insertP += (dirVec * 0.5 * h);
        }

        public static void UpdateDownFlip45(SegInfo rootDuct, double h)
        {
            rootDuct.dstShrink = h;
        }
        public static void UpdateRainProofShutter(Matrix3d mat, SegInfo rootDuct, ThDuctPortsDrawService service)
        {
            rootDuct.dstShrink = 500;
            var sp = rootDuct.l.EndPoint;
            var tor = new Tolerance(1.5, 1.5);
            var otherP = ThMEPHVACService.GetOtherPoint(rootDuct.l, sp, tor);
            var dirVec = (sp - otherP).GetNormal();
            var w = ThMEPHVACService.GetWidth(rootDuct.ductSize);
            var l = new Line(sp - 500 * dirVec, sp - 200 * dirVec);
            var reducing = ThDuctPortsFactory.CreateReducing(l, w, w, false);
            var reducings = new List<LineGeoInfo>() { reducing };
            service.DrawReducing(reducings, mat);
        }
        public static SegInfo SearchRootDuct(List<SegInfo> fanDucts)
        {
            var rootSeg = new SegInfo();
            var maxVal = Double.MinValue;
            foreach (var seg in fanDucts)
            {
                if (seg.airVolume > maxVal)
                {
                    maxVal = seg.airVolume;
                    rootSeg = seg;
                }
            }
            return rootSeg;
        }
    }
}
