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
            
        }

        public abstract String GetPosicaoPorOrdemJogo(int ordemJogo);
    }

    public class InstanciaClassePorGrupo
    {
        public static MontaJogosTorneioRegraCBT getInstancia(int grupo) {
            switch (grupo)
            {
                case 3:
                    return new MontaJogosTorneioRegraCBTCom3Grupos();
                case 4:
                    return new MontaJogosTorneioRegraCBTCom4Grupos();
                case 5:
                    return new MontaJogosTorneioRegraCBTCom5Grupos();
                default:
                    return null;

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
}