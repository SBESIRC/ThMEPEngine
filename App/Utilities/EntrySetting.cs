//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Management;
//using System.Security.Cryptography;
//using System.Text;
//using System.Windows.Forms;
//using VB = Microsoft.VisualBasic;
//using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
//using System.Reflection;
//using System.IO;
//using System.Xml.Linq;

//namespace Tools
//{
//    public class EntrySetting
//    {
//        static string path = GetDataPath() + "Info.txt";
//        /// <summary>  
//        /// 获取本机MAC地址  
//        /// </summary>  
//        /// <returns>本机MAC地址</returns>  
//        public static string GetMacAddress()
//        {
//            try
//            {
//                string strMac = string.Empty;
//                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
//                ManagementObjectCollection moc = mc.GetInstances();
//                foreach (ManagementObject mo in moc)
//                {
//                    if ((bool)mo["IPEnabled"] == true)
//                    {
//                        strMac = mo["MacAddress"].ToString();
//                    }
//                }
//                moc = null;
//                mc = null;
//                return strMac;
//            }
//            catch
//            {
//                return "unknown";
//            }
//        }

//        public static string GetMd5(object text)
//        {
//            string path = text.ToString();

//            MD5CryptoServiceProvider MD5Pro = new MD5CryptoServiceProvider();
//            Byte[] buffer = Encoding.GetEncoding("utf-8").GetBytes(text.ToString());
//            Byte[] byteResult = MD5Pro.ComputeHash(buffer);

//            string md5result = BitConverter.ToString(byteResult).Replace("-", "");
//            return md5result;
//        }

//        public static void Login()
//        {
//            DateTime date = DateTime.Now;

//            var macAddress = GetMacAddress();
//            string inputText = "";

//            var count = 0;
//            while (inputText != GetMd5(macAddress))
//            {
//                inputText = VB.Interaction.InputBox("请输入注册码", "注册码输入框", "", 100, 100);
//                if (inputText == GetMd5(macAddress))
//                {
//                    //MessageBox.Show("验证成功！");
//                    break;
//                }
//                else
//                {
//                    if (count > 2)
//                    {
//                        MessageBox.Show("验证失败");
//                        AcadApp.Quit();
//                        break;
//                    }
//                    MessageBox.Show("注册码错误！请输入正确的注册码！还剩余" + (3 - count) + "次机会");
//                }
//                count++;
//            }


//        }

//        /// <summary>
//        /// 检验程序是否已经过期
//        /// </summary>
//        /// <returns></returns>
//        public static bool LoadSuccess()
//        {

//            //读取文本文件
//            var contents = File.ReadAllLines(path, Encoding.Default);

//            var firstTime = Convert.ToDateTime(Decrypt(contents[2]));
//            var lastTime = Convert.ToDateTime(Decrypt(contents[4]));
//            var finialTime = Convert.ToDateTime(Decrypt(contents[6]));
//            var loginNumber = Convert.ToInt32(Decrypt(contents[10]));

//            //如果时间相减超过62天或者登录超过登录上限62*30次了,则过期
//            return !(Math.Abs((lastTime - firstTime).Days) > 62 || (loginNumber > 62 * 30));

//            ////如果时间相减超过7天或者登录超过登录上限100次了，则需要重新验证，验证成功后，修改初始时间为当前时间,关闭程序时，修改当前时间为当前时间
//            //if (Math.Abs((lastTime - firstTime).Days) > 7 || loginNumber > 100)
//            //{
//            //    //Login();

//            //    //读取文本文件,修改初始时间为当前时间
//            //    contents = File.ReadAllLines(path, Encoding.Default);

//            //    //修改初始时间
//            //    contents[2] = Encryption(DateTime.Now.ToString());
//            //    //修改登录次数
//            //    contents[10] = Encryption("1");

//            //    FileStream fs = new FileStream(path, FileMode.Truncate, FileAccess.ReadWrite);
//            //    StreamWriter sr = new StreamWriter(fs);

//            //    foreach (var content in contents)
//            //    {
//            //        //每一行都重新解密再加密，保证一起变化
//            //        sr.WriteLine(Encryption(Decrypt(content)));
//            //    }

//            //    sr.Close();
//            //    fs.Close();
//            //}
//        }


//        public static void Set()
//        {
//            //读取文本文件
//            var contents = File.ReadAllLines(path, Encoding.Default);

//            //修改最后一次的登录时间
//            contents[4] = Encryption(DateTime.Now.ToString());
//            //修改登录次数
//            contents[10] = Encryption((Convert.ToInt32(Decrypt(contents[10])) + 1).ToString());

//            FileStream fs = new FileStream(path, FileMode.Truncate, FileAccess.ReadWrite);
//            StreamWriter sr = new StreamWriter(fs);

//            foreach (var content in contents)
//            {
//                //每一行都重新解密再加密，保证一起变化
//                sr.WriteLine(Encryption(Decrypt(content)));
//            }

//            sr.Close();
//            fs.Close();

//        }


//        //使用RSA加密、解密
//        //加密
//        private static string Encryption(string express)
//        {
//            CspParameters param = new CspParameters();
//            param.KeyContainerName = "oa_erp_dowork";//密匙容器的名称，保持加密解密一致才能解密成功
//            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(param))
//            {
//                byte[] plaindata = Encoding.Default.GetBytes(express);//将要加密的字符串转换为字节数组
//                byte[] encryptdata = rsa.Encrypt(plaindata, true);//将加密后的字节数据转换为新的加密字节数组
//                return Convert.ToBase64String(encryptdata);//将加密后的字节数组转换为字符串
//            }
//        }

//        //解密
//        private static string Decrypt(string ciphertext)
//        {
//            CspParameters param = new CspParameters();
//            param.KeyContainerName = "oa_erp_dowork";
//            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(param))
//            {
//                byte[] encryptdata = Convert.FromBase64String(ciphertext);
//                byte[] decryptdata = rsa.Decrypt(encryptdata, true);
//                return Encoding.Default.GetString(decryptdata);
//            }
//        }




//        public static XElement OpenXML()
//        {
//            //找到xml文件的路径，并读取
//            string path = GetDataPath();
//            string xmlName = path + "Info.xml";
//            //载入当前目录下的XML文件
//            return XElement.Load(xmlName);
//        }



//        public static string GetDataPath()
//        {
//            //获取当前dll所在的目录，返回上两级，进入data目录
//            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
//            UriBuilder uri = new UriBuilder(codeBase);
//            string path = Uri.UnescapeDataString(uri.Path);
//            var direc = Path.GetDirectoryName(path);
//            direc = direc.Right(@"bin\Debug") + @"Data\Info\";
//            return direc;
//        }

//        /// <summary>
//        /// 生成加密文件
//        /// </summary>
//        public static void GG()
//        {
//            //读取文本文件
//            var contents = File.ReadAllLines(GetDataPath()+ "Info.txt", Encoding.Default);

//            FileStream fs = new FileStream(GetDataPath() + "Info.txt", FileMode.Truncate, FileAccess.ReadWrite);
//            StreamWriter sr = new StreamWriter(fs);

//            foreach (var content in contents)
//            {
//                //每一行都重新解密再加密，保证一起变化
//                sr.WriteLine(Encryption(content));
//            }

//            sr.Close();
//            fs.Close();
//        }

//        /// <summary>
//        /// 生成解密文件
//        /// </summary>
//        public static void vv()
//        {
//            //读取文本文件
//            var contents = File.ReadAllLines(GetDataPath() + "Info.txt", Encoding.Default);

//            FileStream fs = new FileStream(GetDataPath() + "Info.txt", FileMode.Truncate, FileAccess.ReadWrite);
//            StreamWriter sr = new StreamWriter(fs);

//            foreach (var content in contents)
//            {
//                //每一行都重新解密再加密，保证一起变化
//                sr.WriteLine(Decrypt(content));
//            }

//            sr.Close();
//            fs.Close();
//        }

//    }
//}
