using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public class Torneio250 : TipoTorneio
    {
        public override int CalculaPontos(InscricaoTorneio inscricao)
        {
            switch (inscricao.colocacao)
            {
                case 0:
                    return 250;
                case 1:
                    return 175;
                case 2:
                    return 125;
                case 3:
                    return 75;
                case 4:
                    return 37;
                case 5:
                    return 25;
                case 100:  // eliminado na fase de grupo
                    return (int) (0.05 * 250);
                default:
                    return 12;
            }
        }
    }
}