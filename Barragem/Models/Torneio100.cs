using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public class Torneio100 : TipoTorneio
    {
        public override int CalculaPontos(InscricaoTorneio inscricao)
        {
            switch (inscricao.colocacao)
            {
                case 0:
                    return 100;
                case 1:
                    return 70;
                case 2:
                    return 50;
                case 3:
                    return 35;
                case 4:
                    return 20;
                case 5:
                    return 10;
                default:
                    return 5;
            }
        }
    }
}