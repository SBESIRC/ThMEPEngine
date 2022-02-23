using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawValve
    {
        public string valveName;
        public string valveLayer;
        public string valveVisibility;
        public ThDuctPortsDrawValve(string valveVisibility, string valveName, string valveLayer)
        {
            this.valveName = valveName;
            this.valveLayer = valveLayer;
            this.valveVisibility = valveVisibility;
        }
        public void InsertValve(Point3d srtPoint, List<EndlineInfo> endlines)
        {
            if (endlines.Count == 1)// 只有一条endline的情况不插阀
                return;
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var endline in endlines)
                {
                    var rootSeg = endline.endlines.Values.LastOrDefault();
                    var width = ThMEPHVACService.GetWidth(rootSeg.seg.ductSize);
                    var dirVec = ThMEPHVACService.GetEdgeDirection(rootSeg.seg.l);
                    var verticalR = ThMEPHVACService.GetRightVerticalVec(dirVec);
                    var angle = dirVec.GetAngleTo(Vector3d.XAxis);
                    if (Vector3d.XAxis.CrossProduct(dirVec).Z < 0)
                        angle = 2 * Math.PI - angle;
                    angle += 0.5 * Math.PI;
                    var textAngle = (angle >= Math.PI * 0.5) ? Math.PI * 0.5 : 0;
                    var p = rootSeg.seg.l.StartPoint + (dirVec * rootSeg.seg.srcShrink);
                    var insertP = p + verticalR * width * 0.5 + srtPoint.GetAsVector();
                    InsertValve(insertP, width, angle, textAngle);
                }
            }
        }
        public void InsertValves(Point3d srtPoint, List<EndlineInfo> endlines, string visibility)
        {
            if (endlines.Count == 1)// 只有一条endline的情况不插阀
                return;
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var endline in endlines)
                {
                    var rootSeg = endline.endlines.Values.LastOrDefault();
                    var width = ThMEPHVACService.GetWidth(rootSeg.seg.ductSize);
                    var dirVec = ThMEPHVACService.GetEdgeDirection(rootSeg.seg.l);
                    var verticalR = ThMEPHVACService.GetRightVerticalVec(dirVec);
                    var angle = dirVec.GetAngleTo(Vector3d.XAxis);
                    if (Vector3d.XAxis.CrossProduct(dirVec).Z < 0)
                        angle = 2 * Math.PI - angle;
                    angle += 0.5 * Math.PI;
                    var textAngle = (angle >= Math.PI * 0.5) ? Math.PI * 0.5 : 0;
                    var p = rootSeg.seg.l.StartPoint + (dirVec * rootSeg.seg.srcShrink);
                    var insertP = p + verticalR * width * 0.5 + srtPoint.GetAsVector();
                    InsertValve(insertP, width, angle, textAngle);
                    insertP += (dirVec * 320);// 320是默认阀宽
                    InsertValve(insertP, width, angle, textAngle, visibility);
                }
            }
        }
        public void InsertValve(Point3d insertP, double width, double angle, double textAngle, string visibility)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var obj = db.ModelSpace.ObjectId.InsertBlockReference(valveLayer, valveName, insertP, new Scale3d(), angle);
                ThDuctPortsDrawService.SetValveDynBlockProperity(obj, width, 250, textAngle, visibility);
            }
        }
        public void InsertValve(Point3d insertP, double width, double angle, double textAngle)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var obj = db.ModelSpace.ObjectId.InsertBlockReference(valveLayer, valveName, insertP, new Scale3d(), angle);
                ThDuctPortsDrawService.SetValveDynBlockProperity(obj, width, 250, textAngle, valveVisibility);
            }
        }
        public void InsertHole(Point3d insertP, double width, double len, double angle)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var obj = db.ModelSpace.ObjectId.InsertBlockReference(valveLayer, valveName, insertP, new Scale3d(), angle);
                ThDuctPortsDrawService.SetHoleDynBlockProperity(obj, width, len);
            }
        }
        public void InsertMuffler(Point3d insertP, MufflerModifyParam muffler)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var obj = db.ModelSpace.ObjectId.InsertBlockReference(valveLayer, valveName, insertP, new Scale3d(), muffler.rotateAngle);
                ThDuctPortsDrawService.SetMufflerDynBlockProperity(obj, muffler);
            }
        }
    }
}
