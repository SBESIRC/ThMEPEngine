using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ThControlLibraryWPF.CustomProperty
{
    public class TextBoxBaseProperty : TextBox
    {
        /// <summary>
        /// 当鼠标移到控件上时，前景色（这是依赖属性）
        /// </summary>
        public static readonly DependencyProperty FocusBorderColorProperty = DependencyProperty.Register("FocusBorderColor", typeof(Brush), typeof(TextBoxBaseProperty), new PropertyMetadata(null));
        /// <summary>
        /// 当鼠标移到控件上时，前景色
        /// </summary>
        public Brush FocusBorderColor
        {
            get { return (Brush)GetValue(FocusBorderColorProperty); }
            set { SetValue(FocusBorderColorProperty, value); }
        }
        /// <summary>
        /// 提醒
        /// </summary>
        public static readonly DependencyProperty HintTextProperty = DependencyProperty.Register("HintText", typeof(string), typeof(TextBoxBaseProperty), new PropertyMetadata("请输入..."));
        public string HintText
        {
            get { return (string)GetValue(HintTextProperty); }
            set { SetValue(HintTextProperty, value); }
        }
        /// <summary>
        /// 输入框圆角大小
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(TextBoxBaseProperty), new PropertyMetadata(new CornerRadius(2)));
        /// <summary>
        /// 输入框圆角大小,左上，右上，右下，左下
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }
        /// <summary>
        /// 文字Margin
        /// </summary>
        public static readonly DependencyProperty TextMarginProperty = DependencyProperty.Register("TextMargin", typeof(Thickness), typeof(TextBoxBaseProperty), new PropertyMetadata(new Thickness(0, 0, 0, 0)));
        public Thickness TextMargin
        {
            get { return (Thickness)GetValue(TextMarginProperty); }
            set { SetValue(TextMarginProperty, value); }
        }
        /// <summary>
        /// 是否可以复制
        /// </summary>
        public static readonly DependencyProperty CanCopyProperty = DependencyProperty.Register("CanCopy", typeof(bool), typeof(TextBoxBaseProperty), new PropertyMetadata(true));
        /// <summary>
        /// 是否可以复制
        /// </summary>
        public bool CanCopy
        {
            get { return (bool)GetValue(CanCopyProperty); }
            set { SetValue(CanCopyProperty, value); }
        }
        /// <summary>
        /// 是否可以粘贴
        /// </summary>
        public static readonly DependencyProperty CanPasteProperty = DependencyProperty.Register("CanPaste", typeof(bool), typeof(TextBoxBaseProperty), new PropertyMetadata(true));
        /// <summary>
        /// 是否可以粘贴
        /// </summary>
        public bool CanPaste
        {
            get { return (bool)GetValue(CanPasteProperty); }
            set { SetValue(CanPasteProperty, value); }
        }
        /// <summary>
        /// 是否可以剪切
        /// </summary>
        public static readonly DependencyProperty CanCutProperty = DependencyProperty.Register("CanCut", typeof(bool), typeof(TextBoxBaseProperty), new PropertyMetadata(true));
        /// <summary>
        /// 是否可以剪切
        /// </summary>
        public bool CanCut
        {
            get { return (bool)GetValue(CanCutProperty); }
            set { SetValue(CanCutProperty, value); }
        }
        /// <summary>
        /// 数字输入时是否可以输入“-”
        /// </summary>
        public static readonly DependencyProperty NumCanMinusProperty = DependencyProperty.Register("NumCanMinus", typeof(bool), typeof(TextBoxBaseProperty), new PropertyMetadata(true));
        /// <summary>
        /// 数字输入时是否可以输入“-”
        /// </summary>
        public bool NumCanMinus
        {
            get { return (bool)GetValue(NumCanMinusProperty); }
            set { SetValue(NumCanMinusProperty, value); }
        }
        public static readonly DependencyProperty TextBoxInputTypeProperty = DependencyProperty.Register("TextBoxInputType", typeof(EnumTextInputType), typeof(TextBoxBaseProperty), new PropertyMetadata(EnumTextInputType.InputString));
        public EnumTextInputType TextBoxInputType
        {
            get { return (EnumTextInputType)GetValue(TextBoxInputTypeProperty); }
            set { SetValue(TextBoxInputTypeProperty, value); }
        }
        /// <summary>
        /// Double数字输入时最大输入小数点后几位（<=0时不做限制）
        /// </summary>
        public static readonly DependencyProperty MaxDecimalPlacesProperty = DependencyProperty.Register("MaxDecimalPlaces", typeof(int), typeof(TextBoxBaseProperty), new PropertyMetadata(-1));
        /// <summary>
        /// Double数字输入时最大输入小数点后几位（<=0时不做限制）
        /// </summary>
        public int MaxDecimalPlaces
        {
            get { return (int)GetValue(MaxDecimalPlacesProperty); }
            set { SetValue(MaxDecimalPlacesProperty, value); }
        }
    }
    public enum EnumTextInputType
    {
        InputString,
        InputDouble,
        InputInteger,
    }
}
