using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.DrainageGeneralPlan.Utils;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThCADCore.NTS;
using NFox.Cad;

namespace ThMEPWSS.DrainageGeneralPlan.Service
{
    public class ThDrainageGeneralPlanDataService
    {
        /// <summary>
        /// 对主干线进行分组,对main做index
        /// </summary>
        /// <param name="mainP"></param>
        /// <param name="outP"></param>
        public static void Group(List<Line> mainP,List<Polyline> outP, Dictionary<Line, List<Polyline>> mainToOut, Dictionary<Polyline, List<Line>> outToMain)
        {
            for(int i=0; i < mainP.Count; i++)
            {
                var line = mainP[i].ToPolyline();//转成pl
                var mainIndex = new ThCADCoreNTSSpatialIndex(outP.ToCollection());//main索引
                var mBuffer = line.BufferPL(7)[0] as Polyline;
                var nearLine = mainIndex.SelectCrossingPolygon(mBuffer);
                //mainToOut
                for(int j=0; j < nearLine.Count; j++)
                {
                    //outToMain[]
                }
            }
        }
    }
}
