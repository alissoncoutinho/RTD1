using System;
using System.Runtime.Serialization;

namespace Barragem.Class
{
    [Serializable]
    internal class ExceptionRankingDesativado : Exception
    {
        public ExceptionRankingDesativado()
        {
        }

        public ExceptionRankingDesativado(string message) : base(message)
        {
        }

        public ExceptionRankingDesativado(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExceptionRankingDesativado(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}