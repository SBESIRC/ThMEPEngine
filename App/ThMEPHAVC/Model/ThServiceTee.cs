using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Model
{
    public class ThServiceTee
    {
        internal static bool Is_bypass(Point3d tar_srt_pos,
                                       Point3d tar_end_pos,
                                       DBObjectCollection bypass_lines)
        {
            if (bypass_lines == null || bypass_lines.Count == 0)
                return false;
            var tor = new Tolerance(5, 5);
            Line dect_line = new Line(tar_srt_pos, tar_end_pos);
            foreach (Line l in bypass_lines)
            {
                if (ThMEPHVACService.Is_same_line(dect_line, l, tor))
                    return true;
            }
            return false;
        }
    }
}
