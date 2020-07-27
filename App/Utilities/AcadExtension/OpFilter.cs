using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Autodesk.AutoCAD.Geometry;

namespace NFox.Cad.Collections
{
    public abstract class OpFilter
    {

        public abstract string Name { get; }

        public abstract IEnumerable<TypedValue> GetValues();

        public static OpFilter operator !(OpFilter item)
        {
            return item.Not;
        }

        public OpFilter Not
        {
           get { return new OpNot(this); }
        }

        public TypedValue[] ToArray()
        {
            return GetValues().ToArray();
        }

        public static implicit operator SelectionFilter(OpFilter item)
        {
            return new SelectionFilter(item.ToArray());
        }

        public override string ToString()
        {
            string s = "";
            foreach (var value in GetValues())
                s += value.ToString();
            return s;
        }

        #region Expression

        public class Op
        {

           internal  OpFilter Filter { get; private set; }

            internal Op() { }

            private Op(OpFilter filter)
            {
                Filter = filter;
            }

            public Op And(params Op[] args)
            {
                var filter = new OpAnd();
                foreach(var op in args)
                    filter.Add(op.Filter);
                return new Op(filter);
            }

            public Op Or(params Op[] args)
            {
                var filter = new OpOr();
                foreach (var op in args)
                    filter.Add(op.Filter);
                return new Op(filter);
            }

            public Op Dxf(int code)
            {
                return new Op(new OpEqual(code));
            }

            public Op Dxf(int code, string content)
            {
                return new Op(new OpComp(content, code));
            }

            public static Op operator !(Op right)
            {
                right.Filter = !right.Filter;
                return right;
            }

            public static Op operator ==(Op left, object right)
            {
                var eq = (OpEqual)left.Filter;
                eq.SetValue(right);
                return left;
            }


            public static Op operator !=(Op left, object right)
            {
                var eq = (OpEqual)left.Filter;
                eq.SetValue(right);
                left.Filter = eq.Not;
                return left;
            }

            private static Op GetCompOp(string content, Op left, object right)
            {
                var eq = (OpEqual)left.Filter;
                var comp = new OpComp(content, eq.Value.TypeCode, right);
                return new Op(comp);
            }

            public static Op operator >(Op left, object right)
            {
                return GetCompOp(">", left, right);
            }

            public static Op operator <(Op left, object right)
            {
                return GetCompOp("<", left, right);
            }

            public static Op operator >=(Op left, object right)
            {
                return GetCompOp(">=", left, right);
            }

            public static Op operator <=(Op left, object right)
            {
                return GetCompOp("<=", left, right);
            }

            public static Op operator >=(Op left, Point3d right)
            {
                return GetCompOp(">,>,*", left, right);
            }

            public static Op operator <=(Op left, Point3d right)
            {
                return GetCompOp("<,<,*", left, right);
            }

            public static Op operator &(Op left, Op right)
            {
                var filter = new OpAnd();
                filter.Add(left.Filter);
                filter.Add(right.Filter);
                return new Op(filter);
            }

            public static Op operator |(Op left, Op right)
            {
                var filter = new OpOr();
                filter.Add(left.Filter);
                filter.Add(right.Filter);
                return new Op(filter);
            }

            public static Op operator ^(Op left, Op right)
            {
                var filter = new OpXor(left.Filter, right.Filter);
                return new Op(filter);
            }

            public override bool Equals(object obj) => base.Equals(obj);

            public override int GetHashCode() => base.GetHashCode();

        }

        public static OpFilter Bulid(Func<Op, Op> func)
        {
            return func(new OpFilter.Op()).Filter;
        }

        #endregion


    }
}
