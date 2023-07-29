using System;

namespace GeophiresSharp.Core
{
    public class Utilities
    {
        public static double heatcapacitywater(double Twater)
        {
            Twater = (Twater + 273.15) / 1000;
            var A = -203.606;
            var B = 1523.29;
            var C = -3196.413;
            var D = 2474.455;
            var E = 3.855326;
            var cpwater = (A + B * Twater + C * Math.Pow(Twater, 2) + D * Math.Pow(Twater, 3) + E / Math.Pow(Twater, 2)) / 18.02 * 1000;
            return cpwater;
        }

        public static double densitywater(double Twater)
        {
            var T = Twater + 273.15;
            var rhowater = (0.7983223 + (0.00150896 - 2.9104E-06 * T) * T) * 1000.0;
            return rhowater;
        }
    }
}
