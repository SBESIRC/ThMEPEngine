using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.DSFEL.Model
{
    public class RoomInfoModel
    {
        public Polyline room { get; set; }

        public List<Line> evacuationPaths { get; set; }

        public List<ExitModel> exitModels { get; set; }
    }
}
