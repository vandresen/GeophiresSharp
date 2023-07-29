/*
Copyright (C) 2017  Torsten Brischalle
email: torsten@brischalle.de
web: http://www.aaabbb.de

ported from:

numerical inverse laplace transform algorithm by
Fernando Damian Nieuwveldt      
email:fdnieuwveldt@gmail.com
web: http://code.activestate.com/recipes/576934-numerical-inversion-of-the-laplace-transform-using/

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

using System.Numerics;
using System;

namespace BlazorGeophiresSharp.Server.Core
{
    public class LaplaceInversionTalbot : ILaplaceInversion
    {
        private int _n;
        private double _shift = 0.0;

        public string Name { get { return "Talbot"; } }

        //-------------------------------------------------------------------------------------------------

        public LaplaceInversionTalbot(int n = 128)
        {
            _n = n;
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

                // Initiate the stepsize
                double h = 2 * Math.PI / _n;

                // Shift contour to the right in case there is a pole on the positive real axis : Note the contour will
                // not be optimal since it was originally devoloped for function with
                // singularities on the negative real axis
                // For example take F(s) = 1/(s-1), it has a pole at s = 1, the contour needs to be shifted with one
                // unit, i.e shift  = 1. But in the test example no shifting is necessary

                Complex ans = new Complex(0, 0);
                int k;

                double c1 = 0.5017;
                double c2 = 0.6407;
                double c3 = 0.6122;
                Complex c4 = new Complex(0, 0.2645);

                // The for loop is evaluating the Laplace inversion at each point theta which is based on the trapezoidal   rule
                for (k = 0; k <= _n; k++)
                {
                    double theta = -Math.PI + (k + 0.5) * h;
                    Complex z = _shift + _n / t * (c1 * theta / Math.Tan(c2 * theta) - c3 + c4 * theta);
                    Complex dz = _n / t * (-c1 * c2 * theta / Sqr(Math.Sin(c2 * theta)) + c1 / Math.Tan(c2 * theta) + c4);
                    ans += Complex.Exp(z * t) * F(z) * dz;
                }

                yArr[idx] = ((h / (2 * Complex.ImaginaryOne * Math.PI)) * ans).Real;
            }

            return yArr;
        }

        //-------------------------------------------------------------------------------------------------

        private double Sqr(double d)
        {
            return d * d;
        }

        //-------------------------------------------------------------------------------------------------
    }
}
