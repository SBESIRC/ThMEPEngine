using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.DrainageADPrivate.Model
{
    internal class ThDrainageADPDataPass
    {
        //--- data
        public List<Line> HotPipeTopView { get; set; }
        public List<Line> CoolPipeTopView { get; set; }
        public List<Line> ADHotPipe { get; set; }
        public List<Line> ADCoolPipe { get; set; }
        public List<Line> VerticalPipe { get; set; }
        public Point3d StartPt { get; set; }
        public List<ThSaniterayTerminal> Terminal { get; set; }

        //--- parameter
        public double qL { get; set; }//最高用水日的用水定额，[L/(人天)]  UI输入值
        public double m { get; set; } //每户用水人数	UI输入值
        public double Kh { get; set; } //小时变化系数 UI输入值
     

    }
}
