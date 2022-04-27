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
        public List<Line> HotPipeTopView { get; set; } = new List<Line>();
        public List<Line> CoolPipeTopView { get; set; } = new List<Line>();
        public List<Line> VerticalPipe { get; set; } = new List<Line>();
        public List<ThSaniterayTerminal> Terminal { get; set; } = new List<ThSaniterayTerminal>();

        public List<ThValve> Valve { get; set; } = new List<ThValve>(); //截止阀,闸阀,止回阀,防污隔断阀,天正阀,断管
        public List<ThValve> Casing { get; set; } = new List<ThValve>();//套管系统
        public List<ThValve> AngleValve { get; set; } = new List<ThValve>();//给水角阀平面

        //--- parameter
        public Point3d PrintBasePt { get; set; }
        public double qL { get; set; }//最高用水日的用水定额，[L/(人天)]  UI输入值
        public double m { get; set; } //每户用水人数	UI输入值
        public double Kh { get; set; } //小时变化系数 UI输入值

        //----output result
        public List<ThDrainageBlkOutput> OutputDim { get; set; } = new List<ThDrainageBlkOutput>();
        public List<ThDrainageBlkOutput> OutputAngleValve { get; set; } = new List<ThDrainageBlkOutput>();
        public List<ThDrainageBlkOutput> OutputValve { get; set; } = new List<ThDrainageBlkOutput>();
        public List<Line> OutputCoolPipe { get; set; } = new List<Line>();
        public List<Line> OutputHotPipe { get; set; } = new List<Line>();

    }
}
