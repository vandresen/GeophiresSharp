using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Core
{
    public delegate Complex LaplaceFunction(Complex s);

    public interface ILaplaceInversion
    {
        string Name { get; }

        double[] Calculate(LaplaceFunction F, double[] t);
    }
}
