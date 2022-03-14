using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.Models
{

    public class ThPDSCircuitWpfGraphRenderContext : ThPDSCircuitGraphRenderContext
    {
        public Canvas Canvas { get; set; }
    }
}