using System.IO;

namespace TianHua.FanSelection.Function
{
    public abstract class ThModelLoader
    {
        public abstract void LoadFromFile(string path);

        protected string ReadTxt(string _Path)
        {
            try
            {
                using (StreamReader _StreamReader = File.OpenText(_Path))
                {
                    return _StreamReader.ReadToEnd();
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
