using AcHelper;
using Autodesk.AutoCAD.EditorInput;

namespace TianHua.Structure.WPF.UI.StructurePlane
{
    public class FileFormatSelectVM
    {
        private FileFormatSelectModel Model { get; set; }
        public FileFormatSelectVM()
        {
            Model = new FileFormatSelectModel();
        }
        public void Save()
        {
            Model.Write();
        }
        public string BrowseFile()
        {
            PromptOpenFileOptions ops = null;
            switch (Model.FileFormatOption)
            {
                case FileFormatOps.YDB:
                    ops = CreateYdbFileOption();
                    break;
                case FileFormatOps.IFC:
                    ops = CreateIfcFileOption();
                    break;
                case FileFormatOps.GET:
                    ops = CreateCacheFileOption();
                    break;
            }
            return SelectFile(ops);
        }

        private string SelectFile(PromptOpenFileOptions pofo)
        {            
            var pfnr = Active.Editor.GetFileNameForOpen(pofo);
            if(pfnr.Status == PromptStatus.OK)
            {
                return pfnr.StringResult;
            }
            else
            {
                return "";
            }
        }

        private PromptOpenFileOptions CreateYdbFileOption()
        {
            return new PromptOpenFileOptions("\n选择要成图的Ydb文件")
            {
                Filter = "Ydb文件 (*.ydb)|*.ydb",
            };
        }
        private PromptOpenFileOptions CreateIfcFileOption()
        {
            return new PromptOpenFileOptions("\n选择要成图的Ifc文件")
            {
                Filter = "Ifc文件 (*.ifc)|*.ifc",
            };
        }
        private PromptOpenFileOptions CreateCacheFileOption()
        {
            return new PromptOpenFileOptions("\n选择要成图的缓存文件")
            {
                Filter = "缓存文件 (*.get)|*.get",
            };
        }
    }
}
