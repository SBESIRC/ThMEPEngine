/*------------------------------------------------------------------
 * 创建：RiderKing/2007-1-20
 * 
 * 类名：InfoCore.Data.DbAccess 
 * 说明：用于处理名称－值集合的字符串，维护顺序关系
 * 如：Html元素的Style属性，如Color:red;Font-Size:15;
 * 如：Request的参数，如UserName=Niqin&UserAge=15
 * 
 * 审核：
 * 
 * 变更历史：
-------------------------------------------------------------------*/
using System;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.ComponentModel.Design.Serialization;

namespace BuildingModelData
{
    /// <summary>
    /// NameValueString转换器类
    /// </summary>
    public class NameValueStringConverter : TypeConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value != null && value is string)
            {
                return new BmdNameValueString(value as string);
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
                return true;
            return base.CanConvertTo(context, destinationType);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (value != null && value is BmdNameValueString)
            {
                BmdNameValueString nvs = (BmdNameValueString)value;
                if (destinationType == typeof(string))
                {
                    return (nvs == null) ? null : nvs.ToString();
                }

                if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo ctor = typeof(BmdNameValueString).GetConstructor(new Type[] { typeof(string) });
                    if (ctor != null)
                    {
                        return new InstanceDescriptor(ctor, new object[] { nvs.ToString() });
                    }
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            return true;
        }
    }

    /// <summary>
    /// 用于处理键/值对集合的字符串，维护顺序关系
    /// 如：Html元素的Style属性，如Color:red;Font-Size:15;
    /// 如：Request的参数，如UserName=Niqin&UserAge=15
    /// </summary>
    [TypeConverter(typeof(NameValueStringConverter))]
    public class BmdNameValueString : NameValueCollection
    {

        private string splitChar = ";";
        private string linkChar = ":";

        #region 属性
        /// <summary>
        /// 键值对之间的分隔字符
        /// </summary>
        public string SplitChar
        {
            get
            {
                return this.splitChar;
            }
            set
            {
                this.splitChar = value;
            }
        }

        /// <summary>
        /// 键与值之间的连接字符
        /// </summary>
        public string LinkChar
        {
            get
            {
                return this.linkChar;
            }
            set
            {
                this.linkChar = value;
            }
        }

        /// <summary>
        /// 构造键/值对字符串,使用默认SplitChar(;)/LinkChar(:)
        /// </summary>
        public BmdNameValueString()
        {
        }

        /// <summary>
        /// 使用已有String构造键/值对字符串,使用默认SplitChar(;)/LinkChar(:)并编码
        /// </summary>
        /// <param name="nameValueString">键/值对字符串</param>
        public BmdNameValueString(string nameValueString)
        {
            try
            {
                if (nameValueString != null)
                    Reset(nameValueString, true);
            }
            catch {
            }
        }

        /// <summary>
        /// 使用已有String构造键/值对字符串,使用默认SplitChar(;)/LinkChar(:)
        /// </summary>
        /// <param name="nameValueString">键/值对字符串</param>
        /// <param name="unescape">是否编码</param>
        public BmdNameValueString(string nameValueString, bool unescape)
        {
            Reset(nameValueString, unescape);
        }

        /// <summary>
        /// Web请求的参数的键/值对字符串，SplitChar(&)/LinkChar(=)
        /// </summary>
        /// <param name="nameValueString">键/值对字符串</param>
        /// <param name="queryString">是否为Web请求类型</param>
        /// <param name="unescape"></param>
        public BmdNameValueString(string nameValueString, bool queryString, bool unescape)
        {
            this.splitChar = "&";
            this.linkChar = "=";
            Reset(nameValueString, unescape);
        }

        /// <summary>
        /// 使用已有String构造键/值对字符串
        /// </summary>
        /// <param name="nameValueString">键/值对字符串</param>
        /// <param name="splitChar">键值对之间的分隔字符</param>
        /// <param name="linkChar">键与值之间的连接字符</param>
        /// <param name="unescape">是否使用unescape对值进行初始化</param>
        public BmdNameValueString(string nameValueString, string splitChar, string linkChar, bool unescape)
        {
            this.splitChar = splitChar;
            this.linkChar = linkChar;
            this.Reset(nameValueString, unescape);
        }

        #endregion

        #region 共有方法

        public bool ContainsName(string name)
        {
            for (int i = 0; i < Keys.Count; i++)
            {
                if (GetKey(i) == name)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 移除Name里的Value
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="value">subValue</param>
        public void Remove(string name, string value)
        {
            string[] values = GetValues(name);
            if (values.Length == 0) return;

            int idx = value.IndexOf(value);
            if (idx >= 0) value.Remove(idx, 1);
            Set(name, String.Join(",", values));
        }

        /// <summary>
        /// 重新设置键/值对集合
        /// </summary>
        /// <param name="nameValueString">键/值对字符串</param>
        /// <param name="unescape">是否编码</param>
        public void Reset(string nameValueString, bool unescape)
        {
            if (string.IsNullOrEmpty(nameValueString))
            {
                this.Clear();
                return;
            }
            string[] items = null;
            this.Clear();

            items = nameValueString.Split(this.splitChar.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Trim() != "")
                {
                    string[] ary = items[i].Trim().Split(this.linkChar.ToCharArray());
                    string key = unescape ? Regex.Unescape(ary[0]) : ary[0];
                    if (ary.Length > 1)
                    {
                        string val = unescape ? Regex.Unescape(ary[1]) : ary[1];
                        this.Add(key.Trim(), val.Trim());
                    }
                    else
                        this.Add(key.Trim(), "");
                }
            }
        }
        public new BmdNameValueString Add(string name, string value)
        {
            base.Add(name, value);
            return this;
        }

        public string[] GetKeys()
        {
            string[] keys = new string[this.Keys.Count];
            for (int i = 0; i < this.Keys.Count; i++)
                keys[i] = this.Keys[i];
            return keys;
        }

        public string[] GetValues()
        {
            string[] values = new string[this.Count];
            for (int i = 0; i < this.Keys.Count; i++)
                values[i] = this[i];
            return values;
        }

        /// <summary>
        /// 重写ToString方法
        /// </summary>
        /// <returns>键/值对字符串</returns>
        public override string ToString()
        {
            return this.ToString(true);
        }

        /// <summary>
        /// 重写ToString方法
        /// </summary>
        /// <param name="escape">是否编码</param>
        /// <returns>键/值对字符串</returns>
        public string ToString(bool escape)
        {
            if (this.Count == 0) return "";
            StringBuilder strRtn = new StringBuilder();
            for (int i = 0; i < this.Count; i++)
            {
                if (escape)
                    strRtn.Append(Regex.Escape(this.Keys[i]));
                else
                    strRtn.Append(this.Keys[i]);

                strRtn.Append(this.linkChar);
                if (escape)
                    strRtn.Append(Regex.Escape(this.Get(i)));
                else
                    strRtn.Append(this.Get(i));
                strRtn.Append(this.splitChar);
            }
            return strRtn.ToString().Substring(0, strRtn.Length - 1);
        }

        /// <summary>
        /// 重写GetHashCode方法
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// 重写Equals方法
        /// </summary>
        /// <param name="obj">比较对象</param>
        /// <returns>比较结果</returns>
        public override bool Equals(object obj)
        {
            if (obj != null && obj is BmdNameValueString)
            {
                if (obj.ToString() == this.ToString())
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        /// <summary>
        /// 克隆当前对象
        /// </summary>
        /// <returns>当前对象的深拷贝</returns>
        public BmdNameValueString Clone()
        {
            BmdNameValueString nvs = new BmdNameValueString();
            nvs.SplitChar = this.SplitChar;
            nvs.LinkChar = this.LinkChar;
            for (int i = 0; i < this.Count; i++)
            {
                nvs.Add(this.AllKeys[i], this.Get(i));
            }
            return nvs;
        }
        #endregion
        public Dictionary<string, string> ToDict()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            for (int i = 0; i < this.Count; i++)
            {
                dict.Add(this.AllKeys[i], this.Get(i));
            }
            return dict;
        }
    }
}
