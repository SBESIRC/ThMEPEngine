using AcHelper;
using DotNetARX;
using Autodesk.AutoCAD.Customization;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.AutoCAD.ThCui
{
    public class ThPartialCui
    {
        public void Create(string cuiFile, string menuGroupName)
        {
            try
            {
                CustomizationSection cs = Active.Document.AddCui(cuiFile, menuGroupName);
                ThRibbonBar.CreateThRibbonBar(cs);
                if (cs.IsModified)
                {
                    cs.Save();
                }
            }
            catch
            {
            }
        }

        public void Load(string cuiFile, string menuGroupName)
        {
            try
            {
                CustomizationSection cs = new CustomizationSection(cuiFile);
                cs.LoadCui();
            }
            catch
            {
            }
        }

        public void UnLoad(string cuiFile, string menuGroupName)
        {
            try
            {
                // 在CAD关闭的时候，UI环境已经被释放了，调用Application.UnloadPartialMenu()会失败
                // 这里使用CustomizationSection.RemovePartialMenu()来“释放”局部CUIX文件
                //  https://adndevblog.typepad.com/autocad/2012/07/unload-partial-cuix-when-autocad-quits.html
                string mainCuiFile = AcadApp.GetSystemVariable("MENUNAME") + ".cuix";
                CustomizationSection cs = new CustomizationSection(mainCuiFile);
                if (cs.PartialCuiFiles.Contains(cuiFile))
                {
                    cs.RemovePartialMenu(cuiFile, menuGroupName);
                }
                if (cs.IsModified)
                {
                    cs.Save();
                }
            }
            catch
            {
            }
        }
    }
}
