using System.IO;
using System.Text;

namespace ThMEPHVAC.IO
{
    public abstract class ThDuctJsonReader
    {
        protected string ReadWord(string _Path)
        {
            try
            {
                using (StreamReader _StreamReader = new StreamReader(_Path, Encoding.UTF8))
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
