using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFox.Cad.Collections
{
    public abstract class OpList : OpLogi
    {

        protected List<OpFilter> _lst
            = new List<OpFilter>();

        public virtual void Add(OpFilter value)
        {
            _lst.Add(value);
        }

        public void Add(int code, object value)
        {
            _lst.Add(new OpEqual(code, value));
        }

        public void Add(DxfCode code, object value)
        {
            _lst.Add(new OpEqual(code, value));
        }

        public void Add(int code, object value, string comp)
        {
            _lst.Add(new OpComp(comp, code, value));
        }

        public void Add(DxfCode code, object value, string comp)
        {
            _lst.Add(new OpComp(comp, code, value));
        }

        public override IEnumerator<OpFilter> GetEnumerator()
        {
            foreach (var value in _lst)
                yield return value;
        }
    }

    public class OpAnd : OpList
    {

        public override string Name
        {
            get { return "And"; }
        }

        public override void Add(OpFilter value)
        {
            if (value is OpAnd)
            {
                foreach (var item in (OpAnd)value)
                    _lst.Add(item);
            }
            else
            {
                _lst.Add(value);
            }
        }

    }

    public class OpOr : OpList
    {

        public override string Name
        {
            get { return "Or"; }
        }

        public override void Add(OpFilter value)
        {
            if (value is OpOr)
            {
                foreach (var item in (OpOr)value)
                    _lst.Add(item);
            }
            else
            {
                _lst.Add(value);
            }
        }

    }

}
