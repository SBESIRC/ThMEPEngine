using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Diagram
{
    public static class ThPDSMotorInfoService
    {
        public static List<ThPDSMotorInfo> MotorInfos = new List<ThPDSMotorInfo>
        {
            new ThPDSMotorInfo
            {
                TypeCat_3 = ThPDSLoadTypeCat_3.SmokeExhaustFan,
                OperationMode = "系统中任一排烟阀或排烟口开启时，排烟风机、补风机自动启动",
                FaultProtection = "短路保护，接地故障保护，过载保护只报警不跳闸",
                Signal = "排烟防火阀在280°C时应自行关闭，并应连锁关闭排烟风机和补风机；就地手动启停、火灾报警系统启停、消防控制室手动启停",
            },
            new ThPDSMotorInfo
            {
                TypeCat_3 = ThPDSLoadTypeCat_3.MakeupAirFan,
                OperationMode = "系统中任一排烟阀或排烟口开启时，排烟风机、补风机自动启动",
                FaultProtection = "短路保护，接地故障保护，过载保护只报警不跳闸",
                Signal = "排烟防火阀在280°C时应自行关闭，并应连锁关闭排烟风机和补风机；就地手动启停、火灾报警系统启停、消防控制室手动启停",
            },
            new ThPDSMotorInfo
            {
                TypeCat_3 = ThPDSLoadTypeCat_3.StaircasePressurizationFan,
                OperationMode = "系统中任一常闭加压送风口开启时，加压风机应能自动启动",
                FaultProtection = "短路保护，接地故障保护，过载保护只报警不跳闸",
                Signal = "就地手动启停、火灾报警系统启停、消防控制室手动启停",
            },
            new ThPDSMotorInfo
            {
                TypeCat_3 = ThPDSLoadTypeCat_3.ExhaustFan_Smoke,
                OperationMode = "平时排风，消防时排烟；系统中任一排烟阀或排烟口开启时，排烟风机、补风机自动启动",
                FaultProtection = "短路保护，接地故障保护，作为消防风机时过载保护只报警不跳闸，平时用风机时过载切断主回路",
                Signal = "排烟防火阀在280°C时应自行关闭，并应连锁关闭排烟风机和补风机；就地手动启停、火灾报警系统启停、消防控制室手动启停；排风风机平时由CO浓度探测器信号控制起停，浓度超过限定值则启动",
            },
            new ThPDSMotorInfo
            {
                TypeCat_3 = ThPDSLoadTypeCat_3.SupplyFan_Smoke,
                OperationMode = "平时送风，消防时补风；系统中任一排烟阀或排烟口开启时，补风机自动启动",
                FaultProtection = "短路保护，接地故障保护，作为消防风机时过载保护只报警不跳闸，平时用风机时过载切断主回路",
                Signal = "排烟防火阀在280°C时应自行关闭，并应连锁关闭排烟风机和补风机；就地手动启停、火灾报警系统启停、消防控制室手动启停",
            },
            new ThPDSMotorInfo
            {
                TypeCat_3 = ThPDSLoadTypeCat_3.EmergencyFan,
                OperationMode = "正常工作时现场手动或两地控制，或由BAS自动控制",
                FaultProtection = "短路保护，过载保护",
                Signal = "事故时，由可燃气体（或制冷剂如氟利昂等）报警控制器事故信号自动控制启动，可同时由设于保护区门内、门外的按钮盒手动控制启停",
            },
            new ThPDSMotorInfo
            {
                TypeCat_3 = ThPDSLoadTypeCat_3.PostEmergencyFan,
                OperationMode = "正常工作时现场手动或两地控制，或由BAS自动控制",
                FaultProtection = "短路保护，过载保护",
                Signal = "气体灭火系统动作前，由气体灭火系统切断电源、停止运行，气体灭火动作完成后，手动复位，启动风机，排出有害气体",
            },
            new ThPDSMotorInfo
            {
                TypeCat_3 = ThPDSLoadTypeCat_3.ExhaustFan,
                OperationMode = "现场手动或两地控制，或由BAS自动控制",
                FaultProtection = "短路保护，过载保护",
                Signal = "",
            },
            new ThPDSMotorInfo
            {
                TypeCat_3 = ThPDSLoadTypeCat_3.SupplyFan,
                OperationMode = "现场手动或两地控制，或由BAS自动控制",
                FaultProtection = "短路保护，过载保护",
                Signal = "",
            },
            new ThPDSMotorInfo
            {
                TypeCat_3 = ThPDSLoadTypeCat_3.KitchenExhaustFan,
                OperationMode = "现场手动或两地控制，或由BAS自动控制",
                FaultProtection = "短路保护，过载保护",
                Signal = "与餐饮商铺内补风机连锁启停；与油烟净化器连锁启停",
            },
            new ThPDSMotorInfo
            {
                TypeCat_3 = ThPDSLoadTypeCat_3.DomesticWaterPump,
                OperationMode = "工作泵发生故障时备用泵自动投入",
                FaultProtection = "短路保护，接地故障保护，过载保护，剩余电流动作保护",
                Signal = "就地手动启停、电接点压力表启停",
            },
            new ThPDSMotorInfo
            {
                TypeCat_3 = ThPDSLoadTypeCat_3.SubmersiblePump,
                FireLoad = true,
                OperationMode = "工作泵发生故障时备用泵自动投入，超高水位二台泵同时投入",
                FaultProtection = "短路保护，过载保护只报警不跳闸，漏电动作电流只报警不跳闸",
                Signal = "就地手动启停、液位控制器启停、高溢流水位报警",
            },
            new ThPDSMotorInfo
            {
                TypeCat_3 = ThPDSLoadTypeCat_3.SubmersiblePump,
                FireLoad = false,
                OperationMode = "工作泵发生故障时备用泵自动投入，超高水位二台泵同时投入",
                FaultProtection = "短路保护，接地故障保护，过载保护，剩余电流动作保护",
                Signal = "就地手动启停、液位控制器启停、高溢流水位报警",
            },
        };

        public static ThPDSMotorInfo Select(ThPDSLoadTypeCat_3 typeCat_3, bool fireLoad)
        {
            if(typeCat_3.Equals(ThPDSLoadTypeCat_3.SubmersiblePump))
            {
                return MotorInfos.Where(info => info.TypeCat_3.Equals( typeCat_3)
                    && info.FireLoad.Equals(fireLoad)).FirstOrDefault();
            }
            else
            {
                return MotorInfos.Where(info => info.TypeCat_3.Equals(typeCat_3)).FirstOrDefault();
            }
        }
    }
}
