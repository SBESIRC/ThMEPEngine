using System;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper.Commands;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.Service;
using ThMEPWSS.Bussiness;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Engine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace ThMEPWSS.Command
{
    using ThMEPEngineCore.Engine;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.Diagram.ViewModel;
    using ThMEPWSS.JsonExtensionsNs;
    using ThMEPWSS.Pipe.Service;

    //雨水排水系统图
    public class ThRainSystemDiagramCmd : IAcadCommand
    {
        RainSystemDiagramViewModel _vm = null;
        public ThRainSystemDiagramCmd(RainSystemDiagramViewModel vm = null)
        {
            _vm = vm;
        }
        private static Tuple<Point3d, Point3d> SelectPoints()
        {
            return ThMEPWSS.Common.Utils.SelectPoints();
        }

        public void Execute()
        {
        }
    }
}
