using GeophiresSharp.Models;
using System.Threading.Tasks;

namespace GeophiresSharp.Repository
{
    public interface ISurfaceTechnicalRepository
    {
        Task<SurfaceTechnicalParameters> GetSurfaceTechnicalParameters(SubsurfaceTechnicalParameters sstParms);
    }
}
