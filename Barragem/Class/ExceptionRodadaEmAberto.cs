using System;
using System.Runtime.Serialization;

namespace Barragem.Class
{
    [Serializable]
    internal class ExceptionRodadaEmAberto : Exception
    {
        public ExceptionRodadaEmAberto()
        {
        }

        public ExceptionRodadaEmAberto(string message) : base(message)
        {
        }

        public ExceptionRodadaEmAberto(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExceptionRodadaEmAberto(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}