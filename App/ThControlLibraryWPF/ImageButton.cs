using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ThControlLibraryWPF
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
    ///     <MyNamespace:ImageButton/>
    ///
    /// </summary>
    public class ImageButton : Button
    {

        #region 依赖属性 颜色属性
        /// <summary>
        /// 当鼠标移到按钮上时，按钮的前景色（这是依赖属性）
        /// </summary>
        public static readonly DependencyProperty MouseOverForegroundProperty =
            DependencyProperty.Register("MouseOverForeground", typeof(Brush), typeof(ImageButton), new PropertyMetadata(Brushes.Black));
        /// <summary>
        /// 当鼠标移到按钮上时，按钮的前景色
        /// </summary>

        public Brush MouseOverForeground
        {
            get { return (Brush)GetValue(MouseOverForegroundProperty); }
            set { SetValue(MouseOverForegroundProperty, value); }
        }
        /// <summary>
        /// 鼠标移到按钮上时，按钮的背景色（这是依赖属性）
        /// </summary>
        public static readonly DependencyProperty MouseOverBackgroundProperty =
            DependencyProperty.Register("MouseOverBackground", typeof(Brush), typeof(ImageButton), new PropertyMetadata(null));
        /// <summary>
        /// 鼠标移到按钮上时，按钮的背景色
        /// </summary>
        public Brush MouseOverBackground
        {
            get
            {
                return (Brush)GetValue(MouseOverBackgroundProperty);
            }
            set
            {
                SetValue(MouseOverBackgroundProperty, value);
            }
        }


        /// <summary>
        /// 当鼠标按下时，按钮的前景色（这是依赖属性）
        /// </summary>
        public static readonly DependencyProperty MousedownForegroundProperty =
            DependencyProperty.Register("MousedownForeground", typeof(Brush), typeof(ImageButton), new PropertyMetadata(Brushes.Black));
        /// <summary>
        /// 当鼠标按下时，按钮的前景色
        /// </summary>
        public Brush MousedownForeground
        {
            get { return (Brush)GetValue(MousedownForegroundProperty); }
            set { SetValue(MousedownForegroundProperty, value); }
        }


        /// <summary>
        /// 当鼠标按下时，按钮的背景色（这是依赖属性）
        /// </summary>
        public static readonly DependencyProperty MousedownBackgroundProperty =
            DependencyProperty.Register("MousedownBackground", typeof(Brush), typeof(ImageButton), new PropertyMetadata(null));
        /// <summary>
        /// 当鼠标按下时，按钮的背景色
        /// </summary>
        public Brush MousedownBackground
        {
            get { return (Brush)GetValue(MousedownBackgroundProperty); }
            set { SetValue(MousedownBackgroundProperty, value); }
        }


        /// <summary>
        /// 当按钮不可用时，按钮的前景色（这是依赖属性）
        /// </summary>
        public static readonly DependencyProperty DisabledForegroundProperty =
            DependencyProperty.Register(" DisabledForeground", typeof(Brush), typeof(ImageButton), new PropertyMetadata(Brushes.Black));
        /// <summary>
        /// 当按钮不可用时，按钮的前景色
        /// </summary>
        public Brush DisabledForeground
        {
            get { return (Brush)GetValue(DisabledForegroundProperty); }
            set { SetValue(DisabledForegroundProperty, value); }
        }


        /// <summary>
        /// 当按钮不可用时，按钮的背景色（这是依赖属性）
        /// </summary>
        public static readonly DependencyProperty DisabledBackgroundProperty =
            DependencyProperty.Register("DisabledBackground", typeof(Brush), typeof(ImageButton), new PropertyMetadata(Brushes.Gray));
        /// <summary>
        /// 当按钮不可用时，按钮的背景色
        /// </summary>
        public Brush DisabledBackground
        {
            get { return (Brush)GetValue(DisabledBackgroundProperty); }
            set { SetValue(DisabledBackgroundProperty, value); }
        }
        #endregion 依赖属性 颜色属性

        /// <summary>
        /// 图标动画
        /// </summary>
        public static readonly DependencyProperty AllowsAnimationProperty = DependencyProperty.Register(
            "AllowsAnimation", typeof(bool), typeof(ImageButton), new PropertyMetadata(false));
        /// <summary>
        /// 是否启用Ficon动画
        /// </summary>
        public bool AllowsAnimation
        {
            get { return (bool)GetValue(AllowsAnimationProperty); }
            set { SetValue(AllowsAnimationProperty, value); }
        }

        /// <summary>
        /// 图标是否填充
        /// </summary>
        public static readonly DependencyProperty ImageStretchProperty = DependencyProperty.Register(
            "ImageStretch", typeof(Stretch), typeof(ImageButton), new PropertyMetadata(Stretch.Fill));
        public Stretch ImageStretch
        {
            get { return (Stretch)GetValue(ImageStretchProperty); }
            set { SetValue(ImageStretchProperty, value); }
        }
        /// <summary>
        /// 图标填充拉伸方向
        /// </summary>
        public static readonly DependencyProperty ImageStretchDirectionProperty = DependencyProperty.Register(
            "ImageStretchDirection", typeof(StretchDirection), typeof(ImageButton), new PropertyMetadata(StretchDirection.Both));
        public StretchDirection ImageStretchDirection
        {
            get { return (StretchDirection)GetValue(ImageStretchDirectionProperty); }
            set { SetValue(ImageStretchDirectionProperty, value); }
        }
        /// <summary>
        /// 文字方向
        /// </summary>
        public static readonly DependencyProperty TextOrientationProperty = DependencyProperty.Register
            ("TextOrientation", typeof(Orientation), typeof(ImageButton), new PropertyMetadata(Orientation.Horizontal));
        public Orientation TextOrientation
        {
            get { return (Orientation)GetValue(TextOrientationProperty); }
            set { SetValue(TextOrientationProperty, value); }
        }
        /// <summary>
        /// 图标Margin
        /// </summary>
        public static readonly DependencyProperty ImageMarginProperty = DependencyProperty.Register(
            "ImageMargin", typeof(Thickness), typeof(ImageButton), new PropertyMetadata(null));
        public Thickness ImageMargin
        {
            get { return (Thickness)GetValue(ImageMarginProperty); }
            set { SetValue(ImageMarginProperty, value); }
        }
        /// <summary>
        /// 文字Margin
        /// </summary>
        public static readonly DependencyProperty TextMarginProperty = DependencyProperty.Register(
            "TextMargin", typeof(Thickness), typeof(ImageButton), new PropertyMetadata(null));
        public Thickness TextMargin
        {
            get { return (Thickness)GetValue(TextMarginProperty); }
            set { SetValue(TextMarginProperty, value); }
        }
        /// <summary>
        /// 按钮的圆角大小
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(ImageButton), new PropertyMetadata(null));
        /// <summary>
        /// 按钮圆角大小,左上，右上，右下，左下
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }


        #region 依赖属性 图标属性
        /// <summary>
        /// 按钮正常时的图片
        /// </summary>
        public static readonly DependencyProperty NormalImageProperty =
                        DependencyProperty.Register("NormalImage", typeof(ImageSource), typeof(ImageButton), new PropertyMetadata(null));
        public ImageSource NormalImage
        {
            get { return (ImageSource)GetValue(NormalImageProperty); }
            set { SetValue(NormalImageProperty, value); }
        }
        /// <summary>
        /// 按钮在鼠标移入时的图片
        /// </summary>
        public static readonly DependencyProperty HoverImageProperty =
                            DependencyProperty.Register("HoverImage", typeof(ImageSource), typeof(ImageButton), new PropertyMetadata(null));
        public ImageSource HoverImage
        {
            get { return (ImageSource)GetValue(HoverImageProperty); }
            set { SetValue(HoverImageProperty, value); }
        }
        /// <summary>
        /// 按钮在鼠标按下时的图片
        /// </summary>
        public static readonly DependencyProperty PressedImageProperty =
                            DependencyProperty.Register("PressedImage", typeof(ImageSource), typeof(ImageButton), new PropertyMetadata(null));
        public ImageSource PressedImage
        {
            get { return (ImageSource)GetValue(PressedImageProperty); }
            set { SetValue(PressedImageProperty, value); }
        }
        /// <summary>
        /// 按钮在不可用时的图片
        /// </summary>
        public static readonly DependencyProperty DisabledImageProperty =
                            DependencyProperty.Register("DisabledImage", typeof(ImageSource), typeof(ImageButton), new PropertyMetadata(null));
        public ImageSource DisabledImage
        {
            get { return (ImageSource)GetValue(DisabledImageProperty); }
            set { SetValue(DisabledImageProperty, value); }
        }
        /// <summary>
        /// 按钮中图片宽度
        /// </summary>
        public static readonly DependencyProperty ImageWidthProperty = DependencyProperty.Register("ImageWidth", typeof(double), typeof(ImageButton), new PropertyMetadata(null));
        public double ImageWidth
        {
            get { return (double)GetValue(ImageWidthProperty); }
            set { SetValue(ImageWidthProperty, value); }
        }
        /// <summary>
        /// 按钮中图片高度
        /// </summary>
        public static readonly DependencyProperty ImageHeightProperty = DependencyProperty.Register("ImageHeight", typeof(double), typeof(ImageButton), new PropertyMetadata(null));
        public double ImageHeight
        {
            get { return (double)GetValue(ImageHeightProperty); }
            set { SetValue(ImageHeightProperty, value); }
        }
        /// <summary>
        /// 按钮中图片和文字的布局方式
        /// </summary>
        public static readonly DependencyProperty ImageTextLocationProperty = DependencyProperty.Register("ImageTextLocation", typeof(BtnImageText), typeof(ImageButton), new PropertyMetadata(BtnImageText.ImageInTextLeft));
        public BtnImageText ImageTextLocation
        {
            get { return (BtnImageText)GetValue(ImageTextLocationProperty); }
            set { SetValue(ImageTextLocationProperty, value); }
        }
        #endregion


        static ImageButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageButton), new FrameworkPropertyMetadata(typeof(ImageButton)));
        }
    }
    public class ImageTextLocationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var location = (BtnImageText)value;
            string par = parameter.ToString().ToLower();
            int ret = 0;
            switch (location)
            {
                case BtnImageText.ImageBackGround:
                    ret = 0;
                    break;
                case BtnImageText.ImageInTextLeft:
                    if (par.Contains("row"))
                        ret = 0;
                    else if (par.Contains("image"))
                        ret = 0;
                    else
                        ret = 1;
                    break;
                case BtnImageText.ImageInTextRight:
                    if (par.Contains("row"))
                        ret = 0;
                    else if (par.Contains("image"))
                        ret = 1;
                    else
                        ret = 0;
                    break;
                case BtnImageText.ImageInTextUp:
                    if (par.Contains("column"))
                        ret = 0;
                    else if (par.Contains("image"))
                        ret = 0;
                    else
                        ret = 1;
                    break;
                case BtnImageText.ImageInTextBottom:
                    if (par.Contains("column"))
                        ret = 0;
                    else if (par.Contains("image"))
                        ret = 1;
                    else
                        ret = 0;
                    break;
            }
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class ImageTextVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var location = (BtnImageText)value;
            string par = parameter.ToString().ToLower();
            Visibility ret = Visibility.Collapsed;
            switch (location)
            {
                case BtnImageText.ImageBackGround:
                case BtnImageText.ImageInTextLeft:
                case BtnImageText.ImageInTextRight:
                case BtnImageText.ImageInTextUp:
                case BtnImageText.ImageInTextBottom:
                    ret = Visibility.Visible;
                    break;
                case BtnImageText.TextOnly:
                    if (par.Contains("text"))
                        ret = Visibility.Visible;
                    break;
                case BtnImageText.ImageOnly:
                    if (par.Contains("image"))
                        ret = Visibility.Visible;
                    break;
            }
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class TextShowMultiConverter : IMultiValueConverter
    {
        /// <summary>
        /// values[text,Orientation]
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string res = "";
            try
            {
                res = values[0].ToString();
                Orientation textOrientation = (Orientation)values[1];
                if (textOrientation == Orientation.Horizontal)
                    return res;
                byte[] byteArray = System.Text.Encoding.Default.GetBytes(res);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < res.Length; i++)
                {
                    if (i == res.Length - 1)
                    {
                        sb.Append(res[i]);
                    }
                    else
                    {
                        sb.Append(res[i]);
                        sb.Append(Environment.NewLine);
                    }
                }
                res = sb.ToString();
            }
            catch (Exception ex) { }

            return res;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }


    public enum BtnImageText
    {
        ImageInTextLeft,
        ImageInTextRight,
        ImageInTextUp,
        ImageInTextBottom,
        ImageOnly,
        TextOnly,
        ImageBackGround
    }
}
