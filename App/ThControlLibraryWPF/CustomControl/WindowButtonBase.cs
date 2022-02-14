using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ThControlLibraryWPF.CustomControl
{
    /// <summary>
    /// 窗体按钮（最大、最小、关闭）
    /// </summary>
    public class WindowButtonBase : Button
    {
        public static readonly DependencyProperty IconPathProperty = DependencyProperty.Register("IconPath", typeof(string), typeof(WindowButtonBase), new PropertyMetadata(string.Empty));
        public string IconPath
        {
            get
            {
                return (string)GetValue(IconPathProperty);
            }
            set { SetValue(IconPathProperty, value); }
        }
        /// <summary>
        /// 当鼠标移到按钮上时，按钮的前景色（这是依赖属性）
        /// </summary>
        public static readonly DependencyProperty MouseOverForegroundProperty = DependencyProperty.Register("MouseOverForeground", typeof(Brush), typeof(WindowButtonBase), new PropertyMetadata(Brushes.Black));
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
        public static readonly DependencyProperty MouseOverBackgroundProperty = DependencyProperty.Register("MouseOverBackground", typeof(Brush), typeof(WindowButtonBase), new PropertyMetadata(null));
        /// <summary>
        /// 鼠标移到按钮上时，按钮的背景色
        /// </summary>
        public Brush MouseOverBackground
        {
            get { return (Brush)GetValue(MouseOverBackgroundProperty); }
            set { SetValue(MouseOverBackgroundProperty, value); }
        }
        static WindowButtonBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowButtonBase), new FrameworkPropertyMetadata(typeof(WindowButtonBase)));
        }
    }

    #region WindowButtonMin
    public class WindowButtonMin : WindowButtonBase
    {
        protected override void OnClick()
        {
            Window win = Window.GetWindow(this);
            if (win.WindowState != WindowState.Minimized)
                win.WindowState = WindowState.Minimized;
            base.OnClick();
        }
    }
    #endregion

    #region WindowButtonMax
    public class WindowButtonMax : WindowButtonBase
    {
        protected override void OnClick()
        {
            Window win = Window.GetWindow(this);
            if (win.WindowState != WindowState.Maximized)
                win.WindowState = WindowState.Maximized;
            base.OnClick();

        }
    }
    #endregion

    #region WindowButtonNormal
    public class WindowButtonNormal : WindowButtonBase
    {
        protected override void OnClick()
        {
            Window win = Window.GetWindow(this);
            if (win.WindowState != WindowState.Normal)
                win.WindowState = WindowState.Normal;
            base.OnClick();
        }
    }
    #endregion

    #region WindowButtonClose
    public class WindowButtonClose : WindowButtonBase
    {
        protected override void OnClick()
        {
            Window win = Window.GetWindow(this);
            win.Close();
            base.OnClick();
        }
    }
    #endregion
}
