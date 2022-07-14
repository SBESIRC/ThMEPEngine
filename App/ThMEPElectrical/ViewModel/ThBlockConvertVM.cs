﻿using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using AcHelper;

using ThCADExtension;
using ThMEPElectrical.Model;
using ThMEPElectrical.Command;
using ThMEPElectrical.BlockConvert;

namespace ThMEPElectrical.ViewModel
{
    public class ThBlockConvertVM
    {
        /// <summary>
        /// 配置参数
        /// </summary>
        public ThBlockConvertModel Parameter { get; set; }

        /// <summary>
        /// 表格显示数据
        /// </summary>
        public ObservableCollection<BlockConvertInfo> BlockConvertInfos { get; set; }

        /// <summary>
        /// 有序的比对结果
        /// </summary>
        public List<ThBConvertCompareModel> CompareModels { get; set; }

        public List<ThBConvertEntityInfos> TarEntityInfos { get; set; }

        public ThBlockConvertVM()
        {
            Parameter = new ThBlockConvertModel();
            BlockConvertInfos = new ObservableCollection<BlockConvertInfo>();
            CompareModels = new List<ThBConvertCompareModel>();
            TarEntityInfos = new List<ThBConvertEntityInfos>();

            // blank
            for (var i = 0; i <= 15; i++)
            {
                BlockConvertInfos.Add(new BlockConvertInfo());
            }
        }

        /// <summary>
        /// 提资转换
        /// </summary>
        public void BlockConvert()
        {
            // 执行命令
            var cmd = CreateBConvertCommand();
            cmd.Command = BConvertCommand.BlockConvert;
            cmd.Execute();
        }

        /// <summary>
        /// 更新比对
        /// </summary>
        public void UpdateCompare()
        {
            // 执行命令
            var cmd = CreateBConvertCommand();
            FocusToCAD();
            cmd.Command = BConvertCommand.BlockCompare;
            cmd.Execute();
            CompareModels = cmd.CompareModels.OrderBy(o => o.Category).ThenBy(o => o.EquimentType).ThenBy(o => o.Type).ToList();
            TarEntityInfos = cmd.TarEntityInfos;

            var i = 0;
            for (var j = 0; j < CompareModels.Count; j++)
            {
                if (CompareModels[j].Type.Equals(ThBConvertCompareType.Unchanged))
                {
                    continue;
                }

                if (i <= 15)
                {
                    BlockConvertInfos[i].Guid = CompareModels[j].Guid;
                    BlockConvertInfos[i].Category = CompareModels[j].Category.ToString();
                    BlockConvertInfos[i].EquipmentType = CompareModels[j].EquimentType;
                    BlockConvertInfos[i].CompareResult = CompareModels[j].Type.GetDescription();
                    i++;
                }
                else
                {
                    BlockConvertInfos.Add(new BlockConvertInfo(CompareModels[j].Guid, CompareModels[j].Category.ToString(),
                        CompareModels[j].EquimentType, CompareModels[j].Type.GetDescription()));
                }
            }
        }

        /// <summary>
        /// 提资更新
        /// </summary>
        public void BlockUpdate()
        {
            var service = new ThBConvertCompareUpdateService(CompareModels, TarEntityInfos);
            service.Update(Parameter.BlkScaleValue / 100.0);

            CompareModels.Clear();
            BlockConvertInfos.Clear();
            FillBlank();
        }

        /// <summary>
        /// 忽略变化
        /// </summary>
        public void IgnoreChange(List<string> ids)
        {
            ids.ForEach(id =>
            {
                if (string.IsNullOrEmpty(id))
                {
                    return;
                }
                var info = BlockConvertInfos.Where(o => o.Guid.Equals(id)).First();
                BlockConvertInfos.Remove(info);

                var model = CompareModels.Where(o => o.Guid.Equals(id)).First();
                CompareModels.Remove(model);
            });

            FillBlank();
        }

        /// <summary>
        /// 局部更新
        /// </summary>
        public void LocalUpdate(List<string> ids)
        {
            var localModels = CompareModels.Where(o => ids.Contains(o.Guid)).ToList();
            var service = new ThBConvertCompareUpdateService(localModels, TarEntityInfos);
            service.Update(Parameter.BlkScaleValue / 100.0);

            // 删除更新完成项
            IgnoreChange(ids);
        }

        /// <summary>
        /// Zoom
        /// </summary>
        public void Zoom(BlockConvertInfo info)
        {
            var zoomService = new ThBConvertZoomService();
            zoomService.Zoom(CompareModels.Where(o => o.Guid.Equals(info.Guid)).First(), Parameter.BlkScaleValue / 100.0);
        }

        private void FillBlank()
        {
            var count = BlockConvertInfos.Count();
            for (var i = 0; i <= 15 - count; i++)
            {
                BlockConvertInfos.Add(new BlockConvertInfo());
            }
        }

        private ThBConvertCommand CreateBConvertCommand()
        {
            //
            var cmd = new ThBConvertCommand()
            {
                Scale = Parameter.BlkScaleValue,
                FrameStyle = Parameter.BlkFrameValue,
                ConvertManualActuator = Parameter.ManualActuatorOps,
            };
            if (Parameter.HavcOps && Parameter.WssOps)
            {
                cmd.Category = ConvertCategory.ALL;
            }
            else if (Parameter.HavcOps)
            {
                cmd.Category = ConvertCategory.HVAC;
            }
            else if (Parameter.WssOps)
            {
                cmd.Category = ConvertCategory.WSS;
            }
            else
            {
                return cmd;
            }
            switch (Parameter.EquipOps)
            {
                case CapitalOP.Strong:
                    cmd.Mode = ConvertMode.STRONGCURRENT;
                    break;
                case CapitalOP.Weak:
                    cmd.Mode = ConvertMode.WEAKCURRENT;
                    break;
                case CapitalOP.All:
                    cmd.Mode = ConvertMode.ALL;
                    break;
            }
            return cmd;
        }

        private static void FocusToCAD()
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