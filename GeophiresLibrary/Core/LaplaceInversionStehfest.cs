using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Core
{
    public class LaplaceInversionStehfest : ILaplaceInversion
    {
        private readonly int _N = 16;

        private double _ln2;
        private double[] _valArr;

        public string Name { get { return "Stehfest"; } }

        //-------------------------------------------------------------------------------------------------

        public LaplaceInversionStehfest()
        {
            _ln2 = Math.Log(2.0);
            _valArr = new double[_N];

            int n2 = _N / 2;
            double sign = 1;
            int i;

            if ((n2 % 2) != 0)
                sign = -1;

            for (i = 0; i < this._N; i++)
            {
                int kMin = (i + 2) / 2;
                int kMax = i + 1;
                int k;

                if (kMax > n2)
                    kMax = n2;

                _valArr[i] = 0;
                sign = -sign;

                for (k = kMin; k <= kMax; k++)
                {
                    _valArr[i] += (Math.Pow(k, n2) / Factorial(k)) * (Factorial(2 * k)
                                / Factorial(2 * k - i - 1)) / Factorial(n2 - k)
                                / Factorial(k - 1) / Factorial(i + 1 - k);
                }

                _valArr[i] = sign * _valArr[i];
            }
        }

        //-------------------------------------------------------------------------------------------------

        public double[] Calculate(LaplaceFunction F, double[] tArr)
        {
            double[] yArr = new double[tArr.Length];
            int idx;

            for (idx = 0; idx < tArr.Length; idx++)
            {
                double t = tArr[idx];

                if (t <= 0)
                {
                    yArr[idx] = 0;
                    continue;
                }

                double ln2t = this._ln2 / t;
                double x = 0;
                double y = 0;
                int i;

                for (i = 0; i < this._valArr.Length; i++)
                {
                    x += ln2t;
                    y += this._valArr[i] * F(x).Real;
                }

                yArr[idx] = ln2t * y;
            }

            return yArr;
        }

        //-------------------------------------------------------------------------------------------------

        private double Factorial(int n)
        {
            double x = 1;

            if (n > 1)
            {
                int i;

                for (i = 2; i <= n; i++)
                    x *= i;
            }

            return x;
        }

        //-------------------------------------------------------------------------------------------------

    }
}
