using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ThControlLibraryWPF.ControlUtils;

namespace ThControlLibraryWPF.CustomControl
{
    public class ThCustomWindow : Window
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

        public static readonly DependencyProperty WindowTitleRightTemplateProperty = DependencyProperty.Register("WindowTitleRightTemplate", typeof(ControlTemplate), typeof(ThCustomWindow), new PropertyMetadata(null));
        public ControlTemplate WindowTitleRightTemplate
        {
            get { return (ControlTemplate)GetValue(WindowTitleRightTemplateProperty); }
            set { SetValue(WindowTitleRightTemplateProperty, value); }
        }
        public static readonly DependencyProperty WindownTitleTemplateProperty = DependencyProperty.Register("WindownTitleTemplate", typeof(ControlTemplate), typeof(ThCustomWindow), new PropertyMetadata(null));
        public ControlTemplate WindownTitleTemplate
        {
            get { return (ControlTemplate)GetValue(WindownTitleTemplateProperty); }
            set { SetValue(WindownTitleTemplateProperty, value); }
        }

        protected string MutexName = Guid.NewGuid().ToString();
        private bool IsFirstInstance;
        private Mutex _WindowMutex;

        protected bool IsFirstWindowInstance()
        {
            // Allow for multiple runs but only try and get the mutex once
            if (_WindowMutex == null)
            {
                _WindowMutex = new Mutex(true, MutexName, out IsFirstInstance);
            }

            return IsFirstInstance;
        }

        static ThCustomWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ThCustomWindow), new FrameworkPropertyMetadata(typeof(ThCustomWindow)));
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.Loaded += new RoutedEventHandler(OnWindow_Loaded);
            this.Closed += new EventHandler(OnWindow_Closed);
            var windowBehaviorHelper = new WindowBehaviorHelper(this);
            windowBehaviorHelper.RepairBehavior();
        }
        private void OnWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!IsFirstWindowInstance())
                Close();
        }
        void OnWindow_Closed(object sender, EventArgs e)
        {
            // Close and dispose our mutex.
            if (_WindowMutex != null)
            {
                _WindowMutex.Dispose();
            }
        }

        public bool CheckInputData()
        {
            //获取该页面中的Textbox进行验证是否有输入不正确的数据
            var allTextBox = FindControlUtil.GetChildObjects<TextBox>(this, "").ToList();
            List<string> errorMsgs = new List<string>();
            foreach (var textBox in allTextBox)
            {
                var errors = Validation.GetErrors(textBox);
                if (errors == null || errors.Count < 1)
                    continue;
                foreach (var error in errors)
                {
                    var errorStr = error.ErrorContent.ToString();
                    if (string.IsNullOrEmpty(errorStr))
                        continue;
                    errorMsgs.Add(errorStr);
                }
            }
            return errorMsgs.Count < 1;
        }
    }
}
