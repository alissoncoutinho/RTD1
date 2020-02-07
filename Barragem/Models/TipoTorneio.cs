using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public abstract class TipoTorneio
    {
        public abstract int CalculaPontos(Jogo jogo);
    }
}