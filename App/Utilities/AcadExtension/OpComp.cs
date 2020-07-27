using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace NFox.Cad.Collections
{
    public class OpComp : OpEqual
    {

        public string Contant { get; }

        public override string Name
        {
            get { return "Comp"; }
        }

        public OpComp(string content, TypedValue value)
            : base(value)
        {
            Contant = content;
        }

        public OpComp(string content, int code)
            : base(code)
        {
            Contant = content;
        }

        public OpComp(string content, int code, object value)
            : base(code, value)
        {
            Contant = content;
        }

        public OpComp( string content, DxfCode code, object value)
            : base(code, value)
        {
            Contant = content;
        }

        public override IEnumerable<TypedValue> GetValues()
        {
            yield return new TypedValue(-4, Contant);
            yield return Value;
        }

    }
}
