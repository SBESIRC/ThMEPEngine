using System;
using System.Windows.Forms;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Model;

namespace TianHua.Hvac.UI
{
    public partial class fmBypass : Form
    {
        public double air_vloume;
        public string valve_width;
        public string bypass_size;
        public string o_valve_width;
        public string bypass_pattern;
        public DuctSpecModel fan_model;
        public fmBypass(double air_volume)
        {
            InitializeComponent();
            splitContainer2.Panel2Collapsed = true;
            air_vloume = air_volume * 0.5;
        }
        public void InitForm(DuctSpecModel _DuctSpecModel)
        {
            fan_model = _DuctSpecModel;
        }
        private void Rad_Properties_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Rad_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void search_tee_pattern()
        {
            foreach (Control c in groupBox1.Controls)
            {
                if (c is RadioButton && (c as RadioButton).Checked)
                {
                    bypass_pattern = c.Name;
                    break;
                }
            }
        }
        private void search_valve()
        {
            if (RadType1.Checked)
            {
                string s = listBox1.SelectedItem.ToString();
                string[] str = s.Split('x');
                valve_width = str[0];
                o_valve_width = str[1];
                bypass_size = s;
            }
            else
            {
                if (!string.IsNullOrEmpty(textBox2.Text) && !string.IsNullOrEmpty(textBox3.Text))
                {
                    valve_width = textBox2.Text;
                    o_valve_width = textBox3.Text;
                    bypass_size = textBox2.Text + "x" + textBox3.Text;
                }
            }
        }
        private void buttonOK_Click(object sender, EventArgs e)
        {
            search_tee_pattern();
            search_valve();
            this.Close();
        }

        private void fmBypass_Load(object sender, EventArgs e)
        {
            listBox1.SelectedIndex = 0;
            textBox1.Text = "8";
            textBox2.Text = "100";
            textBox3.Text = "100";
            RadType1.Checked = true;
            RadType2.Checked = false;
            splitContainer2.Panel1Collapsed = false;
            splitContainer2.Panel2Collapsed = true;
        }

        private void RadType1_CheckedChanged(object sender, EventArgs e)
        {
            if (RadType1.Checked)
            {
                splitContainer2.Panel1Collapsed = false;
                splitContainer2.Panel2Collapsed = true;
            }
            else
            {
                splitContainer2.Panel1Collapsed = true;
                splitContainer2.Panel2Collapsed = false;
            }
        }

        private void RadType2_CheckedChanged(object sender, EventArgs e)
        {
            if (RadType1.Checked)
            {
                splitContainer2.Panel1Collapsed = false;
                splitContainer2.Panel2Collapsed = true;
            }
            else
            {
                splitContainer2.Panel1Collapsed = true;
                splitContainer2.Panel2Collapsed = false;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox2.Text) && !string.IsNullOrEmpty(textBox3.Text))
            {
                double air_speed = air_vloume / 3600 / 
                                  (Double.Parse(textBox2.Text) *
                                   Double.Parse(textBox3.Text) / 1000000);
                label2.Text = air_speed.ToString("0.00");
                bypass_size = textBox2.Text + "x" + textBox3.Text;
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox2.Text) &&
                !string.IsNullOrEmpty(textBox3.Text))
            {
                double air_speed = air_vloume / 3600 /
                                  (Double.Parse(textBox2.Text) *
                                   Double.Parse(textBox3.Text) / 1000000);
                label2.Text = air_speed.ToString("0.00");
                bypass_size = textBox2.Text + "x" + textBox3.Text;
            }
        }

        private void RBType4_CheckedChanged(object sender, EventArgs e)
        {
            RadType1.Checked = true;
            RadType2.Checked = false;
            RadType1_CheckedChanged(sender, e);
        }

        private void RBType5_CheckedChanged(object sender, EventArgs e)
        {
            RadType1.Checked = true;
            RadType2.Checked = false;
            RadType1_CheckedChanged(sender, e);
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (((int)e.KeyChar < 48 && (int)e.KeyChar != 8 && e.KeyChar != '.') || ((int)e.KeyChar > 57))
            {
                e.Handled = true;
            }
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (((int)e.KeyChar < 48 && (int)e.KeyChar != 8 && e.KeyChar != '.') || ((int)e.KeyChar > 57))
            {
                e.Handled = true;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
                return;
            double air_speed = Double.Parse(textBox1.Text);
            if (air_speed > fan_model.MaxAirSpeed)
                air_speed = fan_model.MaxAirSpeed;
            if (air_speed < fan_model.MinAirSpeed)
                air_speed = fan_model.MinAirSpeed;
            
            ThDuctParameter Duct = new ThDuctParameter(air_vloume, air_speed, true);
            listBox1.Items.Clear();
            listBox1.Items.Add(Duct.DuctSizeInfor.RecommendInnerDuctSize);
            listBox1.SelectedItem = Duct.DuctSizeInfor.RecommendInnerDuctSize;
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }
        private void listBox1_MouseClick(object sender, MouseEventArgs e)
        {
            double air_speed = Double.Parse(textBox1.Text);
            if (air_speed > fan_model.MaxAirSpeed)
                air_speed = fan_model.MaxAirSpeed;
            if (air_speed < fan_model.MinAirSpeed)
                air_speed = fan_model.MinAirSpeed;
            textBox1.Text = air_speed.ToString();
        }
    }
}
