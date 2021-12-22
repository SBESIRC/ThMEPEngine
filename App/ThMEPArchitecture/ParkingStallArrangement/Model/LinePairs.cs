using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public class LinePairs : IEquatable<LinePairs>
    {
        public int Num1 { get; set; }
        public int Num2 { get; set; }
        public LinePairs(int num1, int num2)
        {
            Num1 = num1;
            Num2 = num2;
        }
        public override int GetHashCode()
        {
            return Num1.GetHashCode() ^ Num2.GetHashCode();
        }
        public bool Equals(LinePairs other)
        {
            return (this.Num1 == other.Num1 && this.Num2 == other.Num2)
                || (this.Num1 == other.Num2 && this.Num2 == other.Num1);
        }
    }
}
