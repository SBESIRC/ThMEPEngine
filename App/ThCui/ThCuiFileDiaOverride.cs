using System;
using Autodesk.AutoCAD.ApplicationServices;

namespace TianHua.AutoCAD.ThCui
{
    public class ThCuiFileDiaOverride : IDisposable
    {
        private int FileDia { get; set; }
        public ThCuiFileDiaOverride()
        {
            FileDia = Convert.ToInt32(Application.GetSystemVariable("FILEDIA"));
            Application.SetSystemVariable("FILEDIA", 1);
        }

        public void Dispose()
        {
            Application.SetSystemVariable("FILEDIA", FileDia);
        }
    }
}
