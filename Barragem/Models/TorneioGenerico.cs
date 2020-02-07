using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public class TorneioGenerico : TipoTorneio
    {
        public override int CalculaPontos(Jogo jogo)
        {
            return 0;
        }
    }
}