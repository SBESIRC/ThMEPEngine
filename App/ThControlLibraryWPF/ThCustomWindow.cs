using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WinInterop = System.Windows.Interop;

namespace ThControlLibraryWPF
{
    public class ThCustomWindow : Window, INotifyPropertyChanged
    {
        #region 自定义属性
        /// <summary>
        /// 窗体头部背景色
        /// </summary>
        public static readonly DependencyProperty TitleBackgroundProperty = DependencyProperty.Register("TitleBackground", typeof(Brush), typeof(ThCustomWindow), new PropertyMetadata(Brushes.Black));
        /// <summary>
        /// 窗体头部背景色
        /// </summary>
        public Brush TitleBackground
        {
            get { return (Brush)GetValue(TitleBackgroundProperty); }
            set { SetValue(TitleBackgroundProperty, value); }
        }
        /// <summary>
        /// 窗体头部字体颜色
        /// </summary>
        public static readonly DependencyProperty TitleForegroundProperty = DependencyProperty.Register("TitleForeground", typeof(Brush), typeof(ThCustomWindow), new PropertyMetadata(Brushes.White));
        /// <summary>
        /// 窗体头部字体颜色
        /// </summary>
        public Brush TitleForeground
        {
            get { return (Brush)GetValue(TitleForegroundProperty); }
            set { SetValue(TitleForegroundProperty, value); }
        }

        /// <summary>
        /// 窗体头部字体大小
        /// </summary>
        public static readonly DependencyProperty TitleFontSizeProperty = DependencyProperty.Register("TitleFontSize", typeof(double), typeof(ThCustomWindow), new PropertyMetadata(null));
        /// <summary>
        /// 窗体头部字体大小
        /// </summary>
        public double TitleFontSize 
        {
            get { return (double)GetValue(TitleFontSizeProperty); }
            set { SetValue(TitleFontSizeProperty, value); }
        }
        /// <summary>
        /// 窗体头部文字Weight
        /// </summary>
        public static readonly DependencyProperty TitleFontWeightProperty = DependencyProperty.Register("TitleFontWeight", typeof(FontWeight), typeof(ThCustomWindow), new PropertyMetadata(FontWeights.Black));
        /// <summary>
        /// 体头部文字Weight
        /// </summary>
        public FontWeight TitleFontWeight 
        {
            get { return (FontWeight)GetValue(TitleFontWeightProperty); }
            set { SetValue(TitleFontWeightProperty, value); }
        }
        #endregion

        #region 上方按钮的显隐属性

        private Visibility _btnMinimizeVisibility = Visibility.Visible;
        /// <summary>
        /// 窗体最小化按钮的显示状态
        /// </summary>
        public Visibility BtnMinimizeVisibility
        {
            get
            {
                return _btnMinimizeVisibility;
            }
            set
            {
                _btnMinimizeVisibility = value;
                OnPropertyChanged("BtnMinimizeVisibility");
            }
        }

        private Visibility _btnMaximizeVisibility = Visibility.Visible;
        /// <summary>
        /// 窗体最大化按钮的显示状态
        /// </summary>
        public Visibility BtnMaximizeVisibility
        {
            get
            {
                return _btnMaximizeVisibility;
            }
            set
            {
                _btnMaximizeVisibility = value;
                OnPropertyChanged("BtnMaximizeVisibility");
            }
        }

        private Geometry _btnMaximizePathData;
        /// <summary>
        /// 窗体最大化按钮的样式
        /// </summary>
        public Geometry BtnMaximizePathData
        {
            get
            {
                return _btnMaximizePathData;
            }
            set
            {
                _btnMaximizePathData = value;
                OnPropertyChanged("BtnMaximizePathData");
            }
        }

        #endregion

        ResourceDictionary _resource = null;
        public ThCustomWindow()
        {
            InitializeStyle();
            //这里如果直接绑定到窗体的DataContext，如果外部再绑定，这里会丢失
            //this.DataContext = this;
            this.Loaded += delegate  
            {
                InitializeEvent();
            };

            // 解决最大化覆盖任务栏问题
            this.SourceInitialized += new EventHandler(win_SourceInitialized);
        }
        /// <summary>
        /// 加载自定义窗体样式
        /// </summary>
        private void InitializeStyle()
        {
            _resource = new ResourceDictionary();
            Uri uri = new Uri("ThControlLibraryWPF;component/WindowThemes/CustomStyleWindow.xaml", UriKind.Relative);
            _resource.Source = uri;
            this.Style = _resource["CustomWindowStyle"] as Style;
        }

        /// <summary>
        /// 加载按钮事件委托
        /// </summary>
        private void InitializeEvent()
        {
            ControlTemplate baseWindowTemplate = _resource["CustomWindowControlTemplate"] as ControlTemplate;
            Border tp = (Border)baseWindowTemplate.FindName("topborder", this);
            tp.MouseLeftButtonDown += delegate { this.DragMove(); };
            tp.DataContext = this;

            Button minBtn = (Button)baseWindowTemplate.FindName("btnMin", this);
            minBtn.Click += delegate{this.WindowState = WindowState.Minimized; };

            Button maxBtn = (Button)baseWindowTemplate.FindName("btnMax", this);
            maxBtn.Click += delegate
            {
                this.WindowState = (this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal);
                SetButtonStyle();
            };

            Button closeBtn = (Button)baseWindowTemplate.FindName("btnClose", this);
            closeBtn.Click += delegate{this.Close();};

            
            SetButtonStyle();
            SetButtonVisibility();
        }
        private void SetButtonVisibility() 
        {
            this.BtnMinimizeVisibility = Visibility.Visible;
            this.BtnMaximizeVisibility = Visibility.Visible;
            switch (this.ResizeMode)
            {
                case ResizeMode.NoResize:
                    this.BtnMinimizeVisibility = Visibility.Collapsed;
                    this.BtnMaximizeVisibility = Visibility.Collapsed;
                    break;
                case ResizeMode.CanMinimize:
                    this.BtnMaximizeVisibility = Visibility.Collapsed;
                    this.BtnMinimizeVisibility = Visibility.Visible;
                    break;
            }
        }
        private void SetButtonStyle() 
        {
            if (this.WindowState == WindowState.Maximized)
                this.BtnMaximizePathData = _resource["pathRestore"] as PathGeometry;
            else
                this.BtnMaximizePathData = _resource["pathMaximize"] as PathGeometry;
        }

        /// <summary>
        /// 重绘窗体大小
        /// </summary>
        void win_SourceInitialized(object sender, EventArgs e)
        {
            System.IntPtr handle = (new WinInterop.WindowInteropHelper(this)).Handle;
            WinInterop.HwndSource.FromHwnd(handle).AddHook(new WinInterop.HwndSourceHook(WindowProc));
        }

        #region 实现INotifyPropertyChanged接口
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion


        //////////////////////////////////////////////////////////////////////////
        // 使用Window API处理窗体大小  
        //////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// POINT aka POINTAPI
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>
            /// x coordinate of point.
            /// </summary>
            public int x;
            /// <summary>
            /// y coordinate of point.
            /// </summary>
            public int y;

            /// <summary>
            /// Construct a point of coordinates (x,y).
            /// </summary>
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        /// <summary>
        /// 窗体大小信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        };

        /// <summary> Win32 </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT
        {
            /// <summary> Win32 </summary>
            public int left;
            /// <summary> Win32 </summary>
            public int top;
            /// <summary> Win32 </summary>
            public int right;
            /// <summary> Win32 </summary>
            public int bottom;

            /// <summary> Win32 </summary>
            public static readonly RECT Empty = new RECT();

            /// <summary> Win32 </summary>
            public int Width
            {
                get { return Math.Abs(right - left); }  // Abs needed for BIDI OS
            }
            /// <summary> Win32 </summary>
            public int Height
            {
                get { return bottom - top; }
            }

            /// <summary> Win32 </summary>
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }


            /// <summary> Win32 </summary>
            public RECT(RECT rcSrc)
            {
                this.left = rcSrc.left;
                this.top = rcSrc.top;
                this.right = rcSrc.right;
                this.bottom = rcSrc.bottom;
            }

            /// <summary> Win32 </summary>
            public bool IsEmpty
            {
                get
                {
                    // BUGBUG : On Bidi OS (hebrew arabic) left > right
                    return left >= right || top >= bottom;
                }
            }
            /// <summary> Return a user friendly representation of this struct </summary>
            public override string ToString()
            {
                if (this == RECT.Empty) { return "RECT {Empty}"; }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }

            /// <summary> Determine if 2 RECT are equal (deep compare) </summary>
            public override bool Equals(object obj)
            {
                if (!(obj is Rect)) { return false; }
                return (this == (RECT)obj);
            }

            /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
            public override int GetHashCode()
            {
                return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            }


            /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
            public static bool operator ==(RECT rect1, RECT rect2)
            {
                return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom);
            }

            /// <summary> Determine if 2 RECT are different(deep compare)</summary>
            public static bool operator !=(RECT rect1, RECT rect2)
            {
                return !(rect1 == rect2);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            /// <summary>
            /// </summary>            
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            /// <summary>
            /// </summary>            
            public RECT rcMonitor = new RECT();

            /// <summary>
            /// </summary>            
            public RECT rcWork = new RECT();

            /// <summary>
            /// </summary>            
            public int dwFlags = 0;
        }

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        private static System.IntPtr WindowProc(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }
            return (System.IntPtr)0;
        }

        /// <summary>
        /// 获得并设置窗体大小信息
        /// </summary>
        private static void WmGetMinMaxInfo(System.IntPtr hwnd, System.IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            System.IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != System.IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
            }
            Marshal.StructureToPtr(mmi, lParam, true);
        }
    }
}
