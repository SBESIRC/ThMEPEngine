using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    public enum BeamType
    {
        MainBeam,  // 主梁
        SecondBeam // 次梁
    }

    public class BeamProfile
    {
        //public BeamType Type; // 主次梁

        public Polyline Profile; // 梁的轮廓

        public double Height;   // 梁的高度

        public BeamProfile(Polyline poly, double height = 500)
        {
            Profile = poly;
            Height = height;
        }
    }

}
