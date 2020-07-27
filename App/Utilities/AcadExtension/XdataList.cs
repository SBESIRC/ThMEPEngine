using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFox.Cad.Collections
{
    public class XdataList : List<TypedValue>
    {

        public XdataList() { }

        public XdataList(IEnumerable<TypedValue> values) 
            : base(values)
        { }

        public void Add(int code, object obj)
        {
            base.Add(new TypedValue(code, obj));
        }

        public void Add(LispDataType code, object obj)
        {
            base.Add(new TypedValue((int)code, obj));
        }

        public void Add(DxfCode code, object obj)
        {
            base.Add(new TypedValue((int)code, obj));
        }

        #region Convert

        public static XdataList FromBuffer(ResultBuffer rb)
        {
            var lst = new XdataList();
            lst.AddRange(rb.Cast<TypedValue>());
            return lst;
        }

        public ResultBuffer ToBuffer()
        {
            var rb = new ResultBuffer();
            foreach (var value in this)
                rb.Add(value);
            return rb;
        }

        public static implicit operator TypedValue[] (XdataList rlst)
        {
            return rlst.ToArray();
        }

        public static implicit operator ResultBuffer(XdataList rlst)
        {
            return rlst.ToBuffer();
        }

        public static implicit operator SelectionFilter(XdataList rlst)
        {
            return new SelectionFilter(rlst.ToArray());
        }

        public override string ToString()
        {
            return ToBuffer().ToString();
        }

        #endregion

    }
}

