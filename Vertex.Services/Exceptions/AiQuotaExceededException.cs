using System;

namespace Vertex.Services.Exceptions
{
    public class AiQuotaExceededException : Exception
    {
        public AiQuotaExceededException(int quota, int used)
            : base($"AI quota exhausted ({used}/{quota} requests used). Contact an administrator to increase the quota.")
        {
            Quota = quota;
            Used = used;
        }

        public int Quota { get; }
        public int Used { get; }
        public int Remaining => Math.Max(0, Quota - Used);
    }
}
