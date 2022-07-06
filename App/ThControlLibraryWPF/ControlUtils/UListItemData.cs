using System;

namespace ThControlLibraryWPF.ControlUtils
{
    /// <summary>
    /// 选择项类，用于ComboBox或者ListBox添加项
    /// </summary>
    public class UListItemData
    {
        private string _name = string.Empty;
        private int _value = 0;
        private object _tag;
        private Type _type;
        public UListItemData() { }
        public UListItemData(string name, int value)
        {
            _value = value;
            _name = name;
        }
        public UListItemData(string name, int value, object tag)
        {
            _value = value;
            _name = name;
            _tag = tag;
        }
        public UListItemData(string name, int value, Type type)
        {
            _value = value;
            _name = name;
            _type = type;
        }
        public UListItemData(string name, int value, object tag, Type type)
        {
            _value = value;
            _name = name;
            _tag = tag;
            _type = type;
        }
        public override string ToString()
        {
            //#if DEBUG
            //            return string.Format("{0}_{1}", _name, _value);
            //#else            
            return _name;
            //#endif
        }

        public string Name { get { return this._name; } set { this._name = value; } }
        public int Value { get { return this._value; } set { this._value = value; } }
        public object Tag { get { return this._tag; } set { this._tag = value; } }
        public Type EnumType { get { return this._type; } set { this._type = value; } }
    }
}
