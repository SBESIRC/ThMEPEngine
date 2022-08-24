using Dreambuild.AutoCAD;
using System.Collections.Generic;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThJumpWireDirectionConfig
    {
        public static Dictionary<int, int> JumpWireDirectionConfig(int startNumber, bool isSingleRow, int loopNumber)
        {
            var result = new Dictionary<int, int>();
            var firstNumbers = new List<int>();
            var secondNumbers = new List<int>();
            if (isSingleRow)
            {
                firstNumbers = CalucalteContinousIndexes(startNumber, loopNumber, 1);
            }
            else
            {
                firstNumbers = CalucalteContinousIndexes(startNumber, loopNumber, 2);
                secondNumbers = CalucalteContinousIndexes(startNumber + 1, loopNumber, 2);
            }
            var firstMark = SetMark(firstNumbers);
            var secondMark = SetMark(secondNumbers);
            result = Add(result, firstMark);
            result = Add(result, secondMark);
            return result;
        }

        private static Dictionary<int, int> Add(Dictionary<int, int> sourceDict, Dictionary<int, int> newDict)
        {
            var result = new Dictionary<int, int>();
            sourceDict.ForEach(o =>
            {
                result.Add(o.Key, o.Value);
            });
            newDict.ForEach(o =>
            {
                if (!result.ContainsKey(o.Key))
                {
                    result.Add(o.Key, o.Value);
                }
            });
            return result;
        }

        private static Dictionary<int, int> SetMark(List<int> numbers)
        {
            var result = new Dictionary<int, int>();
            for (int i = 0; i < numbers.Count; i++)
            {
                if (i == 0)
                {
                    result.Add(numbers[i], 0);
                }
                else
                {
                    result.Add(numbers[i], i % 2 == 1 ? 1 : -1);
                }
            }
            return result;
        }

        private static List<int> CalucalteContinousIndexes(int startInex, int loopNumber, int delta)
        {
            var result = new List<int>();
            for (int i = 0; i < loopNumber; i++)
            {
                result.Add(startInex + delta * i);
            }
            return result;
        }
    }
}
