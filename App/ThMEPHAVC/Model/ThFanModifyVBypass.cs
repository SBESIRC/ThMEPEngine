using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThFanModifyVBypass
    {
        //public ThVTee vt;
        //private Tolerance tor;
        //private Matrix3d disMat;
        //private List<VTElbowModifyParam> vtElbows;
        //private ThModifyFanConnComponent service;
        //public ThFanModifyVBypass(string modifySize, ObjectId[] ductCompIds, DuctModifyParam param)
        //{
        //    using (var db = AcadDatabase.Active())
        //    {
        //        tor = new Tolerance(1.5, 1.5);
        //        ThDuctPortsInterpreter.GetVtElbow(out vtElbows);
        //        UpdateVElbowPos(param.sp);
        //        Get_vt_pos(f_detect, l_detect, param.ductSize, out Point3d i_vt_pos, out Point3d o_vt_pos);
        //        vt = new ThVTee(i_vt_pos, o_vt_pos, modifySize);
        //        var p = new Point3d(start_point.X, start_point.Y, 0);
        //        var vt_pinter = new ThDrawVBypass(param.airVolume, flag_param.scale, flag_param.scenario, p, modifySize, param.elevation.ToString());
        //        var line_type = Get_line_type(ids[0]);
        //        if (!(line_type == ThHvacCommon.DASH_LINETYPE))
        //            vt_pinter.Draw4VerticalBypass(vt.vtElbow, i_vt_pos, o_vt_pos);
        //        else
        //            vt_pinter.Draw5VerticalBypass(vt.vtElbow, i_vt_pos, o_vt_pos);
        //        service = new ThModifyFanConnComponent(p);
        //        var l = new Line(i_vt_pos, o_vt_pos);
        //        var pl = ThMEPHVACService.GetLineExtend(l, 10);
        //        var width = ThMEPHVACService.GetWidth(modifySize);
        //        service.UpdateValve(pl, Point3d.Origin, width);// 只更新电动多叶调节阀，不需要给new_p
        //    }
        //}
        //private void UpdateVElbowPos(Point3d startPoint)
        //{
        //    disMat = Matrix3d.Displacement(-startPoint.GetAsVector());
        //    foreach (var e in vtElbows)
        //        e.detectP = e.detectP.TransformBy(disMat);
        //}
        //private void Get_vt_pos(Point3d f_detect, 
        //                        Point3d l_detect, 
        //                        string org_size,
        //                        out Point3d i_vt_pos,
        //                        out Point3d o_vt_pos)
        //{
        //    var sp = new Point3d(f_detect.X, f_detect.Y, 0);
        //    var ep = new Point3d(l_detect.X, l_detect.Y, 0);
        //    var dir_vec = (ep - sp).GetNormal();
        //    ThMEPHVACService.GetWidthAndHeight(org_size, out double w, out double h);
        //    i_vt_pos = sp - (dir_vec * h * 0.5);
        //    o_vt_pos = ep + (dir_vec * h * 0.5);
        //}
        //private string Get_line_type(ObjectId id)
        //{
        //    using (var db = AcadDatabase.Active())
        //    {
        //        var l = db.Element<Entity>(id) as Line;
        //        return l.Linetype;
        //    }
        //}
    }
}
