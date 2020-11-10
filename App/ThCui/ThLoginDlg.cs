using System;
using System.Windows.Forms;
using ThIdentity.SDK;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;


namespace TianHua.AutoCAD.ThCui
{
    public partial class ThLoginDlg : Form
    {
        public string User
        {
            get
            {
                return textBox_user_name.Text;
            }
        }

        public string Password
        {
            get
            {
                return textBox_password.Text;
            }
        }

        public ThLoginDlg()
        {
            InitializeComponent();
        }

        private void button_ok_Click(object sender, EventArgs e)
        {
            if (comboBox_profile.SelectedIndex == -1)
            {
                AcadApp.ShowAlertDialog("请选择您的专业！");
                DialogResult = DialogResult.None;
                return;
            }
            if (!ThIdentityService.Login(User, Password))
            {
                AcadApp.ShowAlertDialog("登录失败！请重新登录");
                DialogResult = DialogResult.None;
                return;
            }
            ThCuiProfileManager.Instance.CurrentProfile = (Profile)comboBox_profile.SelectedIndex;
        }
    }
}
