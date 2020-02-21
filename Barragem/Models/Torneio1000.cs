using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public class Torneio1000 : TipoTorneio
    {
        public override int CalculaPontos(InscricaoTorneio inscricao)
        {
            switch (inscricao.colocacao)
            {
                case 0:
                    return 1000;
                case 1:
                    return 700;
                case 2:
                    return 500;
                case 3:
                    return 300;
                case 4:
                    return 150;
                case 5:
                    return 100;
                default:
                    return 50;
            }
        }
    }
}