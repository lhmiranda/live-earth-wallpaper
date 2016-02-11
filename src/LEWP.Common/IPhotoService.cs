using System.Threading;
using System.Threading.Tasks;

namespace LEWP.Common
{
    public interface IPhotoService
    {
        Task Start(CancellationToken token);
        void ForceStart();
        bool CanForce();
    }
}