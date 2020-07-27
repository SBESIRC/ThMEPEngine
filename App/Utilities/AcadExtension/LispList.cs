using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

namespace NFox.Cad.Collections
{

    public enum ListType
    {
        List,
        DottedPair
    }

    public class LispList : LispData, IEnumerable<LispData>
    {

        protected List<LispData> _lst = 
            new List<LispData>();

        protected LispList _parent;

        protected virtual TypedValue ListEnd
        {
            get { return new TypedValue((int)LispDataType.ListEnd);}
        }

        public override bool IsList
        {
            get { return true; }
        }

        public LispList()
            : base(LispDataType.ListBegin)
        { }

        public LispData this[int index]
        {
            get { return _lst[index]; }
            set { _lst[index] = value; }
        }

        #region AddRemove

        public int Count
        {
            get { return _lst.Count; }
        }

        public void Add(LispData value)
        {
            _lst.Add(value);
            if (value.IsList)
                ((LispList)value)._parent = this;
        }

        private void Add(TypedValue value)
        {
            Add(new LispData(value));
        }

        public void Add(short value)
        {
            Add(new LispData(value));
        }

        public void Add(int value)
        {
            Add(new LispData(value));
        }

        public void Add(double value)
        {
            Add(new LispData(value));
        }

        public void Add(Point2d value)
        {
            Add(new LispData(value));
        }

        public void Add(Point3d value)
        {
            Add(new LispData(value));
        }

        public void Add(ObjectId value)
        {
            Add(new LispData(value));
        }

        public void Add(string value)
        {
            Add(new LispData(value));
        }

        public void Add(SelectionSet value)
        {
            Add(new LispData(value));
        }

        public void RemoveAt(int index)
        {
            if (index > -1 && index < _lst.Count)
            {
                _lst.RemoveAt(index);
            }
        }

        public void Remove(LispData value)
        {
            _lst.Remove(value);
        }

        public bool Contains(LispData value)
        {
            return _lst.Contains(value);
        }

        public int IndexOf(LispData value)
        {
            return _lst.IndexOf(value);
        }

        #endregion

        #region Convert

        public IEnumerator<LispData> GetEnumerator()
        {
            return _lst.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal override void GetValues(ResultBuffer rb)
        {
            rb.Add(Value);
            _lst.ForEach(d => d.GetValues(rb));
            rb.Add(ListEnd);
        }

        public override object GetValue()
        {
            return ToBuffer();
        }

        public override void SetValue(object value)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(int code, object value)
        {
            throw new NotImplementedException();
        }

        public ResultBuffer ToBuffer()
        {
            ResultBuffer rb = new ResultBuffer();
            GetValues(rb);
            return rb;
        }

        public static LispList FromBuffer(ResultBuffer rb)
        {
            LispList lst = new LispList();
            if (rb != null)
            {
                LispList clst = lst;
                foreach (TypedValue value in rb)
                {
                    switch ((LispDataType)value.TypeCode)
                    {
                        case LispDataType.ListBegin:
                            var slst = new LispList();
                            clst.Add(slst);
                            clst = slst;
                            break;
                        case LispDataType.ListEnd:
                            clst = clst._parent;
                            break;
                        case LispDataType.DottedPair:
                            var plst = clst._parent;
                            plst[plst.IndexOf(clst)] =
                                new LispDottedPair
                                {
                                    _lst = clst._lst,
                                    _parent = clst._parent
                                };
                            clst = plst;
                            break;
                        default:
                            clst.Add(value);
                            break;
                    }
                }
            }
            return lst;
        }

        #endregion

    }


}
