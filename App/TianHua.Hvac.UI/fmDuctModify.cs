using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace TianHua.Hvac.UI
{
    public partial class fmDuctModify : Form
    {
        public string duct_size;
        public double air_volume;
        public fmDuctModify(double air_volume_, string duct_size_)
        {
            InitializeComponent();
            AcceptButton = button1;
            label5.Text = air_volume_.ToString("0.");
            duct_size = duct_size_;
            air_volume = air_volume_;
            label7.Text = duct_size;
            string[] s = duct_size.Split('x');
            if (s.Length == 2)
            {
                textBox8.Text = s[0];
                textBox1.Text = s[1];
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBox8.Text) || String.IsNullOrEmpty(textBox1.Text))
                return;
            duct_size = textBox8.Text + "x" + textBox1.Text;
            DialogResult = DialogResult.OK;
            this.Close();
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!Is_integer_str(textBox1.Text))
                textBox1.Text = "";
            else
                Set_port_speed();
        }
        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            if (!Is_integer_str(textBox8.Text))
                textBox8.Text = "";
            else
                Set_port_speed();
        }
        private bool Is_integer_str(string text)
        {
            string reg = "^[0-9]*$";
            return Regex.Match(text, reg).Success;
        }
        private void Set_port_speed()
        {
            if (String.IsNullOrEmpty(textBox8.Text) || String.IsNullOrEmpty(textBox1.Text))
                return;
            double speed = Calc_air_speed(air_volume, Double.Parse(textBox8.Text), Double.Parse(textBox1.Text));
            label22.Text = speed.ToString("0.00");
        }
        private double Calc_air_speed(double air_volume, double duct_width, double duct_height)
        {
            return air_volume / 3600 / (duct_width * duct_height / 1000000);
        }
    }
}
