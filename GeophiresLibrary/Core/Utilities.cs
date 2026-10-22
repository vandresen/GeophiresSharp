using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Core
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

        public static double[] ArrayDensityWater(double[] Twater)
        {
            var T = Twater.Select(x => x + 273.15).ToArray();
            var tempArray = T.Select(x => 2.9104E-06 * x).ToArray();
            tempArray = tempArray.Select(x => 0.00150896 - x).ToArray();
            tempArray = tempArray.Zip(T, (x, y) => x * y).ToArray();
            var rhowater = tempArray.Select(x => (0.7983223 + x) * 1000.0).ToArray();
            return rhowater;
        }

        public static double Erf(double x)
        {
            // constants
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return sign * y;
        }

        public static double[] ArrayViscosityWater(double[] Twater)
        {
            double[] power = Twater.Select(temp => Math.Pow(10, 247.8 / (temp + 273.15 - 140))).ToArray();
            var muwater = power.Select(x => 2.414E-5 * x).ToArray();
            return muwater;
        }

        public static double ViscosityWater(double Twater)
        {
            double power = Math.Pow(10, 247.8 / (Twater + 273.15 - 140));
            double muwater = 2.414E-5 * power;
            return muwater;
        }

        public static double VaporPressureWater(double Twater)
        {
            double A, B, C;
            if (Twater < 100)
            {
                A = 8.07131;
                B = 1730.63;
                C = 233.426;
            }
            else
            {
                A = 8.14019;
                B = 1810.94;
                C = 244.485;
            }
            // water vapor pressure in kPa using Antione Equation\n";
            double power = A - B / (C + Twater);
            double result = 133.322 * Math.Pow(10, power) / 1000.0;
            return result;
        }

        public static double Average(double[] numbers)
        {
            double sum = 0;
            for (int i = 0; i < numbers.Length; i++)
            {
                sum += numbers[i];
            }
            double avg = sum / numbers.Length;
            return avg;
        }

        public static double[] linspace(double StartValue, double EndValue, int numberofpoints)
        {
            double[] parameterVals = new double[numberofpoints];
            double increment = Math.Abs(StartValue - EndValue) / Convert.ToDouble(numberofpoints - 1);
            int j = 0; //will keep a track of the numbers 
            double nextValue = StartValue;
            for (int i = 0; i < numberofpoints; i++)
            {
                parameterVals.SetValue(nextValue, j);
                j++;
                if (j > numberofpoints)
                {
                    throw new IndexOutOfRangeException();
                }
                nextValue = nextValue + increment;
            }
            return parameterVals;
        }

        public static T[] SliceArray<T>(T[] array, int startIndex)
        {
            if (startIndex >= array.Length)
                return new T[0]; // If the startIndex is greater than or equal to the array length, return an empty array

            return array.Skip(startIndex).ToArray();
        }

        public static double[] SqrtArray(double[] array)
        {
            return array.Select(x => Math.Sqrt(x)).ToArray();
        }

        public static double TrapezoidalIntegration(double[] y, float dx)
        {
            double integral = 0.0;

            for (int i = 0; i < y.Length - 1; i++)
            {
                integral += (y[i] + y[i + 1]) * dx / 2.0;
            }

            return integral;
        }

        public static class WellCostCalculator
        {
            private const double MinValidDepthM = 500.0;
            private const double MaxValidDepthM = 7000.0;
            private const int SimpleCorrelation = 5;

            // GEOPHIRES-X correlations: cost (M$) = (c2*d^2 + c1*d + c0) * 1E-6, d in meters
            private static readonly Dictionary<int, (double c2, double c1, double c0)> Correlations = new()
            {
                // 2025 NREL cost curve update
                [1] = (0.258496, 357.967, 738531.58),   // vertical small, baseline
                [2] = (0.240624, 646.1621, 503625.06),   // deviated small, baseline
                [3] = (0.248458, 935.8985, 626586.68),   // vertical large, baseline
                [4] = (0.217333, 1362.93, 301066.16),   // deviated large, baseline
                                                        // GeoVision variants
                [6] = (0.13710, 129.61033, 1205587.57100),
                [7] = (0.00804, 455.60507, 921007.68680),
                [8] = (0.15340, 120.31700, 1431801.54400),
                [9] = (0.00854, 506.08357, 1057330.39000),
                [10] = (0.18927, 293.45174, 1326526.31300),
                [11] = (0.00315, 782.69676, 983620.25270),
                [12] = (0.19950, 296.13011, 1697867.70900),
                [13] = (0.00380, 838.90249, 1181947.04400),
                [14] = (0.00252, 439.44503, 590611.90110),
                [15] = (0.00719, 455.85233, 753377.73080),
                [16] = (-0.00240, 752.93946, 524337.65380),
                [17] = (0.00376, 762.52696, 765103.07690),
            };

            /// <summary>Cost of one vertical well in M$, including the adjustment factor.</summary>
            public static double CostOfOneWell(double depthM, int correlation,
                                               double perMeterCost, double adjustmentFactor)
            {
                if (depthM < MinValidDepthM && correlation != SimpleCorrelation)
                {
                    Console.WriteLine($"Warning: correlation {correlation} invalid for depth < {MinValidDepthM} m " +
                                      $"({depthM} m). Falling back to per-meter cost ({perMeterCost} $/m).");
                    correlation = SimpleCorrelation;
                }
                if (depthM > MaxValidDepthM && correlation != SimpleCorrelation)
                {
                    Console.WriteLine($"Warning: correlation {correlation} may be invalid for depth > {MaxValidDepthM} m ({depthM} m).");
                }

                double cost;
                if (correlation == SimpleCorrelation)
                {
                    cost = perMeterCost * depthM * 1E-6;
                }
                else if (Correlations.TryGetValue(correlation, out var c))
                {
                    cost = (c.c2 * depthM * depthM + c.c1 * depthM + c.c0) * 1E-6;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(correlation), correlation,
                        "Unknown well drilling cost correlation (valid: 1-17).");
                }

                return adjustmentFactor * cost;
            }
        }
    }
}
