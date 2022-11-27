using ThCADExtension;
using ThMEPWSS.UndergroundFireHydrantSystem;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public class Dics
    {
        public static void CreateLeadLineDic(ref SprayIn sprayIn)
        {
            double tolerance = 50;
            for (int i = 0; i < sprayIn.LeadLines.Count - 1; i++)
            {
                var l1 = sprayIn.LeadLines[i];

                for (int j = i + 1; j < sprayIn.LeadLines.Count; j++)
                {
                    var l2 = sprayIn.LeadLines[j];

                    if (l1.GetLinesDist(l2) < tolerance)
                    {
                        sprayIn.LeadLineDic.AddItem(l1, l2);
                        sprayIn.LeadLineDic.AddItem(l2, l1);
                        continue;
                    }
                    if (l1.GetLineDist2(l2) < tolerance)
                    {
                        sprayIn.LeadLineDic.AddItem(l1, l2);
                        sprayIn.LeadLineDic.AddItem(l2, l1);
                        continue;
                    }
                    if(l2.GetLineDist2(l1) < tolerance)
                    {
                        sprayIn.LeadLineDic.AddItem(l2, l1);
                        sprayIn.LeadLineDic.AddItem(l1, l2);
                        continue;
                    }
                    if (l1.LineIsIntersection(l2))
                    {
                        if (l1.GetLineDist2(l2) < tolerance || l2.GetLineDist2(l1) < tolerance)
                        {
                            sprayIn.LeadLineDic.AddItem(l2, l1);
                            sprayIn.LeadLineDic.AddItem(l1, l2);
                            continue;
                        }
                    }
                }
            }
        }
    }
}
