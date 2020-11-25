using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    /// <summary>
    /// 记录次梁的轮廓以及次梁的高度
    /// </summary>
    public class SecondBeamProfileInfo
    {
        public Polyline Profile; // 次梁的轮廓

        public double Height;   // 次梁的高度

        public bool IsUsed = false;

        public bool IsHolePoly = false;

        public int OrderNum = 0;

        public List<SecondBeamProfileInfo> RelatedSecondBeams = new List<SecondBeamProfileInfo>(); // 记录相关联的信息

        public SecondBeamProfileInfo(Polyline poly, double height = 500)
        {
            Profile = poly;
            Height = height;
        }
    }
}
