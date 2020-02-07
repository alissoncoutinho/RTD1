using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public class Torneio500 : TipoTorneio
    {
        public override int CalculaPontos(Jogo jogo)
        {
            switch (jogo.faseTorneio)
            {
                case 0:
                    return 500;
                case 1:
                    return 350;
                case 2:
                    return 250;
                case 3:
                    return 150;
                case 4:
                    return 75;
                case 5:
                    return 50;
                default:
                    return 25;
            }
        }
    }
}