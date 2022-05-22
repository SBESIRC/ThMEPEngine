using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ThControlLibraryWPF.ControlUtils
{
    public class CommonUtil
    {
        /// <summary>
        /// 模仿C#的Application.Doevent函数。可以适当添加try catch 模块
        /// </summary>
        public static void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }
        public static object ExitFrame(object f)
        {
            ((DispatcherFrame)f).Continue = false;
            return null;
        }
        public static void SetComboxDefault(ComboBox comboBox, string name)
        {
            if (null == comboBox)
                return;
            if (null == comboBox.Items || comboBox.Items.Count < 1)
                return;
            foreach (var item in comboBox.Items)
            {
                var itemData = item as UListItemData;
                if (null == itemData)
                    continue;
                if (itemData.Name.Equals(name))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
            comboBox.SelectedIndex = 0;
        }
        public static void SetComboxDefault(ComboBox comboBox, int value)
        {
            if (null == comboBox)
                return;
            if (null == comboBox.Items || comboBox.Items.Count < 1)
                return;
            foreach (var item in comboBox.Items)
            {
                var itemData = item as UListItemData;
                if (null == itemData)
                    continue;
                if (itemData.Value == value)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
            comboBox.SelectedIndex = 0;
        }

        #region 枚举的相关信息
        public static List<UListCheckItem> EnumDescriptionToList<T>(int minValue,int maxValue) where T : Enum
        {
            var allItmes = EnumDescriptionToList<T>();
            var retUList = new List<UListCheckItem>();
            foreach (var item in allItmes)
            {
                if (item.Value == -1)
                {
                    retUList.Add(item);
                }
                else if (item.Value >= minValue && item.Value <= maxValue)
                {
                    retUList.Add(item);
                }
            }
            return retUList;
        }
        public static List<UListCheckItem> EnumDescriptionToList<T>() where T : Enum
        {
            var enumType = typeof(T);
            var retUList = new List<UListCheckItem>();
            string[] allEnums = null;
            try
            {
                allEnums = Enum.GetNames(enumType);
            }
            catch (Exception ex) { throw ex; }
            if (null == allEnums || allEnums.Length < 1)
                return retUList;
            foreach (var item in allEnums)
            {
                var enumItem = enumType.GetField(item);
                int value = (int)enumItem.GetValue(item);
                object[] objs = enumItem.GetCustomAttributes(typeof(DescriptionAttribute), false);
                string des = "";
                if (objs.Length == 0)
                    des = item;
                else
                    des = ((DescriptionAttribute)objs[0]).Description;
                retUList.Add(new UListCheckItem(des, value, enumItem));
            }
            return retUList;
        }
        public static List<UListItemData> EnumDescriptionToList(Type enumType,int minValue,int maxValue, string noSelectName = "") 
        {
            var uList = EnumDescriptionToList(enumType, noSelectName);
            var retUList = new List<UListItemData>();
            foreach (var item in uList) 
            {
                if (item.Value == -1)
                {
                    retUList.Add(item);
                }
                else if (item.Value >= minValue && item.Value <= maxValue) 
                {
                    retUList.Add(item);
                }
            }
            return retUList;
        }
        public static List<UListItemData> EnumDescriptionToList(Type enumType, List<int> values, string noSelectName = "")
        {
            var uList = EnumDescriptionToList(enumType, noSelectName);
            var retUList = new List<UListItemData>();
            foreach (var item in uList) 
            {
                if (item.Value == -1)
                {
                    retUList.Add(item);
                }
                else if(values.Any(c=>c == item.Value))
                {
                    retUList.Add(item);
                }
            }
            return retUList;
        }
        public static List<UListItemData> EnumDescriptionToList(Type enumType, string noSelectName = "")
        {
            if (enumType.BaseType != typeof(Enum))
                throw new Exception("不支持非枚举类型");
            var itemDatas = new List<UListItemData>();
            string[] allEnums = null;
            try
            {
                allEnums = Enum.GetNames(enumType);
            }
            catch (Exception ex) { throw ex; }
            if (!string.IsNullOrEmpty(noSelectName))
            {
                itemDatas.Add(new UListItemData(noSelectName, -1));
            }
            if (null == allEnums || allEnums.Length < 1)
                return itemDatas;
            foreach (var item in allEnums)
            {
                var enumItem = enumType.GetField(item);
                int value = (int)enumItem.GetValue(item);
                object[] objs = enumItem.GetCustomAttributes(typeof(DescriptionAttribute), false);
                string des = "";
                if (objs.Length == 0)
                    des = item;
                else
                    des = ((DescriptionAttribute)objs[0]).Description;
                itemDatas.Add(new UListItemData(des, value, enumItem));
            }
            return itemDatas;
        }
        public static T GetEnumItemByDescription<T>(string desName) where T:Enum
        {
            Type enumType = typeof(T);
            string[] allEnums = null;
            try
            {
                allEnums = Enum.GetNames(enumType);
            }
            catch (Exception ex) 
            {
                throw ex; 
            }
            if (null == allEnums || allEnums.Length < 1)
                return default(T);
            foreach (var item in allEnums)
            {
                var enumItem = enumType.GetField(item);
                object[] objs = enumItem.GetCustomAttributes(typeof(DescriptionAttribute), false);
                string des = "";
                if (objs.Length == 0)
                    des = item;
                else
                    des = ((DescriptionAttribute)objs[0]).Description;
                if (string.IsNullOrEmpty(des))
                    continue;
                if (desName == des)
                    return (T)enumItem.GetValue(item);
            }
            return default(T);
        }
        /// <summary>
        /// 获取枚举值的描述信息DescriptionAttribute中内容
        /// [DescriptionAttribute("")]
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string GetEnumDescription(Enum enumValue)
        {
            string value = enumValue.ToString();
            FieldInfo field = enumValue.GetType().GetField(value);
            if (field == null)
                return "";
            object[] objs = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (objs.Length == 0)
                return value;
            DescriptionAttribute description = (DescriptionAttribute)objs[0];
            return description.Description;
        }
        #endregion
    }
}
