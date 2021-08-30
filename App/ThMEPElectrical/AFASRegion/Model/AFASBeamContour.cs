using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.AFASRegion.Model
{
    /// <summary>
    /// 梁 轮廓
    /// </summary>
    public class AFASBeamContour
    {
        public static double WallThickness { get; set; } = 100;
        public double Height { get; set; }
        public double Width { get; set; }
        public double BottomElevation { get; set; } //梁底标高，包含楼板高度
        public Point3d StartPoint { get; set; }
        public Point3d EndPoint { get; set; }
        public Polyline BeamBoundary { get; set; }
        public Line BeamCenterline { get; set; }
        public BeamType BeamType
        {
            get
            {
                double height = this.BottomElevation - WallThickness;
                if (height > 600)
                    return BeamType.HighBeam;
                else if (height < 200)
                    return BeamType.LowBeam;
                else
                    return BeamType.MiddleBeam;
            }
        }

        public AFASBeamContour Clone()
        {
            return new AFASBeamContour()
            {
                Height = this.Height,
                Width = this.Width,
                BottomElevation = this.BottomElevation,
                StartPoint = this.StartPoint,
                EndPoint = this.EndPoint,
                BeamBoundary = this.BeamBoundary,
                BeamCenterline = this.BeamCenterline,
            };
        }
    }

    public enum BeamType
    {
        HighBeam,
        MiddleBeam,
        LowBeam
    }
}
