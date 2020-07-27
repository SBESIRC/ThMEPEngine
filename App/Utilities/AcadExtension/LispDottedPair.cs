using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace NFox.Cad.Collections
{
    public class LispDottedPair : LispList
    {

        protected override TypedValue ListEnd
        {
            get { return new TypedValue((int)LispDataType.DottedPair); }
        }

    }
}
