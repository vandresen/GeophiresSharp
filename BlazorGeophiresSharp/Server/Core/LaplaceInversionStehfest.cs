/*
Copyright (C) 2017  Torsten Brischalle
email: torsten@brischalle.de
web: http://www.aaabbb.de

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;

namespace BlazorGeophiresSharp.Server.Core
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
