using System;
using System.Threading;
using System.Threading.Tasks;

namespace LEWP.Common
{
    public interface IPhotoService
    {
        Task Start(TimeSpan interval, CancellationToken token);
    }
}