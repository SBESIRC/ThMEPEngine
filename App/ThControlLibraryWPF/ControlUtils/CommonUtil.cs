using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
