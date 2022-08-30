using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.Platform3D.UI.ViewModels
{
    public class RelatePaperVM
    {
        private List<string> _paperFrameBlockNames;
        private string _frameNameAttributeKey = "内框名称";
        /// <summary>
        /// 装图框名
        /// </summary>
        public ObservableCollection<PaperItem> Items { get; set; }
        public RelatePaperVM()
        {
            _paperFrameBlockNames = new List<string> {
                "THAPE_A0L_inner","THAPE_A0L1_inner",
                "THAPE_A0L2_inner","THAPE_A1L_inner",
                "THAPE_A1L1_inner","THAPE_A1L2_inner",
                "THAPE_A1L4_inner","THAPE_A1L5_inner",
                "THAPE_A2L_inner","THAPE_A2L1_inner",
                "THAPE_A2L2_inner","THAPE_A3L_inner" };
            Items = new ObservableCollection<PaperItem>();

            // 获取Cad中图框
            var paperNames = GetPaperNames();
            paperNames.ForEach(o => Items.Add(new PaperItem(o)));
        }

        public void SetValue(string name,bool isSelected)
        {
            foreach(PaperItem item in Items)
            {
                if(item.Name == name)
                {
                    item.IsSelected = isSelected;
                    break;
                }
            }
        }

        public List<string> GetSelectItemNames()
        {
            return Items.OfType<PaperItem>().Where(o=>o.IsSelected).Select(o=>o.Name).Distinct().ToList();
        }

        private List<string> GetPaperNames()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acadDb = AcadDatabase.Active())
            {
                return acadDb.ModelSpace.OfType<BlockReference>()
                    .Where(o => IsPaperFrameBlk(o.GetEffectiveName()))
                    .Select(o =>
                    {
                        var attributes = o.ObjectId.GetAttributesInBlockReferenceEx();
                        var equalAttributes = attributes.Where(x => x.Key == _frameNameAttributeKey);
                        if (equalAttributes.Count()==1)
                        {
                            return equalAttributes.First().Value;
                        }
                        else
                        {
                            return "";
                        }
                    })
                    .Where(o => !string.IsNullOrEmpty(o))
                    .Distinct()
                    .ToList();
            }
        }

        private bool IsPaperFrameBlk(string blkName)
        {
            return _paperFrameBlockNames.Contains(blkName);
        }
    }
    public class PaperItem
    {
        public string Name { get; set; } = "";
        public bool IsSelected { get; set; }

        public PaperItem(string name)
        {
            this.Name = name;
            this.IsSelected = false;
        }
    }
}
