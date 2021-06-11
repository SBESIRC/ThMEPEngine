using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Temp
{
    public interface IColumnData
    {
        List<Entity> OuterColumns { get; set; }
        List<Entity> OtherColumns { get; set; }
    }
}
