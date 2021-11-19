using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Overlay.Snap;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;

using ThMEPStructure.Command;
using ThMEPStructure.GirderConnect.ConnectMainBeam.ConnectProcess;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;

namespace ThMEPStructure.GirderConnect.Command
{
    class BeamConnectCommand : ThMEPBaseCommand
    {
        //input
        public Polyline frame { get; set; }//车库边界轮廓
        public List<Polyline> obstacles { get; set; }//内部障碍物轮廓
        public Dictionary<Polyline, List<Polyline>> houses { get; set; }//楼层和楼层上的剪力墙
        public List<Polyline> walls { get; set; } //墙体
        public List<Polyline> columns { get; set; }//柱子

        //output
        public List<Line> lines { get; set; } //主梁连接线

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
