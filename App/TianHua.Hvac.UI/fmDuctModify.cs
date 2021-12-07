using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace TianHua.Hvac.UI
{
    public partial class fmDuctModify : Form
    {
        public string ductSize;
        public double airVolume;
        public fmDuctModify(double airVolume, string ductSize)
        {
            InitializeComponent();
            AcceptButton = button1;
            label5.Text = airVolume.ToString("0.");
            this.ductSize = ductSize;
            this.airVolume = airVolume;
            label7.Text = this.ductSize;
            string[] s = this.ductSize.Split('x');
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
            ductSize = textBox8.Text + "x" + textBox1.Text;
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
            double speed = Calc_air_speed(airVolume, Double.Parse(textBox8.Text), Double.Parse(textBox1.Text));
            label22.Text = speed.ToString("0.00");
        }
        private double Calc_air_speed(double airVolume, double ductWidth, double ductHeight)
        {
            return airVolume / 3600 / (ductWidth * ductHeight / 1000000);
        }
    }
}
