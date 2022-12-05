using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NFox.Cad;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPTCH.Services;
using ThPlatform3D.Command;
using ThPlatform3D.Model;
using Tianhua.Platform3D.UI.UI;
using acadApp = Autodesk.AutoCAD.ApplicationServices;
using ThPlatform3D.Service;
using Autodesk.AutoCAD.EditorInput;
using ThPlatform3D.Common;

namespace Tianhua.Platform3D.UI.ViewModels
{
    public class ViewDetailVM : INotifyPropertyChanged
    {        
        private string _id = "";
        public string Id
        {
            get => _id;
        }
        private ThViewDetailInfo _viewDetailInfo;
        public ThViewDetailInfo ViewDetailInfo => _viewDetailInfo;

        private Dictionary<string, List<ThEditStoreyInfo>> _storeyInfoMap;

        private ObservableCollection<string> _buildingNos;
        /// <summary>
        /// 单体子项:1#,2#,3#...
        /// </summary>
        public ObservableCollection<string> BuildingNos 
        {
            get => _buildingNos;
            set
            {
                _buildingNos = value;
                RaisePropertyChanged("BuildingNos");
            }
        }
        private ObservableCollection<string> _floors;
        /// <summary>
        /// 标高:1F,2F,3F...
        /// </summary>
        public ObservableCollection<string> Floors 
        {
            get=> _floors; 
            set
            {
                _floors = value;
                RaisePropertyChanged("Floors");
            }
        }
        private ObservableCollection<string> _viewTypes { get; set; }
        /// <summary>
        /// 视图类型：平面图,立面图,剖面图...
        /// </summary>
        public ObservableCollection<string> ViewTypes
        {
            get => _viewTypes;
            set
            {
                _viewTypes = value;
                RaisePropertyChanged("ViewTypes");
            }
        }
        private ObservableCollection<string> _viewScales;
        /// <summary>
        /// 视图比例：1:20,1:50...
        /// </summary>
        public ObservableCollection<string> ViewScales
        {
            get => _viewScales;
            set
            {
                _viewScales = value;
                RaisePropertyChanged("ViewScales");
            }
        }
        private ObservableCollection<string> _viewTemplates;
        /// <summary>
        /// 视图模板：天华模板...
        /// </summary>
        public ObservableCollection<string> ViewTemplates
        {
            get => _viewTemplates;
            set
            {
                _viewTemplates = value;
                RaisePropertyChanged("ViewTemplates");
            }
        }

        private ObservableCollection<string> _sectionFrames;
        /// <summary>
        /// 剖面框
        /// </summary>
        public ObservableCollection<string> SectionFrames
        {
            get => _sectionFrames;
            set
            {
                _sectionFrames = value;
                RaisePropertyChanged("SectionFrames");
            }
        }
        public ViewDetailVM()
        {
            _id = Guid.NewGuid().ToString();
            _storeyInfoMap = new Dictionary<string, List<ThEditStoreyInfo>>();
            _viewDetailInfo = new ThViewDetailInfo();
            _viewDetailInfo.ViewType = "平面图";
            _sectionFrames = new ObservableCollection<string>();
            _buildingNos = new ObservableCollection<string>(); 
            _viewTemplates = new ObservableCollection<string> { "天华默认" };
            _viewTypes = new ObservableCollection<string> { "平面图", "剖面图", "立面图" };
            _viewScales = new ObservableCollection<string> { "1:20", "1:50","1:100", "1:150","1:200","1:500"};
            if(string.IsNullOrEmpty(_viewDetailInfo.ViewTemplate))
            {
                _viewDetailInfo.ViewTemplate = "天华默认";
            }
            if(string.IsNullOrEmpty(_viewDetailInfo.ViewScale))
            {
                _viewDetailInfo.ViewScale = "1:20";
            }
            if (string.IsNullOrEmpty(_viewDetailInfo.ViewType))
            {
                _viewDetailInfo.ViewType = "平面图";
            }
        }

        public ViewDetailVM(ThViewDetailInfo info):this()
        {
            _viewDetailInfo = info;
            _sectionFrames = info.SectionFrames;
            if(string.IsNullOrEmpty(_viewDetailInfo.ViewType))
            {
                _viewDetailInfo.ViewType = "平面图";
            }
        }
        public void SetStoreyInfoMap(Dictionary<string, List<ThEditStoreyInfo>> storeyInfoMap)
        {
            _storeyInfoMap = storeyInfoMap;
            BuildingNos = new ObservableCollection<string>(storeyInfoMap.Keys);
            if(BuildingNos.Count>0)
            {
                if(string.IsNullOrEmpty(_viewDetailInfo.BuildingNo)|| 
                    !BuildingNos.Contains(_viewDetailInfo.BuildingNo))
                {
                    _viewDetailInfo.BuildingNo = BuildingNos[0];
                }
            }
            else
            {
                _viewDetailInfo.BuildingNo = "";
            }
        }

        public void UpdateFloors()
        {
            var floors = GetFloors(_viewDetailInfo.BuildingNo);
            Floors = new ObservableCollection<string>(floors);
            if (Floors.Count > 0)
            {
                if (string.IsNullOrEmpty(_viewDetailInfo.Floor))
                {
                    _viewDetailInfo.Floor = Floors[0];
                }                
            }
        }

        public void UpdateViewName()
        {
            if(_viewDetailInfo.ViewType=="平面图")
            {
                var floor = _viewDetailInfo.Floor.ToUpper();
                if (!string.IsNullOrEmpty(_viewDetailInfo.Floor))
                {
                    if(!floor.EndsWith("F"))
                    {
                        floor = floor + "F";
                    }
                }
                var scales = _viewDetailInfo.ViewScale.Split(':');
                string scale = "";
                if(scales.Length>0)
                {
                    scale = scales.Last();
                }
                _viewDetailInfo.ViewName = _viewDetailInfo.BuildingNo+"_"+
                    floor + _viewDetailInfo.ViewType+"_"+ scale;
            }
            else if(_viewDetailInfo.ViewType == "平面图")
            {
                _viewDetailInfo.ViewName = _viewDetailInfo.BuildingNo + "_" + _viewDetailInfo.ViewType;
            }
            else if(_viewDetailInfo.ViewType == "剖面图")
            {
                _viewDetailInfo.ViewName = _viewDetailInfo.BuildingNo + "_" + _viewDetailInfo.ViewType;
            }
        }

        public bool ValideData()
        {
            //TODO
            return true;
        }

        public void CreateSection()
        {
            if(acadApp.Application.DocumentManager.MdiActiveDocument == null)
            {
                return;
            }
            //用户绘制剖切线
            using (var docLock = Active.Document.LockDocument())
            {
                Active.Document.Window.Focus();
                var drawLines = new DBObjectCollection();
                using (var cmd = new ThDrawLineCmd())
                {
                    cmd.Execute();
                    drawLines = cmd.Lines.Clone();
                    cmd.Lines.MDispose();
                }
                if(drawLines.Count==0)
                {
                    return;
                }
                else
                {                    
                    var lastLine = drawLines.OfType<Line>().Last();
                    if(lastLine.Length<1.0)
                    {
                        drawLines.MDispose();
                        return;
                    }
                    else
                    {
                        var vdName = _viewDetailInfo.ViewSectionDirection.GetViewDirectionName();
                        var pqkInputVM = new PQKInputVM(_viewDetailInfo.SectionFrames.ToList(), vdName);
                        var sectionFrameUI = new PQKInputUI(pqkInputVM);
                        var digRes = acadApp.Application.ShowModalWindow(sectionFrameUI);
                        if(pqkInputVM.IsValid)
                        {
                            _viewDetailInfo.SectionFrames.Add(pqkInputVM.Name);
                            _viewDetailInfo.SectionFrame = pqkInputVM.Name;

                            // 创建
                            var parameter = new ThPQKParameter
                            {
                                Start = lastLine.StartPoint,
                                End = lastLine.EndPoint,
                                Mark = pqkInputVM.Name,
                                Depth = pqkInputVM.Depth,
                                Direction = _viewDetailInfo.ViewSectionDirection,
                            };
                            var creator = new ThPQKCreator();
                            var blkId = creator.Create(Active.Database, parameter);      
                            if(!blkId.IsNull)
                            {
                                using (var acadDb = AcadDatabase.Active())
                                {
                                    var tvs = ThPQKXDataService.Create(new List<string> { parameter.Mark });
                                    ThPQKXDataService.AddXData(blkId, tvs);
                                }
                            }
                            drawLines.MDispose();
                        }
                        else
                        {
                            drawLines.MDispose();
                            return;
                        }
                    }
                }
            }
        }

        public void QuickLocate()
        {
            if (acadApp.Application.DocumentManager.MdiActiveDocument == null)
            {
                return;
            }
            if (!string.IsNullOrEmpty(_viewDetailInfo.SectionFrame))
            {
                using (var docLock = Active.Document.LockDocument())
                using (var acadDb = AcadDatabase.Active())
                {
                    Active.Document.Window.Focus();
                    var blks = acadDb.ModelSpace
                        .OfType<BlockReference>()
                        .Where(o =>
                        {
                            var tvs = ThPQKXDataService.GetXData(o.ObjectId);
                            if(tvs==null || tvs.Count==0)
                            {
                                return false;
                            }
                            else
                            {
                                return tvs.Where(tv => tv.Value.ToString() == _viewDetailInfo.SectionFrame).Any();
                            }                            
                        }).ToCollection();
                    if(blks.Count>0)
                    {
                        Active.Editor.SetImpliedSelection(new ObjectId[0]);
                        Active.Editor.SetImpliedSelection(blks.OfType<BlockReference>().Select(o => o.ObjectId).ToArray());
                        var first = blks.OfType<BlockReference>().First();
                        if(first.Bounds.HasValue)
                        {
                            var centerPt = first.GeometricExtents.MinPoint.GetMidPt(first.GeometricExtents.MaxPoint);
                            Active.Editor.Zoom(first.GeometricExtents.MinPoint, first.GeometricExtents.MaxPoint, centerPt, 1.0);
                        }                        
                    }
                }
            }           
        }

        public void SelectFloorSection()
        {
            if (acadApp.Application.DocumentManager.MdiActiveDocument == null)
            {
                return;
            }
            using (var docLock = Active.Document.LockDocument())
            using (var acadDb = AcadDatabase.Active())
            {
                Active.Document.Window.Focus();
                var ppo = new PromptEntityOptions("\n请选择对齐标注")
                {
                    AllowNone = true,
                };
                var per = Active.Editor.GetEntity(ppo);
                if(per.Status==PromptStatus.OK)
                {
                    var entity = acadDb.Element<Entity>(per.ObjectId);
                    if(entity is AlignedDimension ad)
                    {
                        _viewDetailInfo.SectionDistance = Math.Round(ad.Measurement,0);
                    }
                }
            }
        }

        private List<string> GetFloors(string buildingNo)
        {
            if(this._storeyInfoMap.ContainsKey(buildingNo))
            {
                return this._storeyInfoMap[buildingNo].Select(o => o.FloorNo).ToList();
            }
            else
            {
                return new List<string>();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged != null)
            {
                propertyChanged.Invoke((object)this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
