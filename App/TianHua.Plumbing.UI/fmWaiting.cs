using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;

namespace TianHua.Plumbing.UI
{
    /// <summary>
    /// 旋转等待方法执行窗体
    /// </summary>
    public partial class fmWaiting : DevExpress.XtraEditors.XtraForm
    {
        private Action Method { get; set; }

        /// <summary>
        /// 旋转等待指定方法执行结束
        /// </summary>
        /// <param name="_Method">无入参和返回值方法</param>
        /// <param name="_DisplayWaitStr">需要显示的等待消息</param>
        public static void WaitingExcute(Action _Method, string _DisplayWaitStr)
        {
            WaitingExcute(null, _Method, _DisplayWaitStr);
        }

        /// <summary>
        /// 旋转等待指定方法执行结束
        /// </summary>
        /// <param name="_Handle">需要锁定为模式窗口的窗体</param>
        /// <param name="_Method">无入参和返回值方法</param>
        /// <param name="_DisplayWaitStr">需要显示的等待消息</param>
        public static void WaitingExcute(Form _Handle, Action _Method, string _DisplayWaitStr)
        {
            using (var fm = new fmWaiting(_Method, _DisplayWaitStr))
                fm.ShowDialog(_Handle);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_Method">执行方法(无入参和返回值)</param>
        /// <param name="_DisplayWaitStr"></param>
        public fmWaiting(Action _Method, string _DisplayWaitStr)
        {
            InitializeComponent();
            this.Method = _Method;
            this.label1.Text = _DisplayWaitStr;
        }

        private void fmWaiting_Load(object sender, EventArgs e)
        {
            loadingCircle1.Size = new System.Drawing.Size(45, 45);
            loadingCircle1.OuterCircleRadius = 15;
            loadingCircle1.SpokeThickness = 3;
            Task.Factory.StartNew((_current) =>
            {
                try
                {
                    Thread.CurrentThread.CurrentUICulture = _current as System.Globalization.CultureInfo;
                    Method.Invoke();
                }
                catch{   }
            }, Thread.CurrentThread.CurrentUICulture).ContinueWith(result => this.DialogResult = DialogResult.OK);
        }
    }
}