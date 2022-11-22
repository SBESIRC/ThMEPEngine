using System;
using System.Windows;
using ThPlatform3D.Model.User;
using ThPlatform3D.Service;

namespace Tianhua.Platform3D.UI.UI
{
    /// <summary>
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        UserInfo loginRes = null;
        public Login()
        {
            InitializeComponent();
            loginRes = null;
        }
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            loginRes = null;
            if (string.IsNullOrEmpty(txtUName.Text) || string.IsNullOrEmpty(txtUPsw.Password))
            {
                MessageBox.Show("用户名不能为空", "提醒", MessageBoxButton.OK);
            }
            string uName = txtUName.Text.ToString();
            string uPsw = txtUPsw.Password.ToString();
            UserLoginService userLogin = new UserLoginService("THCAD");
            try
            {
                loginRes = userLogin.UserLoginByNamePsw(uName, uPsw);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "登录失败提醒", MessageBoxButton.OK);
                return;
            }
            this.DialogResult = true;
            this.Close();
        }
        public UserInfo UserLoginInfo() 
        {
            return loginRes;
        }
    }
}
