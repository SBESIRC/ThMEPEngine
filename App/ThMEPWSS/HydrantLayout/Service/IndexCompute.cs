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
using ThMEPWSS.HydrantLayout.Model;
using ThMEPWSS.HydrantLayout.Engine;
using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Data;

using NFox.Cad;
using ThMEPEngineCore.Diagnostics;


namespace ThMEPWSS.HydrantLayout.Service
{
    class IndexCompute
    {
        Point3d center;
        public IndexCompute(Point3d center)
        {
            this.center = center;
        }

        //-------------------------------------------
        //指标计算函数（此处只有一个计算贴墙长度的函数）


        //计算贴墙长度
        public double CalculateWallLength(Polyline wholeObb, Polyline singleLean)
        {
            double lengthAgainstWall = 0;

            var bufferObb = wholeObb.Buffer(20);
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

        public static double ComputeOverlapScore(Polyline area, Polyline shell, ThCADCoreNTSSpatialIndex forbidden) 
        {
            double score = 0;
            double overlapArea = ComputeOverlapArea(area, shell, forbidden);
            if (overlapArea == 0) return 100;
            else return ((area.Area - overlapArea) / area.Area);
        }

        public static double ComputeOverlapArea(Polyline area ,Polyline shell, ThCADCoreNTSSpatialIndex forbidden) 
        {
            double overlapArea = 0;
            var bufferArea = area.Buffer(-10);
            var pl = bufferArea.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
            //List<Polyline> pakings = ProcessedData.ParkingIndex.SelectCrossingPolygon(shell).OfType<Polyline>().ToList();
            DBObjectCollection pakings = ProcessedData.ParkingIndex.SelectCrossingPolygon(shell);

            if (pakings.Count > 0)
            {
                List<Polyline> obj = pl.Intersection(pakings).OfType<Polyline>().ToList();
                for (int i = 0; i < obj.Count; i++)
                {
                    overlapArea = overlapArea + obj[i].Area;
                }
                return overlapArea;
            }

            return 0;
            
        }
    }
}
