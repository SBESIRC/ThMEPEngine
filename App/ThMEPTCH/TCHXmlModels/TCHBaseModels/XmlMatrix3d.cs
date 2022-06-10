using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPTCH.TCHXmlModels.TCHBaseModels
{
    public class XmlMatrix3d:XmlString
    {
        public Matrix3d? GetCADMatrix3D() 
        {
            return null;
        }
    }
}
