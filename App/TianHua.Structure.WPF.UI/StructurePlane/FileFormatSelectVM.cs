namespace TianHua.Structure.WPF.UI.StructurePlane
{
    public class FileFormatSelectVM
    {
        private FileFormatSelectModel Model { get; set; }
        public FileFormatSelectVM()
        {
            Model = new FileFormatSelectModel();
        }
        public void Run()
        {
            Model.Write();
        }        
    }
}
