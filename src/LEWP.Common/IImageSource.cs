using System.Threading;

namespace LEWP.Common
{
    public interface IImageSource
    {
        string GetImage(CancellationToken token);
    }
}