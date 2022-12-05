using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThPlatform3D.Model
{
    public class ThProjectFile
    {
        private string _projectFileId = "";
        public string ProjectFileId 
        {
            get => _projectFileId;
            set => _projectFileId=value;
        }
        private string _prjId = "";
        public string PrjId
        {
            get => _prjId;
            set => _prjId = value;
        }
        private string _subPrjId = "";
        public string SubPrjId
        {
            get => _subPrjId;
            set => _subPrjId = value;
        }
        private string _fileName = "";
        public string FileName
        {
            get => _fileName;
            set => _fileName = value;
        }

        private string _majorName = "";
        public string MajorName
        {
            get => _majorName;
            set => _majorName = value;
        }

        private bool _isDel;
        public bool IsDel
        { 
            get => _isDel;
            set => _isDel =value;
        }
    }
}
