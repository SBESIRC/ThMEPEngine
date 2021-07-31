using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Model;

namespace TianHua.Hvac.UI
{
    public partial class fmDuctPorts : Form
    {
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
        public fmDuctPorts(DuctPortsParam param)
        {
            InitializeComponent();
            if (Math.Abs(param.air_volume) > 1e-3)
            {
                comboBox2.Text = param.scenario;
                Scenario_init();
                Component_init(param);
            }
            else
            {
                Combobox_init();
                Scenario_init();
            }
            Duct_size_init();
            Set_duct_variables();
            Port_init();
            Set_port_speed();
            Set_port_range();
        }

        private void Component_init(DuctPortsParam param)
        {
            textBox2.Text = param.air_volume.ToString();
            textBox3.Text = param.air_speed.ToString();
            textBox4.Text = param.elevation.ToString();
            textBox7.Text = param.port_num.ToString();
            string []s = param.port_size.Split('x');
            textBox8.Text = s[0];
            textBox1.Text = s[1];
            textBox9.Text = param.port_name;
            comboBox1.Text = param.scale;
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
        private void Combobox_init()
        {
            comboBox1.Text = "1:150";
            comboBox2.Text = "消防补风兼平时送风";
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
            double avg_air_volumn = air_volume / port_num;
            port_size = textBox8.Text + "x" + textBox1.Text;
            double speed = Calc_air_speed(avg_air_volumn, Double.Parse(textBox8.Text), Double.Parse(textBox1.Text));
            label22.Text = speed.ToString("0.00");
        }

        private void Duct_size_init()
        {
            if (String.IsNullOrEmpty(textBox2.Text) || String.IsNullOrEmpty(textBox3.Text))
                return;
            air_volume = Double.Parse(textBox2.Text);
            Limit_air_volumn(ref air_volume);
            air_speed = Double.Parse(textBox3.Text);
            Limit_air_speed(ref air_speed);
            Update_recommend_duct_size_list(air_volume, air_speed);
            if (listBox1.SelectedItem != null)
                duct_size = listBox1.SelectedItem.ToString();
        }
        private void Update_recommend_duct_size_list(double air_volume, double air_speed)
        {
            if (Math.Abs(air_speed) < 1e-3 || Math.Abs(air_volume) < 1e-3)
                return;
            var Duct = new ThDuctParameter(air_volume, air_speed, true);
            listBox1.Items.Clear();
            foreach (var duct_size in Duct.DuctSizeInfor.DefaultDuctsSizeString)
                listBox1.Items.Add(duct_size);
            listBox1.SelectedItem = Duct.DuctSizeInfor.RecommendOuterDuctSize;
        }
        private void Limit_air_volumn(ref double air_volumn)
        {
            if (Math.Abs(air_volumn) < 1e-3)
                return;
            double air_volumn_floor = 1500;
            double air_volumn_ceiling = 60000;
            if (air_volumn > air_volumn_ceiling)
            {
                air_volumn = air_volumn_ceiling;
                textBox2.Text = air_volumn_ceiling.ToString();
            }
            if (air_volumn < air_volumn_floor)
                air_volumn = air_volumn_floor;
        }
        private void Limit_air_speed(ref double air_speed)
        {
            if (Math.Abs(air_speed) < 1e-3)
                return;
            if (air_speed > air_speed_max)
            {
                air_speed = air_speed_max;
                textBox3.Text = air_speed_max.ToString();
            }
            if (air_speed < air_speed_min)
                air_speed = air_speed_min;
        }
        private void Port_init()
        {
            switch (comboBox2.Text)
            {
                case "消防排烟":
                case "厨房排油烟":
                case "平时排风":
                case "消防排烟兼平时排风":
                case "事故排风":
                case "平时排风兼事故排风":
                    radioButton3.Text = "下回单层百叶";
                    radioButton4.Text = "侧回单层百叶";
                    break;
                case "消防补风":
                case "消防加压送风":
                case "厨房排油烟补风":
                case "平时送风":
                case "消防补风兼平时送风":
                case "事故补风":
                case "平时送风兼事故补风":
                    radioButton3.Text = "下送单层百叶";
                    radioButton4.Text = "侧送单层百叶";
                    break;
            }
        }
        private void Scenario_init()
        {
            switch (comboBox2.Text)
            {
                case "消防排烟":
                case "消防补风":
                case "消防加压送风":
                    air_speed = 15;
                    air_speed_min = 5;
                    air_speed_max = 20;
                    break;
                case "厨房排油烟":
                case "厨房排油烟补风":
                case "事故排风":
                case "事故补风":
                case "平时送风":
                case "平时排风":
                case "消防排烟兼平时排风":
                case "消防补风兼平时送风":
                case "平时送风兼事故补风":
                case "平时排风兼事故排风":
                    air_speed = 8;
                    air_speed_min = 5;
                    air_speed_max = 10;
                    break;
            }
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
            double air_speed = Calc_air_speed(air_volume, Double.Parse(textBox5.Text), Double.Parse(textBox6.Text));
            label13.Text = air_speed.ToString("0.00");
            duct_size = textBox6.Text + "x" + textBox5.Text;
        }
        private double Calc_air_speed(double air_volume, double duct_width, double duct_height)
        {
            return air_volume / 3600 / (duct_width * duct_height / 1000000);
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
            if (!Is_integer_str(textBox1.Text))
                textBox1.Text = "";
            else
                Set_port_speed();
        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (Is_integer_str(textBox2.Text))
            {
                if (!String.IsNullOrEmpty(textBox2.Text))
                {
                    air_volume = Double.Parse(textBox2.Text);
                    Limit_air_volumn(ref air_volume);
                    Set_port_speed();
                    Update_recommend_duct_size_list(air_volume, air_speed);
                }
            }
            else
                textBox2.Text = "";
        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        { 
            if (Is_float_2_decimal(textBox3.Text))
            {
                if (!String.IsNullOrEmpty(textBox3.Text))
                {
                    air_speed = Double.Parse(textBox3.Text);
                    Limit_air_speed(ref air_speed);
                    Update_recommend_duct_size_list(air_volume, air_speed);
                }
            }
            else
                textBox3.Text = "";
            
        }
        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (Is_float_2_decimal(textBox4.Text))
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
            if (!Is_integer_str(textBox5.Text))
                textBox5.Text = "";
            else
                Update_duct_size();
        }
        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            if (!Is_integer_str(textBox6.Text))
                textBox6.Text = "";
            else
                Update_duct_size();
        }
        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            if (Is_integer_str(textBox7.Text))
            {
                if (!String.IsNullOrEmpty(textBox7.Text))
                {
                    port_num = (int)Double.Parse(textBox7.Text);
                    Set_port_speed();
                }
            }
            else
                textBox7.Text = "";
        }
        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            if (!Is_integer_str(textBox8.Text))
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
            Scenario_init();
            Port_init();
            scenario = comboBox2.Text;
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            duct_size = (string)listBox1.SelectedItem;
        }
        private bool Is_float_2_decimal(string text)
        {
            string reg = "^[0-9]*[.]?[0-9]{0,2}$";
            return Regex.Match(text, reg).Success;
        }
        private bool Is_integer_str(string text)
        {
            string reg = "^[0-9]*$";
            return Regex.Match(text, reg).Success;
        }
    }
}