using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public class CalculadoraDePontos
    {
        public static TipoTorneio AddTipoTorneio(String tipo)
        {
            switch (tipo)
            {
                case "250":
                    return new Torneio250();
                case "500":
                    return new Torneio500();
                case "1000":
                    return new Torneio1000();
                default:
                    return new TorneioGenerico();
            }
        }
        
    }
}