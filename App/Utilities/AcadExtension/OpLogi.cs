using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections;

namespace NFox.Cad.Collections
{
    public abstract class OpLogi : OpFilter, IEnumerable<OpFilter>
    {

        public TypedValue First
        {
            get { return new TypedValue(-4, $"<{Name}"); }
        }

        public TypedValue Last
        {
            get { return new TypedValue(-4, $"{Name}>"); }
        }

        public override IEnumerable<TypedValue> GetValues()
        {
            yield return First;
            foreach (var item in this)
            {
                foreach (var value in item.GetValues())
                    yield return value;
            }
            yield return Last;
        }

        public abstract IEnumerator<OpFilter> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class OpNot : OpLogi
    {

        private OpFilter Value { get; }

        public OpNot(OpFilter value)
        {
            Value = value;
        }

        public override string Name
        {
            get { return "Not"; }
        }

        public override IEnumerator<OpFilter> GetEnumerator()
        {
            yield return Value;
        }
    }

    public class OpXor : OpLogi
    {

        public OpFilter Left { get; }
        public OpFilter Right { get; }

        public OpXor(OpFilter left, OpFilter right)
        {
            Left = left;
            Right = right;
        }

        public override string Name
        {
            get { return "Xor"; }
        }

        public override IEnumerator<OpFilter> GetEnumerator()
        {
            yield return Left;
            yield return Right;
        }
    }


}
