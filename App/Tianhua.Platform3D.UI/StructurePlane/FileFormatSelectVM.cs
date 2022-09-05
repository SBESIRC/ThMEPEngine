using AcHelper;
using System.IO;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;

namespace Tianhua.Platform3D.UI.StructurePlane
{
    public class FileFormatSelectVM
    {
        public string SelectedFileName { get; private set; }
        public FileFormatSelectModel Model { get; set; }
        public FileFormatSelectVM()
        {
            SelectedFileName = "";
            Model = new FileFormatSelectModel();
        }
        public void Save()
        {
            Model.Write();
        }
        public void BrowseFile()
        {
            string defaultFileName = GetDefaultFileName();
            if(string.IsNullOrEmpty(defaultFileName))
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
                SelectedFileName = SelectFile(ops);
            }
            else
            {
                SelectedFileName = defaultFileName;
            }
        }

        private string GetDefaultFileName()
        {
            // 根据当前Dwg的文件名，去找默认的文件
            string dwgPath = GetActiveDwgPath();
            if (File.Exists(dwgPath))
            {
                var fileInfo = new FileInfo(dwgPath);
                var dir = fileInfo.Directory.FullName;
                var fileName = Path.GetFileNameWithoutExtension(dwgPath);
                string fullPath = "";
                switch (Model.FileFormatOption)
                {
                    case FileFormatOps.YDB:
                        fullPath = Path.Combine(dir, fileName + ".ydb");
                        break;
                    case FileFormatOps.IFC:
                        fullPath = Path.Combine(dir, fileName + ".ifc");
                        break;
                    case FileFormatOps.GET:
                        fullPath = Path.Combine(dir, fileName + ".get");
                        break;
                }
                if(File.Exists(fullPath))
                {
                    return fullPath;
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
        }

        private string GetActiveDwgPath()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            return doc.Name;
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
