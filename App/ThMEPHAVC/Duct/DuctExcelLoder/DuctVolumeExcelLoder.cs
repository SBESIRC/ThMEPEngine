using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Publics.BaseCode;

namespace ThMEPHVAC.Duct.DuctExcelLoder
{
    public class DuctVolumeExcelLoder
    {
        public List<DuctVolumeInformation> VolumeInformations { get; private set; }

        public DuctVolumeExcelLoder()
        {
            VolumeInformations = new List<DuctVolumeInformation>();
        }

        public void LoadFromFile(string path)
        {
            VolumeInformations = FuncJson.Deserialize<List<DuctVolumeInformation>>(ReadTxt(path));
        }

        private string ReadTxt(string path)
        {
            try
            {
                using (StreamReader streamReader = File.OpenText(path))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch
            {
                return string.Empty;
            }
        }

    }

}
