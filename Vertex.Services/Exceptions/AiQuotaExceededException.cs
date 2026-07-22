using System;

namespace Vertex.Services.Exceptions
{
    public class AiQuotaExceededException : Exception
    {
        public AiQuotaExceededException(int quota, int used)
            : base($"Organization AI quota exhausted ({used}/{quota} requests used). Upgrade the plan or contact an administrator.")
        {
            Quota = quota;
            Used = used;
        }

        public int Quota { get; }
        public int Used { get; }
        public int Remaining => Math.Max(0, Quota - Used);
    }
}
