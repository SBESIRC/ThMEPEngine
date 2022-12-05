using System;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using CommunityToolkit.Mvvm.Input;
using ThControlLibraryWPF.ControlUtils;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Electrical;
using ThMEPElectrical.DCL.Service;

namespace TianHua.Electrical.UI.LightningProtectLeadWire
{
    public class ThLightningProtectLeadWireVM : NotifyPropertyChangedBase
    {
        private string _id = "";
        public string Id => _id;
        private List<ThEStoreys> _estoreys = new List<ThEStoreys>();
        public ObservableCollection<string> EStoreyDisplays { get; set; } //与_estoreys是一一对应的
        public ObservableCollection<string> LightningProtectionGrades { get; set; }
        private string _lightningProtectionGrade = "";
        private Dictionary<string,int> _levelGradeMap = new Dictionary<string,int>();
        public string LightningProtectionGrade
        {
            get
            {
                return _lightningProtectionGrade;
            }
            set
            {
                _lightningProtectionGrade = value;
                RaisePropertyChanged("LightningProtectionGrade");
            }
        }

        public ThLightningProtectLeadWireVM()
        {
            _levelGradeMap = new Dictionary<string, int>();
            _levelGradeMap.Add("一类", 1);
            _levelGradeMap.Add("二类", 2);
            _levelGradeMap.Add("三类", 3);
            _id = Guid.NewGuid().ToString();
            _lightningProtectionGrade = "三类";
            _estoreys = new List<ThEStoreys>();
            EStoreyDisplays = new ObservableCollection<string>();
            LightningProtectionGrades = new ObservableCollection<string> (_levelGradeMap.Keys);
        }

        public ICommand LayOutCmd
        {
            get
            {
                return new RelayCommand(Layout);
            }
        }

        public ICommand InsertStoreyCmd
        {
            get
            {
                return new RelayCommand(InsertStorey);
            }
        }

        public ICommand ReadStoreyCmd
        {
            get
            {
                return new RelayCommand(ReadStorey);
            }
        }

        private void Layout()
        {
            int levelIndex = -1;
            if(_levelGradeMap.ContainsKey(_lightningProtectionGrade))
            {
                levelIndex = _levelGradeMap[_lightningProtectionGrade];
            }
            if(levelIndex!=-1 && _estoreys.Count>0)
            {
                using (var lockDoc = Active.Document.LockDocument())
                using (var builder = new ThLightningProtectLeadWireBuilder(_estoreys, levelIndex))
                {
                    builder.Build();
                }
            }
        }

        private void InsertStorey()
        {
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THLCDY");
        }

        private void ReadStorey()
        {
            using (var lockDoc = Active.Document.LockDocument())
            using (var acadDb = AcadDatabase.Active())
            {
                var mode = GetSelectStoreyFrameMode();
                if (mode == "S")
                {
                    this._estoreys = SelectEStoreys(acadDb);
                    this._estoreys = this._estoreys.Where(o => !string.IsNullOrEmpty(o.StoreyTypeString)).ToList();
                    this._estoreys = SortStoreys(this._estoreys);
                    UpdateEStoreyDisplayValues();
                }
                else if (mode == "A")
                {
                    this._estoreys = RecognizeEStoreys(acadDb.Database, new Point3dCollection());
                    this._estoreys = this._estoreys.Where(o => !string.IsNullOrEmpty(o.StoreyTypeString)).ToList();
                    this._estoreys = SortStoreys(this._estoreys);
                    UpdateEStoreyDisplayValues();
                }
                else
                {
                    return;
                }
            }
        }

        private void UpdateEStoreyDisplayValues()
        {
            this.EStoreyDisplays.Clear();
            this._estoreys.ForEach(o => this.EStoreyDisplays.Add(o.StoreyNumber + o.StoreyTypeString));
        }

        private List<ThEStoreys> SortStoreys(List<ThEStoreys> estoreys)
        {
            // 楼层从高到底排序
            // 小屋面，大屋面,
            var results = new List<ThEStoreys>();
            var temps = estoreys.OfType<ThEStoreys>().ToList();
            results.AddRange(FindEStoreys(temps, "小屋面")); //把小屋面置顶
            results.AddRange(FindEStoreys(temps, "大屋面")); //把大屋面置于小屋面之后           
            temps = temps.Except(results).ToList();// 把大屋面，小屋面去掉
            temps = temps.OrderByDescending(o => GetStoreyStartNumber(o.StoreyNumber)).ToList();
            results.AddRange(temps);
            return results; 
        }

        private double GetStoreyStartNumber(string storeyNumber)
        {
            // B1F->-1,B2F->-2
            // 如果B1FM->-1.5 , 1FM->1.5,如果后缀有M，要加0.5
            var newStoreyNumber = storeyNumber.ToUpper().Trim();
            string parrern1 = @"^\d+";
            string parrern2 = @"^B\s*\d+\s*F";
            if (Regex.IsMatch(newStoreyNumber, parrern1))
            {               
                var match = Regex.Match(newStoreyNumber, parrern1);
                if(newStoreyNumber.IndexOf('M')>=0)
                {
                    return int.Parse(match.Value)+0.5;
                }
                else
                {
                    return int.Parse(match.Value);
                }
            }
            else if (Regex.IsMatch(newStoreyNumber, parrern2))
            {
                string number = newStoreyNumber.Substring(1, newStoreyNumber.IndexOf('F') - 1).Trim();
                if(newStoreyNumber.IndexOf('M') >= 0)
                {
                    return -1 * (int.Parse(number)+0.5);
                }
                else
                {
                    return -1 * int.Parse(number);
                }
            }
            else
            {
                return -1;
            }
        }


        private List<ThEStoreys> FindEStoreys(List<ThEStoreys> storeys, string storeyType)
        {
            var results = new List<ThEStoreys>();
            foreach (ThEStoreys estorey in storeys)
            {
                if (estorey.StoreyTypeString == storeyType)
                {
                    results.Add(estorey);
                }
            }
            return results;
        }

        private List<ThEStoreys> SelectEStoreys(AcadDatabase acadDb)
        {
            var eStoreys = new List<ThEStoreys>();
            var options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "请选择楼层框",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
               RXClass.GetClass(typeof(BlockReference)).DxfName,
            };
            var layerNames = new string[0];
            var filter = ThSelectionFilterTool.Build(dxfNames, layerNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status == PromptStatus.OK)
            {
                result.Value.GetObjectIds().OfType<ObjectId>().ForEach(o =>
                {
                    var br = acadDb.Element<BlockReference>(o);
                    if (IsEStoreyBlockReference(br))
                    {
                        eStoreys.Add(new ThEStoreys(o));
                    }
                });
            }
            return eStoreys;
        }

        private List<ThEStoreys> RecognizeEStoreys(Database database,Point3dCollection pts)
        {
            var engine = new ThEStoreysRecognitionEngine();
            engine.Recognize(database, new Point3dCollection());
            return engine.Elements.OfType<ThEStoreys>().ToList();
        }

        private bool IsEStoreyBlockReference(BlockReference br)
        {
            return !br.BlockTableRecord.IsNull && br.GetEffectiveName() == "AI-楼层框定E";
        }

        private string GetSelectStoreyFrameMode()
        {
            var options = new PromptKeywordOptions("\n请选择楼层框");
            options.Keywords.Add("A", "A", "整张图纸读取(A)");
            options.Keywords.Add("S", "S", "手选楼层框(S)");
            options.Keywords.Default = "S";
            var result = Active.Editor.GetKeywords(options);
            if (result.Status == PromptStatus.OK)
            {
                return result.StringResult;
            }
            else
            {
                return "";
            }
        }

        private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
