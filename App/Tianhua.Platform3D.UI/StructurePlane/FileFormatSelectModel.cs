using ThControlLibraryWPF.ControlUtils;
using ThPlatform3D.StructPlane;

namespace Tianhua.Platform3D.UI.StructurePlane
{
    public class FileFormatSelectModel : NotifyPropertyChangedBase
    {
        private FileFormatOps fileFormatOption;
        /// <summary>
        /// 文件格式
        /// </summary>
        public FileFormatOps FileFormatOption
        {
            get => fileFormatOption;
            set
            {
                fileFormatOption = value;
                RaisePropertyChanged("FileFormatOption");
            }
        }
        public FileFormatSelectModel()
        {
            Load();
        }
        private void Load()
        {
            fileFormatOption = Convert(ThDrawingParameterConfig.Instance.FileFormatOption);
        }
        public void Write()
        {
            ThDrawingParameterConfig.Instance.FileFormatOption = fileFormatOption.ToString();
        }
        private FileFormatOps Convert(string fileFormatOption)
        {
            var result = FileFormatOps.IFC;
            switch (fileFormatOption)
            {
                case "YDB":
                    result = FileFormatOps.YDB;
                    break;
                case "IFC":
                    result = FileFormatOps.IFC;
                    break;
                case "GET":
                    result = FileFormatOps.GET;
                    break;
            }
            return result;
        }
    }
    /// <summary>
    /// 文件格式
    /// </summary>
    public enum FileFormatOps
    {
        YDB = 0,
        IFC = 1,
        GET = 2,
    }
}
