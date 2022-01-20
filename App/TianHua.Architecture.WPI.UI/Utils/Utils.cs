using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;

namespace TianHua.Architecture.WPI.UI
{
    public class Utils
    {
        static private bool IsInputCodeCorrect = false;

        private static bool InputPassCodeCorrect()
        {
            var InputCode = Active.Editor.GetString("\n 请输入测试代码:");

            if (InputCode.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return false;

            DateTime today = DateTime.Today;
            string[] array = today.ToString("M/d").Split(new char[] { '/' }, 2);
            int Month = Int32.Parse(array[0]);

            string CorrectCode;
            if(Month < 10)
            {
                CorrectCode = "0" + Month.ToString();
            }
            else CorrectCode = Month.ToString();

            int Day = Int32.Parse(array[1]);
            if (Day < 15)
            {
                CorrectCode += "12";
            }
            else
            {
                CorrectCode += "34";
            }


            if (InputCode.StringResult == CorrectCode) return true;
            else 
            {
                Active.Editor.WriteMessage("测试代码不正确! \n");
                return false;
            } 
        }

        public static bool IsCorrectPassCode()
        {
            if (!IsInputCodeCorrect)
            {
                IsInputCodeCorrect = InputPassCodeCorrect();
                return IsInputCodeCorrect;
            }
            else return true;
        }
    }
}
