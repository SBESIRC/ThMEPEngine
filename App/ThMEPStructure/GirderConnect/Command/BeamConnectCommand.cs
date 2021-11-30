using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Command;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;
using System;

namespace ThMEPStructure.GirderConnect.Command
{
    public class BeamConnectCommand : ThMEPBaseCommand, IDisposable
    {
        //input
        public Polyline frame { get; set; }//车库边界轮廓
        public List<Polyline> obstacles { get; set; }//内部障碍物轮廓
        public Dictionary<Polyline, List<Polyline>> houses { get; set; }//楼层和楼层上的剪力墙
        public List<Polyline> walls { get; set; } //墙体
        public List<Polyline> columns { get; set; }//柱子

        //output
        public List<Line> lines { get; set; } //主梁连接线

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var points = GetObject.GetCenters(acdb);
                //ConnectMainBeam.Calculate(points);
            }
        }
    }
}
