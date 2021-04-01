using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TianHua.Hvac.UI
{
    public partial class fmBypass : Form
    {
        public string IValveWidth { get; set; }
        public string TeeWidth { get; set; }
        public string OValveWidth { get; set; }
        public string tee_pattern { get; set; }
        private string air_vloume { get;}
        public fmBypass(string airVloume)
        {
            InitializeComponent();
            air_vloume = airVloume;
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
                    tee_pattern = c.Name;
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
                IValveWidth = str[0];
                OValveWidth = str[1];
                TeeWidth = s;
            }
            else
            {
                if (!string.IsNullOrEmpty(textBox2.Text) && !string.IsNullOrEmpty(textBox3.Text))
                {
                    IValveWidth = textBox2.Text;
                    OValveWidth = textBox3.Text;
                    TeeWidth = textBox2.Text + "x" + textBox3.Text;
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
            label1.Visible = false;
            label2.Visible = false;
            textBox2.Visible = false;
            textBox3.Visible = false;
            textBox1.Text = "8";
            textBox2.Text = "100";
            textBox3.Text = "100";
        }

        private void RadType1_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Visible = true;
            listBox1.Visible = true;
            textBox2.Visible = false;
            textBox3.Visible = false;
            label1.Visible = false;
            label2.Visible = false;
            label3.Visible = true;
            label4.Visible = true;
        }

        private void RadType2_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Visible = false;
            listBox1.Visible = false;
            textBox2.Visible = true;
            textBox3.Visible = true;
            label1.Visible = true;
            label2.Visible = true;
            label3.Visible = false;
            label4.Visible = false;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox2.Text) &&
                !string.IsNullOrEmpty(textBox3.Text))
            {
                double air_speed = Double.Parse(air_vloume) / 3600 * 
                                   Double.Parse(textBox2.Text) * 
                                   Double.Parse(textBox3.Text) / 1000000;
                label2.Text = "计算风速    " + air_speed.ToString("0.00") + " m/s";
                TeeWidth = textBox2.Text + "x" + textBox3.Text;
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox2.Text) &&
                !string.IsNullOrEmpty(textBox3.Text))
            {
                label2.Text = "计算风速    " +
                          (Double.Parse(textBox2.Text) *
                           Double.Parse(textBox3.Text)).ToString() +
                           " m/s";
                TeeWidth = textBox2.Text + "x" + textBox3.Text;
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
    }
}
