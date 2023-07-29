using BlazorGeophiresSharp.Server.Models;
using System.Threading.Tasks;

namespace BlazorGeophiresSharp.Server.Repository
{
    public interface ISurfaceTechnicalRepository
    {
        Task<SurfaceTechnicalParameters> GetSurfaceTechnicalParameters(SubsurfaceTechnicalParameters sstParms);
    }
}
