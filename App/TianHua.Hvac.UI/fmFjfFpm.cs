using System;
using System.Windows.Forms;
using ThMEPHVAC.Model;
using TianHua.Publics.BaseCode;

namespace TianHua.Hvac.UI
{
    public partial class fmFjfFpm : Form
    {
        public int port_num;
        public string scale;
        public string scenario;
        public string port_size;
        public string port_range;
        public string port_name;
        public string i_duct_size;
        public string o_duct_size;
        public double air_speed;
        public double elevation1 { get; set; }
        public double elevation2 { get; set; }
        public double air_volume { get; set; }
        private double air_speed_max;
        private double air_speed_min;
        public fmFjfFpm(DuctSpecModel _DuctSpecModel, bool is_exhaust)
        {
            InitializeComponent();
            AcceptButton = button1;
            textBox2.Text = _DuctSpecModel.StrAirVolume;
            textBox3.Text = FuncStr.NullToStr(_DuctSpecModel.AirSpeed);
            air_volume = Get_air_volume(textBox2.Text);
            air_speed = Double.Parse(textBox3.Text);
            Init_scenario();
            Update_port_name();
            Init_listbox(_DuctSpecModel, is_exhaust);
        }
        private void Init_scenario()
        {
            comboBox1.Text = "1:100";
            comboBox2.Text = "消防排烟兼平时排风";
            ThHvacUIService.Limit_air_speed_range(comboBox2.Text, ref air_speed, ref air_speed_min, ref air_speed_max);
        }
        private void Init_listbox(DuctSpecModel _DuctSpecModel, bool is_exhaust)
        {
            if (String.IsNullOrEmpty(textBox2.Text) || String.IsNullOrEmpty(textBox3.Text))
                return;
            air_volume = Get_air_volume(textBox2.Text);
            Update_air_volume();
            air_speed = Double.Parse(textBox3.Text);
            Update_air_speed();
            ThHvacUIService.Update_recommend_duct_size_list(listBox1, air_volume, air_speed);
            ThHvacUIService.Update_recommend_duct_size_list(listBox2, air_volume, air_speed);
            listBox1.SelectedItem = is_exhaust ? _DuctSpecModel.OuterTube : _DuctSpecModel.InnerTube;
            listBox2.SelectedItem = is_exhaust ? _DuctSpecModel.InnerTube : _DuctSpecModel.OuterTube;
        }
        private void air_volume_changed(object sender, EventArgs e)
        {
            if (ThHvacUIService.Is_integer_str(textBox2.Text) || 
                ThHvacUIService.Is_double_volume(textBox2.Text))
            {
                if (!String.IsNullOrEmpty(textBox2.Text))
                {
                    air_volume = Get_air_volume(textBox2.Text);
                    Update_air_volume();
                    Set_port_speed();
                    ThHvacUIService.Update_recommend_duct_size_list(listBox1, air_volume, air_speed);
                    ThHvacUIService.Update_recommend_duct_size_list(listBox2, air_volume, air_speed);
                }
            }
            else
                textBox2.Text = "";
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
            double volume = air_volume;
            ThHvacUIService.Limit_air_volume(out bool is_high, ref volume);
            air_volume = volume;
            double max = 60000;
            if (is_high)
                textBox2.Text = max.ToString();
        }
        private void Update_in_duct_size()
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
        }
        private void Update_out_duct_size()
        {
            if (String.IsNullOrEmpty(textBox11.Text) || String.IsNullOrEmpty(textBox10.Text))
            {
                string s = listBox2.SelectedItem.ToString();
                string[] str = s.Split('x');
                textBox11.Text = str[0];
                textBox10.Text = str[1];
                return;
            }
            double air_speed = ThHvacUIService.Calc_air_speed(air_volume, Double.Parse(textBox10.Text), Double.Parse(textBox11.Text));
            label29.Text = air_speed.ToString("0.00");
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
        private void air_speed_changed(object sender, EventArgs e)
        {
            if (ThHvacUIService.Is_float_2_decimal(textBox3.Text))
            {
                if (!String.IsNullOrEmpty(textBox3.Text))
                {
                    air_speed = Double.Parse(textBox3.Text);
                    Update_air_speed();
                    ThHvacUIService.Update_recommend_duct_size_list(listBox1, air_volume, air_speed);
                    ThHvacUIService.Update_recommend_duct_size_list(listBox2, air_volume, air_speed);
                }
            }
            else
                textBox3.Text = "";
        }

        private void elevation1_changed(object sender, EventArgs e)
        {
            if (ThHvacUIService.Is_float_2_decimal(textBox4.Text))
            {
                if (!String.IsNullOrEmpty(textBox4.Text))
                    elevation1 = Double.Parse(textBox4.Text);
            }
            else
                textBox4.Text = "";
        }

        private void elevation2_changed(object sender, EventArgs e)
        {
            if (ThHvacUIService.Is_float_2_decimal(textBox12.Text))
            {
                if (!String.IsNullOrEmpty(textBox12.Text))
                    elevation2 = Double.Parse(textBox12.Text);
            }
            else
                textBox12.Text = "";
        }

        private void scenario_changed(object sender, EventArgs e)
        {
            ThHvacUIService.Limit_air_speed_range(comboBox2.Text, ref air_speed, ref air_speed_min, ref air_speed_max);
            Update_port_name();
            scenario = comboBox2.Text;
            Set_port_range();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
                radioButton2.Checked = false;
            splitContainer5.Panel1Collapsed = false;
            splitContainer5.Panel2Collapsed = true;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
                radioButton1.Checked = false;
            splitContainer5.Panel1Collapsed = true;
            splitContainer5.Panel2Collapsed = false;
            Update_in_duct_size();
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton6.Checked)
                radioButton5.Checked = false;
            splitContainer8.Panel1Collapsed = false;
            splitContainer8.Panel2Collapsed = true;
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked)
                radioButton6.Checked = false;
            splitContainer8.Panel1Collapsed = true;
            splitContainer8.Panel2Collapsed = false;
            Update_out_duct_size();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.Is_integer_str(textBox1.Text))
                textBox1.Text = "";
            else
                Set_port_speed();
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.Is_integer_str(textBox8.Text))
                textBox8.Text = "";
            else
                Set_port_speed();
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.Is_integer_str(textBox5.Text))
                textBox5.Text = "";
            else
                Update_in_duct_size();
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.Is_integer_str(textBox6.Text))
                textBox6.Text = "";
            else
                Update_in_duct_size();
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
        private void textBox11_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.Is_integer_str(textBox11.Text))
                textBox11.Text = "";
            else
                Update_out_duct_size();
        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.Is_integer_str(textBox10.Text))
                textBox10.Text = "";
            else
                Update_out_duct_size();
        }

        private void btnOK(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            air_volume = Get_air_volume(textBox2.Text);
            air_speed = Double.Parse(textBox3.Text);
            elevation1 = Double.Parse(textBox4.Text);
            elevation2 = Double.Parse(textBox12.Text);
            scale = comboBox1.Text;
            scenario = comboBox2.Text;
            i_duct_size = radioButton1.Checked ?
                         (string)listBox1.SelectedItem :
                          textBox6.Text + "x" + textBox5.Text;
            o_duct_size = radioButton6.Checked ?
                         (string)listBox2.SelectedItem :
                         textBox11.Text + "x" + textBox10.Text;
            port_name = textBox9.Text;
            Set_port_range();
            this.Close();
        }
        private double Get_air_volume(string str_volume)
        {
            if (str_volume.Contains("/"))
            {
                string[] str = textBox2.Text.Split('/');
                return Double.Parse(str[1]);
            }
            else
                return Double.Parse(textBox2.Text);
        }
    }
}
