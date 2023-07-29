using System.Numerics;

namespace BlazorGeophiresSharp.Server.Core
{
    public delegate Complex LaplaceFunction(Complex s);

    public interface ILaplaceInversion
    {
        string Name { get; }

        double[] Calculate(LaplaceFunction F, double[] t);
    }
}
