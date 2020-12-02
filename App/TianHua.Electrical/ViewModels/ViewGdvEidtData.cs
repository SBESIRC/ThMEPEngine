using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical
{
    [Serializable]
    [DataContract]
    public class ViewGdvEidtData
    {
        public string DisplayMember { get; set; }

        public string ValueMember { get; set; }
 
        public string Tag { get; set; }


         
        public virtual string Empty
        {
            set { value = Empty; } 
            get { return " "; }
        }

        /// <summary>
        /// 重写ToSring方法
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.ValueMember.Trim();
        }

        /// <summary>
        /// 主键
        /// </summary>
        public object 主键
        {
            get { return this.ValueMember; }
        }

        /// <summary>
        /// 重载==
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static bool operator ==(ViewGdvEidtData A, ViewGdvEidtData B)
        {
            if ((object)A == null && (object)B == null)
            {
                return true;
            }
            else if ((object)A == null || (object)B == null)
            {
                return false;
            }
            return A.ValueMember.Trim() == B.ValueMember.Trim();
        }

        /// <summary>
        /// 重载!=
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static bool operator !=(ViewGdvEidtData A, ViewGdvEidtData B)
        {
            if ((object)A == null && (object)B == null)
            {
                return false;
            }
            else if ((object)A == null || (object)B == null)
            {
                return true;
            }
            return A.ValueMember.Trim() != B.ValueMember.Trim();
        }

        /// <summary>
        /// 重写Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is ViewGdvEidtData)) { return false; }
            var tmp = obj as ViewGdvEidtData;
            return tmp == this;
        }


    }
}
