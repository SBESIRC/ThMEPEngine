using System;
using System.Windows.Forms;
using ThMEPHVAC.Model;

namespace TianHua.Hvac.UI
{
    public partial class fmDuctPorts : Form
    {
        public bool is_redraw;
        public int port_num;
        public double air_volume;
        public double elevation;
        public double air_speed;
        public string duct_size;
        public string port_size;
        public string graph_scale;
        public string scenario;
        public string port_name;
        public string port_range;
        private double air_speed_max;
        private double air_speed_min;
        private ThMEPHVACParam param;
        public fmDuctPorts(ThMEPHVACParam param)
        {
            InitializeComponent();
            AcceptButton = button1;
            this.param = param;
            checkBox1.Enabled = param.is_redraw;
            Init_scenario();
            Duct_size_init();
            Set_duct_variables();
            Update_port_name();
            Set_port_speed();
            Set_port_range();
            is_redraw = checkBox1.Checked;
        }
        private void Init_scenario()
        {
            if (Math.Abs(param.air_volume) > 1e-3)
                Init_by_pre_param();
            else
            {
                comboBox1.Text = "1:100";
                comboBox2.Text = "消防排烟兼平时排风";
            }
            ThHvacUIService.Limit_air_speed_range(comboBox2.Text, ref air_speed, ref air_speed_min, ref air_speed_max);
        }
        private void Update_port_name()
        {
            ThHvacUIService.Port_init(comboBox2.Text, out string down_port_name, out string side_port_name);
            radioButton3.Text = down_port_name;
            radioButton4.Text = side_port_name;
        }
        private void Update_air_speed()
        {
            // 高于风速上限立即在面板更新
            ThHvacUIService.Limit_air_speed(air_speed_max, air_speed_min, out bool is_high, ref air_speed);
            if (is_high)
                textBox3.Text = air_speed_max.ToString();
        }
        private void Update_air_volume()
        {
            // 高于风量上限立即在面板更新
            ThHvacUIService.Limit_air_volume(out bool is_high, ref air_volume);
            double max = 60000;
            if (is_high)
                textBox2.Text = max.ToString();
        }
        private void Init_by_pre_param()
        {
            comboBox1.Text = param.scale;
            comboBox2.Text = param.scenario;
            textBox2.Text = param.air_volume.ToString();
            textBox3.Text = param.air_speed.ToString();
            textBox4.Text = param.elevation.ToString();
            textBox7.Text = param.port_num.ToString();
            string []s = param.port_size.Split('x');
            textBox8.Text = s[0];
            textBox1.Text = s[1];
            textBox9.Text = param.port_name;
            if (param.port_range.Contains("下"))
            {
                radioButton3.Checked = true;
                radioButton4.Checked = false;
            }
            else
            {
                radioButton3.Checked = false;
                radioButton4.Checked = true;
            }
            checkBox1.Enabled = param.is_redraw;
        }
        private void buttonOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            port_name = textBox9.Text;
            if (radioButton1.Checked)
                duct_size = (string)listBox1.SelectedItem;
            else if (radioButton2.Checked)
                duct_size = textBox6.Text + "x" + textBox5.Text;
            air_volume = Double.Parse(textBox2.Text);
            air_speed = Double.Parse(textBox3.Text);
            elevation = Double.Parse(textBox4.Text);
            port_num = (int)Double.Parse(textBox7.Text);
            port_size = textBox8.Text + "x" + textBox1.Text;
            this.Close();
        }
        private void Set_port_range()
        {
            if (radioButton3.Checked)
                port_range = radioButton3.Text;
            else if (radioButton4.Checked)
                port_range = radioButton4.Text;
        }
        private void Set_port_speed()
        {
            if (String.IsNullOrEmpty(textBox7.Text) || String.IsNullOrEmpty(textBox8.Text) || String.IsNullOrEmpty(textBox1.Text))
                return;
            port_num = (int)Double.Parse(textBox7.Text);
            double avg_air_volume = air_volume / port_num;
            port_size = textBox8.Text + "x" + textBox1.Text;
            double speed = ThHvacUIService.Calc_air_speed(avg_air_volume, Double.Parse(textBox8.Text), Double.Parse(textBox1.Text));
            label22.Text = speed.ToString("0.00");
        }
        private void Duct_size_init()
        {
            if (String.IsNullOrEmpty(textBox2.Text) || String.IsNullOrEmpty(textBox3.Text))
                return;
            air_volume = Double.Parse(textBox2.Text);
            Update_air_volume();
            air_speed = Double.Parse(textBox3.Text);
            Update_air_speed();
            ThHvacUIService.Update_recommend_duct_size_list(listBox1, air_volume, air_speed);
            if (listBox1.SelectedItem != null)
                duct_size = listBox1.SelectedItem.ToString();
        }
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
                radioButton2.Checked = false;
            splitContainer3.Panel1Collapsed = false;
            splitContainer3.Panel2Collapsed = true;
        }
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
                radioButton1.Checked = false;
            splitContainer3.Panel1Collapsed = true;
            splitContainer3.Panel2Collapsed = false;
            Update_duct_size();
        }
        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
                radioButton4.Checked = false;
            port_range = radioButton3.Text;
        }
        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
                radioButton3.Checked = false;
            port_range = radioButton4.Text;
        }
        private void Update_duct_size()
        {
            if (String.IsNullOrEmpty(textBox6.Text) || String.IsNullOrEmpty(textBox5.Text))
            {
                string s = listBox1.SelectedItem.ToString();
                string[] str = s.Split('x');
                textBox6.Text = str[0];
                textBox5.Text = str[1];
                return;
            }
            double air_speed = ThHvacUIService.Calc_air_speed(air_volume, Double.Parse(textBox5.Text), Double.Parse(textBox6.Text));
            label13.Text = air_speed.ToString("0.00");
            duct_size = textBox6.Text + "x" + textBox5.Text;
        }
        private void Set_duct_variables()
        {
            if (String.IsNullOrEmpty(textBox2.Text) || String.IsNullOrEmpty(textBox3.Text) || String.IsNullOrEmpty(textBox4.Text))
                return;
            if (radioButton1.Checked)
            {
                if (listBox1.SelectedItem != null)
                    duct_size = listBox1.SelectedItem.ToString();
                air_speed = Double.Parse(textBox3.Text);
            }
            else
            {
                if (String.IsNullOrEmpty(textBox6.Text) || String.IsNullOrEmpty(textBox5.Text) || String.IsNullOrEmpty(label13.Text))
                    duct_size = textBox6.Text + "x" + textBox5.Text;
                air_speed = Double.Parse(label13.Text);
            }
            graph_scale = comboBox1.Text;
            scenario = comboBox2.Text;
            air_volume = Double.Parse(textBox2.Text);
            elevation = Double.Parse(textBox4.Text);
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.Is_integer_str(textBox1.Text))
                textBox1.Text = "";
            else
                Set_port_speed();
        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (ThHvacUIService.Is_integer_str(textBox2.Text))
            {
                if (!String.IsNullOrEmpty(textBox2.Text))
                {
                    air_volume = Double.Parse(textBox2.Text);
                    Update_air_volume();
                    Set_port_speed();
                    ThHvacUIService.Update_recommend_duct_size_list(listBox1, air_volume, air_speed);
                }
            }
            else
                textBox2.Text = "";
        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        { 
            if (ThHvacUIService.Is_float_2_decimal(textBox3.Text))
            {
                if (!String.IsNullOrEmpty(textBox3.Text))
                {
                    air_speed = Double.Parse(textBox3.Text);
                    Update_air_speed();
                    ThHvacUIService.Update_recommend_duct_size_list(listBox1, air_volume, air_speed);
                }
            }
            else
                textBox3.Text = "";
        }
        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (ThHvacUIService.Is_float_2_decimal(textBox4.Text))
            {
                if (!String.IsNullOrEmpty(textBox4.Text))
                    elevation = Double.Parse(textBox4.Text);
            }
            else
                textBox4.Text = "";                
        }

        private void FmDuctPorts_LostFocus(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.Is_integer_str(textBox5.Text))
                textBox5.Text = "";
            else
                Update_duct_size();
        }
        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.Is_integer_str(textBox6.Text))
                textBox6.Text = "";
            else
                Update_duct_size();
        }
        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            if (ThHvacUIService.Is_integer_str(textBox7.Text))
            {
                if (!String.IsNullOrEmpty(textBox7.Text))
                    Set_port_speed();
            }
            else
                textBox7.Text = "";
        }
        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.Is_integer_str(textBox8.Text))
                textBox8.Text = "";
            else
                Set_port_speed();
        }
        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            port_name = textBox9.Text;
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            graph_scale = comboBox1.Text;
        }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            ThHvacUIService.Limit_air_speed_range(comboBox2.Text, ref air_speed, ref air_speed_min, ref air_speed_max);
            Update_port_name();
            scenario = comboBox2.Text;
            Set_port_range();
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            duct_size = (string)listBox1.SelectedItem;
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                splitContainer1.Panel1.Enabled = false;
                if (param.port_range.Contains("下"))
                {
                    radioButton3.Checked = true;
                    radioButton4.Checked = false;
                }
                else
                {
                    radioButton3.Checked = false;
                    radioButton4.Checked = true;
                }
                radioButton3.Enabled = false;
                radioButton4.Enabled = false;
                textBox7.Text = param.port_num.ToString();
                
                textBox7.Enabled = false;
                label22.Enabled = false;
                
            }
            else
            {
                splitContainer1.Panel1.Enabled = true;
                radioButton3.Enabled = true;
                radioButton4.Enabled = true;
                textBox7.Enabled = true;
                label22.Enabled = true;
            }
            is_redraw = checkBox1.Checked;
        }
    }
}