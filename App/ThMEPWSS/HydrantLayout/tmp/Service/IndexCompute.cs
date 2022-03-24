using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;


using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using ThMEPWSS.HydrantLayout.tmp.Model;
using ThMEPWSS.HydrantLayout.tmp.Engine;
using ThMEPWSS.HydrantLayout.tmp.Service;
using ThMEPWSS.HydrantLayout.Model;
using NFox.Cad;
using ThMEPEngineCore.Diagnostics;


namespace ThMEPWSS.HydrantLayout.tmp.Service
{
    class IndexCompute
    {
        //计算贴墙数量  ? 待讨论，是否移到外部       
        Point3d center;
        public IndexCompute(Point3d center)
        {
            this.center = center;
        }

        //指标函数

        //计算贴墙长度
        public double CalculateWallLength(Polyline wholeObb, Polyline singleLean)
        {
            double lengthAgainstWall = 0;

            var bufferObb = wholeObb.Buffer(10);
            var pl = bufferObb.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();

            DrawUtils.ShowGeometry(pl, "l1buffer", 3);
            //var trimedWall = singleLean.Trim(pl);
            if (singleLean.Contains(pl)) return 0.0;
            else
            {
                var trimedWall = pl.Trim(singleLean);
                var newpl = trimedWall.OfType<Curve>().ToList();


                lengthAgainstWall = newpl.Sum(x => x.GetLength());
                newpl.ForEach(x => DrawUtils.ShowGeometry(x, "l1against", 2));

                return lengthAgainstWall;
            }
        }
    }
}
