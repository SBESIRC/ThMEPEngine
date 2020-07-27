using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;

namespace NFox.Cad.Collections
{

    public class LispData
    {

        public readonly static LispData T =
            new LispData(new TypedValue((int)LispDataType.T_atom));

        public readonly static LispData Nil =
            new LispData(new TypedValue((int)LispDataType.Nil));

        public TypedValue Value { get; protected set; }

        public virtual bool IsList { get { return false; } }



        #region Constructor

        public LispData() { }

        public LispData(LispDataType code)
        {
            Value = new TypedValue((int)code);
        }

        public LispData(LispDataType code, object value)
        {
            Value = new TypedValue((int)code, value);
        }

        internal LispData(TypedValue value)
        {
            Value = value;
        }

        public LispData(short value)
            : this(LispDataType.Int16, value)
        { }

        public LispData(int value)
            : this(LispDataType.Int32, value)
        { }

        public LispData(double value)
            : this(LispDataType.Double, value)
        { }

        public LispData(Point2d value)
            : this(LispDataType.Point2d, value)
        { }

        public LispData(Point3d value)
            : this(LispDataType.Point3d, value)
        { }

        public LispData(ObjectId value)
            : this(LispDataType.ObjectId, value)
        { }

        public LispData(string value)
            : this(LispDataType.Text, value)
        { }

        public LispData(SelectionSet value)
            : this(LispDataType.SelectionSet, value)
        { }

        public static LispData Angle(double value)
        {
            return new LispData(LispDataType.Angle, value);
        }

        public static LispData Orientation(double value)
        {
            return new LispData(LispDataType.Orientation, value);
        }

        #endregion

        internal virtual void GetValues(ResultBuffer rb)
        {
            rb.Add(Value);
        }

        public virtual object GetValue()
        {
            return Value;
        }

        public virtual void SetValue(object value)
        {
            Value = new TypedValue(Value.TypeCode, value);
        }

        public virtual void SetValue(int code, object value)
        {
            Value = new TypedValue(code, value);
        }

    }
}
