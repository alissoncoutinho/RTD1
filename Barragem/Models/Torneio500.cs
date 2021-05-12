using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public class Torneio500 : TipoTorneio
    {
        public override int CalculaPontos(InscricaoTorneio inscricao)
        {
            switch (inscricao.colocacao)
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
                case 100:  // eliminado na fase de grupo
                    return (int) (0.05 * 500);
                default:
                    return 25;
            }
        }
    }
}