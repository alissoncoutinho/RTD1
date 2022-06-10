using Barragem.Context;
using Barragem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace Barragem.Class
{
    public abstract class MontaJogosTorneioRegraCBT
    {
        public void MontarJogosPrimeiraRodada(List<Jogo> jogosPrimeiraRodada , List<ClassificadosEmCadaGrupo> primeirosColocados, BarragemDbContext db) {
            foreach (var jogo in jogosPrimeiraRodada) {
                var posicoes = GetPosicaoPorOrdemJogo((int)jogo.ordemJogo).Split('|');
                montarJogo(jogo, posicoes, primeirosColocados);
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        private void montarJogo(Jogo jogo, string[] posicoes, List<ClassificadosEmCadaGrupo> primeirosColocados)
        {
            var arrayPosicao1o = posicoes[0].Split('-');
            int posicao1o = Convert.ToInt32(arrayPosicao1o[0]);
            int posicao2o = Convert.ToInt32(posicoes[1]);
            if (arrayPosicao1o.Count() == 1){
                jogo.desafiado_id = primeirosColocados[posicao1o - 1].userId;
                jogo.desafiado2_id = primeirosColocados[posicao1o - 1].userIdParceiro;
            } else {
                jogo.desafiado_id = primeirosColocados[posicao1o - 1].userId2oColocado;
                jogo.desafiado2_id = primeirosColocados[posicao1o - 1].userIdParceiro2oColocado;
            }
            if (posicao2o != 0) {
                jogo.desafiante_id = primeirosColocados[posicao2o - 1].userId2oColocado;
                jogo.desafiante2_id = primeirosColocados[posicao2o - 1].userIdParceiro2oColocado;
            }
            if (jogo.desafiado_id == 10)
            {
                var desafianteTemp_id = jogo.desafiante_id;
                var desafianteTemp2_id = jogo.desafiante2_id;
                jogo.desafiante_id = jogo.desafiado_id;
                jogo.desafiante2_id = jogo.desafiado2_id;
                jogo.desafiado_id = desafianteTemp_id;
                jogo.desafiado2_id = desafianteTemp2_id;
            }
            
        }

        public abstract String GetPosicaoPorOrdemJogo(int ordemJogo);
    }

    public class InstanciaClassePorGrupo
    {
        public static MontaJogosTorneioRegraCBT getInstancia(int grupo) {
            switch (grupo)
            {
                case 1:
                    return new MontaJogosTorneioRegraCBTCom1Grupos();
                case 2:
                    return new MontaJogosTorneioRegraCBTCom2Grupos();
                case 3:
                    return new MontaJogosTorneioRegraCBTCom3Grupos();
                case 4:
                    return new MontaJogosTorneioRegraCBTCom4Grupos();
                case 5:
                    return new MontaJogosTorneioRegraCBTCom5Grupos();
                case 6:
                    return new MontaJogosTorneioRegraCBTCom6Grupos();
                case 7:
                    return new MontaJogosTorneioRegraCBTCom7Grupos();
                case 8:
                    return new MontaJogosTorneioRegraCBTCom8Grupos();
                case 9:
                    return new MontaJogosTorneioRegraCBTCom9Grupos();
                case 10:
                    return new MontaJogosTorneioRegraCBTCom10Grupos();
                case 11:
                    return new MontaJogosTorneioRegraCBTCom11Grupos();
                case 12:
                    return new MontaJogosTorneioRegraCBTCom12Grupos();
                default:
                    return null;

            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom1Grupos : MontaJogosTorneioRegraCBT
    {

        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo)
            {
                case 1:
                    return "1|1";
                default:
                    return "0|0";
            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom2Grupos : MontaJogosTorneioRegraCBT
    {

        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo)
            {
                case 1:
                    return "1|2";
                case 2:
                    return "2|1";
                default:
                    return "0|0";
            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom3Grupos : MontaJogosTorneioRegraCBT
    {
        
        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo) {
                case 1:
                    return "1|0";
                case 2:
                    return "3-S|2";
                case 3:
                    return "3|1";
                case 4:
                    return "2|0";
                default:
                    return "0|0";
            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom4Grupos : MontaJogosTorneioRegraCBT
    {
        //TODO: Montagem dos jogos estranha...
        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo)
            {
                case 1:
                    return "1|3";
                case 2:
                    return "4|2";
                case 3:
                    return "3|1";
                case 4:
                    return "2|4";
                default:
                    return "0|0";
            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom5Grupos : MontaJogosTorneioRegraCBT
    {
        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo)
            {
                case 1:
                    return "1|0";
                case 2:
                    return "5-S|2";
                case 3:
                    return "3-S|0";
                case 4:
                    return "4|0";
                case 5:
                    return "3|0";
                case 6:
                    return "5|0";
                case 7:
                    return "1-S|4";
                case 8:
                    return "2|0";
                default:
                    return "0|0";
            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom6Grupos : MontaJogosTorneioRegraCBT
    {
        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo)
            {
                case 1:
                    return "1|0";
                case 2:
                    return "5-S|2";
                case 3:
                    return "6|3";
                case 4:
                    return "4|0";
                case 5:
                    return "3|0";
                case 6:
                    return "5|6";
                case 7:
                    return "1-S|4";
                case 8:
                    return "2|0";
                default:
                    return "0|0";
            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom7Grupos : MontaJogosTorneioRegraCBT
    {
        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo)
            {
                case 1:
                    return "1|0";
                case 2:
                    return "7|2";
                case 3:
                    return "6|3";
                case 4:
                    return "4|5";
                case 5:
                    return "3|4";
                case 6:
                    return "5|6";
                case 7:
                    return "1-S|7";
                case 8:
                    return "2|0";
                default:
                    return "0|0";
            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom8Grupos : MontaJogosTorneioRegraCBT
    {
        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo)
            {
                case 1:
                    return "1|8";
                case 2:
                    return "7|2";
                case 3:
                    return "6|3";
                case 4:
                    return "4|5";
                case 5:
                    return "3|6";
                case 6:
                    return "5|4";
                case 7:
                    return "8|1";
                case 8:
                    return "2|7";
                default:
                    return "0|0";
            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom9Grupos : MontaJogosTorneioRegraCBT
    {
        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo)
            {
                case 1:
                    return "1|0";
                case 2:
                    return "8-S|3";
                case 3:
                    return "9|0";
                case 4:
                    return "6|0";
                case 5:
                    return "7|0";
                case 6:
                    return "2-S|0";
                case 7:
                    return "5-S|0";
                case 8:
                    return "4|0";
                case 9:
                    return "3|0";
                case 10:
                    return "4-S|0";
                case 11:
                    return "1-S|0";
                case 12:
                    return "8|0";
                case 13:
                    return "5|0";
                case 14:
                    return "9-S|0";
                case 15:
                    return "6-S|7";
                case 16:
                    return "2|0";
                default:
                    return "0|0";
            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom10Grupos : MontaJogosTorneioRegraCBT
    {
        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo)
            {
                case 1:
                    return "1|0";
                case 2:
                    return "8-S|3";
                case 3:
                    return "9|0";
                case 4:
                    return "6|0";
                case 5:
                    return "7|0";
                case 6:
                    return "2-S|10";
                case 7:
                    return "5-S|0";
                case 8:
                    return "4|0";
                case 9:
                    return "3|0";
                case 10:
                    return "4-S|0";
                case 11:
                    return "1-S|0";
                case 12:
                    return "8|0";
                case 13:
                    return "5|0";
                case 14:
                    return "10|9";
                case 15:
                    return "6-S|7";
                case 16:
                    return "2|0";
                default:
                    return "0|0";
            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom11Grupos : MontaJogosTorneioRegraCBT
    {
        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo)
            {
                case 1:
                    return "1|0";
                case 2:
                    return "8-S|3";
                case 3:
                    return "9|0";
                case 4:
                    return "6|0";
                case 5:
                    return "7|0";
                case 6:
                    return "2-S|10";
                case 7:
                    return "11|5";
                case 8:
                    return "4|0";
                case 9:
                    return "3|0";
                case 10:
                    return "4-S|0";
                case 11:
                    return "1-S|11";
                case 12:
                    return "8|0";
                case 13:
                    return "5|0";
                case 14:
                    return "10|9";
                case 15:
                    return "6-S|7";
                case 16:
                    return "2|0";
                default:
                    return "0|0";
            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom12Grupos : MontaJogosTorneioRegraCBT
    {
        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo)
            {
                case 1:
                    return "1|0";
                case 2:
                    return "8-S|3";
                case 3:
                    return "9|12";
                case 4:
                    return "6|0";
                case 5:
                    return "7|0";
                case 6:
                    return "2-S|10";
                case 7:
                    return "11|5";
                case 8:
                    return "4|0";
                case 9:
                    return "3|0";
                case 10:
                    return "12|4";
                case 11:
                    return "1-S|11";
                case 12:
                    return "8|0";
                case 13:
                    return "5|0";
                case 14:
                    return "10|9";
                case 15:
                    return "6-S|7";
                case 16:
                    return "2|0";
                default:
                    return "0|0";
            }
        }
    }
}