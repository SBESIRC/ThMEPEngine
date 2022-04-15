using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThControlLibraryWPF.CustomProperty;

namespace ThControlLibraryWPF.CustomControl
{
    /// <summary>
    /// 按照步骤 1a 或 1b 操作，然后执行步骤 2 以在 XAML 文件中使用此自定义控件。
    ///
    /// 步骤 1a) 在当前项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
    /// 元素中:
    ///
    ///     xmlns:MyNamespace="clr-namespace:ThControlLibraryWPF"
    ///
    ///
    /// 步骤 1b) 在其他项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
    /// 元素中:
    ///
    ///     xmlns:MyNamespace="clr-namespace:ThControlLibraryWPF;assembly=ThControlLibraryWPF"
    ///
    /// 您还需要添加一个从 XAML 文件所在的项目到此项目的项目引用，
    /// 并重新生成以避免编译错误:
    ///
    ///     在解决方案资源管理器中右击目标项目，然后依次单击
    ///     “添加引用”->“项目”->[浏览查找并选择此项目]
    ///
    ///
    /// 步骤 2)
    /// 继续操作并在 XAML 文件中使用控件。
    ///
    ///     <MyNamespace:NumberTextBox/>
    ///
    /// </summary>
    public class InputTextBox : TextBoxBaseProperty
    {
        private TextBox _inputText;
        List<Key> baseKeys = new List<Key>()
        {
            Key.Back,
            Key.Enter,
            Key.Home,
            Key.End,
            Key.NumLock,
            Key.Left,
            Key.Right,
            Key.Insert,
            Key.Delete,
            Key.Escape
        };
        static InputTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InputTextBox), new FrameworkPropertyMetadata(typeof(InputTextBox)));
        }

        /// <summary>
        /// 声明路由事件
        /// 参数:要注册的路由事件名称，路由事件的路由策略，事件处理程序的委托类型(可自定义)，路由事件的所有者类类型
        /// </summary>
        public static readonly RoutedEvent TextBoxEnterEvent = EventManager.RegisterRoutedEvent("EnterEvent", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventArgs<Object>), typeof(InputTextBox));
        /// <summary>
        /// 处理各种路由事件的方法 
        /// </summary>
        public event RoutedEventHandler EnterEvent
        {
            //将路由事件添加路由事件处理程序
            add { AddHandler(TextBoxEnterEvent, value); }
            //从路由事件处理程序中移除路由事件
            remove { RemoveHandler(TextBoxEnterEvent, value); }
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_inputText != null)
            {
                _inputText.KeyUp -= _KeyUpEvent;
                _inputText.TextChanged -= _TextBoxTextChanged;
                _inputText.PreviewKeyDown -= _TextBoxPreviewKeyDown;
            }
            _inputText = GetTemplateChild("_tbInput") as TextBox;
            if (_inputText != null)
            {
                //通过keydowm过滤掉输入非数字
                _inputText.PreviewKeyDown += _TextBoxPreviewKeyDown;
                //通过textchange删除粘贴过来的数据中有非数字字符的
                _inputText.TextChanged += _TextBoxTextChanged;

                //用来注册回车事件
                _inputText.KeyUp += _KeyUpEvent;
            }
        }
        private void _TextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_CheckIsBaseInput(e))
            {
                e.Handled = false;
                return;
            }
            if (!_CheckCtrlCVX(e))
            {
                e.Handled = true;
                return;
            }
            if (e.Key == Key.OemMinus) 
            {
                e.Handled = !this.NumCanMinus;
                return;
            }
            switch (this.TextBoxInputType)
            {
                case EnumTextInputType.InputString:
                    return;
                case EnumTextInputType.InputDouble:
                    e.Handled = _CheckDoubleInput(e);
                    break;
                case EnumTextInputType.InputInteger:
                    e.Handled = _CheckIntInput(e);
                    break;
            }
        }
        private bool _CheckIsBaseInput(KeyEventArgs e)
        {
            
            if (baseKeys.Any(c=>c == e.Key))
                return true;
            return false;
        }
        private bool _CheckCtrlCVX(KeyEventArgs e)
        {
            if (_IsCtrlC(e))
                return this.CanCopy;
            if (_IsCtrlV(e))
                return this.CanPaste;
            if (_IsCtrlX(e))
                return this.CanCut;
            return true;
        }
        private bool _CheckDoubleInput(KeyEventArgs e)
        {
            if (_IsNumber(e) || e.Key == Key.Decimal || e.Key == Key.OemPeriod || e.Key == Key.OemMinus)
            {
                if (e.KeyboardDevice.Modifiers != ModifierKeys.None)
                    return true;
            }
            return false;
        }
        private bool _CheckIntInput(KeyEventArgs e)
        {
            if (_IsNumber(e) || e.Key == Key.OemMinus)
            {
                if (e.KeyboardDevice.Modifiers != ModifierKeys.None)
                    return true;
            }
            return false;
        }

        private bool _IsCtrlC(KeyEventArgs e)
        {
            return e.Key == Key.C && e.KeyboardDevice.Modifiers == ModifierKeys.Control;
        }
        private bool _IsCtrlV(KeyEventArgs e)
        {
            return e.Key == Key.V && e.KeyboardDevice.Modifiers == ModifierKeys.Control;
        }
        private bool _IsCtrlX(KeyEventArgs e)
        {
            return e.Key == Key.X && e.KeyboardDevice.Modifiers == ModifierKeys.Control;
        }
        private bool _IsNumber(KeyEventArgs e)
        {
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9 && e.KeyboardDevice.Modifiers != ModifierKeys.Shift) ||
              (e.Key >= Key.D0 && e.Key <= Key.D9 && e.KeyboardDevice.Modifiers != ModifierKeys.Shift))
            {
                return true;
            }
            return false; ;
        }

        private void _TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            var txtStr = textBox.Text.ToString();
            if (string.IsNullOrEmpty(txtStr))
                return;

            var index = textBox.CaretIndex;
            string newStr = "";
            switch (this.TextBoxInputType)
            {
                case EnumTextInputType.InputString:
                    return;
                case EnumTextInputType.InputDouble:
                    int addDpointInt = -1;
                    if (e.Changes.Count == 1)
                    {
                        //单个输入时可能时修改小数点位置，这里需要单独判断
                        TextChange[] change = new TextChange[e.Changes.Count];
                        e.Changes.CopyTo(change, 0);
                        if (change[0].AddedLength > 0 && change[0].Offset < txtStr.Length)
                        {
                            var addCh = txtStr[change[0].Offset];
                            if (addCh.ToString().Equals("."))
                                addDpointInt = change[0].Offset;
                        }
                    }

                    newStr = _DoubleNumTextChange(txtStr, addDpointInt, out int firstDPointIndex);
                    if (addDpointInt > -1 && firstDPointIndex > -1 && firstDPointIndex < addDpointInt)
                        index -= 1;
                    break;
                case EnumTextInputType.InputInteger:
                    newStr = _IntNumTextChange(txtStr);
                    break;
            }
            textBox.Text = newStr;
            textBox.CaretIndex = newStr.Length >= index ? index : newStr.Length;
        }
        string _IntNumTextChange(string txtStr)
        {
            //屏蔽中文输入和非法字符粘贴输入
            var chars = txtStr.ToCharArray();
            //这里允许复制粘贴，就要先将非法字符移除
            //允许的字符有 -(45) 0(48)---9(57) 
            //-有的话必须在第一位
            var newStr = "";
            for (int i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (c == 46 || (i != 0 && c == 45))
                    continue;
                bool isAdd = false;
                if (i == 0)
                {
                    if (c == 45 || c == 46 || (c >= 48 && c <= 57))
                    {
                        isAdd = true;
                    }
                }
                else if (c == 46 || (c >= 48 && c <= 57))
                {
                    isAdd = true;
                }
                if (!isAdd)
                    continue;
                newStr += c.ToString();
            }
            return newStr;
        }
        string _DoubleNumTextChange(string txtStr, int addDpointInt, out int firstDPointIndex)
        {
            //屏蔽中文输入和非法字符粘贴输入
            firstDPointIndex = -1;
            var chars = txtStr.ToCharArray();
            //这里允许复制粘贴，就要先将非法字符移除
            //允许的字符有 -(45) 0(48)---9(57) .
            //-有的话必须在第一位
            //.有不能在 -后，且只能有一个
            var newStr = "";
            bool haveDPoint = false;
            for (int i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if ((haveDPoint && c == 46) || (i != 0 && c == 45))
                    continue;
                if (!haveDPoint)
                    haveDPoint = c == 46;
                if (haveDPoint && firstDPointIndex < 0)
                    firstDPointIndex = i;
                if (addDpointInt > -1 && haveDPoint && i < addDpointInt)
                {
                    haveDPoint = false;
                    firstDPointIndex = -1;
                    continue;
                }
                bool isAdd = false;
                if (i == 0)
                {
                    if (c == 45 || c == 46 || (c >= 48 && c <= 57))
                    {
                        isAdd = true;
                    }
                }
                else if (c == 46 || (c >= 48 && c <= 57))
                {
                    isAdd = true;
                }
                if (isAdd && firstDPointIndex > -1 && MaxDecimalPlaces > 0)
                {
                    int decimalCount = i - firstDPointIndex;
                    isAdd = decimalCount <= MaxDecimalPlaces;
                }
                if (!isAdd)
                    continue;
                newStr += c.ToString();
            }
            return newStr;
        }
        private void _KeyUpEvent(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            this.RaiseEvent(new RoutedEventArgs(TextBoxEnterEvent, this));
        }
    }
}
