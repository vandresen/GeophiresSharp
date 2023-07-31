using System.Threading.Tasks;

namespace BlazorGeophiresSharp.Client.Services
{
    public interface IDisplayMessage
    {
        ValueTask DisplayErrorMessage(string message);
        ValueTask DisplaySuccessMessage(string message);
    }
}
