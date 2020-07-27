using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFox.Cad.Collections
{
    public class OpEqual : OpFilter
    {

        public TypedValue Value { get; private set; }

        public override string Name
        {
            get { return "Equal"; }
        }

        public OpEqual(int code)
        {
            Value = new TypedValue(code);
        }

        public OpEqual(int code, object value)
        {
            Value = new TypedValue(code, value);
        }

        public OpEqual(DxfCode code, object value)
        {
            Value = new TypedValue((int)code, value);
        }

        internal OpEqual(TypedValue value)
        {
            Value = value;
        }

        public override IEnumerable<TypedValue> GetValues()
        {
            yield return Value;
        }

        public void SetValue(object value)
        {
            Value = new TypedValue(Value.TypeCode, value);
        }

        public void SetValue(int code, object value)
        {
            Value = new TypedValue(code, value);
        }

    }
}
