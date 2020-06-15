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
        private int posicao2 = 0;
        private int posicao3 = 0;
        public MontaJogosTorneioRegraCBTCom4Grupos()
        {
            posicao2 = new Random().Next(3, 5); // sorteia entre 3 e 4
            posicao3 = (posicao2 == 3) ? 4 : 3;
        }

        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo)
            {
                case 1:
                    return "1|3";
                case 2:
                    return posicao2 + "|2";
                case 3:
                    return posicao3 + "|1";
                case 4:
                    return "2|4";
                default:
                    return "0|0";
            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom5Grupos : MontaJogosTorneioRegraCBT
    {
        private int posicao4 = 0;
        private int posicao5 = 0;
        public MontaJogosTorneioRegraCBTCom5Grupos()
        {
            posicao4 = new Random().Next(3, 5); // sorteia entre 3 e 4
            posicao5 = (posicao4 == 3) ? 4 : 3;
        }
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
                    return posicao4 + "|0";
                case 5:
                    return posicao5 + "|0";
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
        private int posicao4 = 0;
        private int posicao5 = 0;
        public MontaJogosTorneioRegraCBTCom6Grupos()
        {
            posicao4 = new Random().Next(3, 5); // sorteia entre 3 e 4
            posicao5 = (posicao4 == 3) ? 4 : 3;
        }
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
                    return posicao4 + "|0";
                case 5:
                    return posicao5 + "|0";
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
        private int posicao4 = 0;
        private int posicao5 = 0;
        private int posicao3 = 0;
        private int posicao6 = 0;
        public MontaJogosTorneioRegraCBTCom7Grupos()
        {
            posicao4 = new Random().Next(3, 5); // sorteia entre 3 e 4
            posicao5 = (posicao4 == 3) ? 4 : 3;

            posicao3 = new Random().Next(5, 7); // sorteia entre 5 e 6
            posicao6 = (posicao3 == 5) ? 6 : 5;
        }

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
                    return posicao3 + "|3";
                case 4:
                    return posicao4 + "|5";
                case 5:
                    return posicao5 + "|4";
                case 6:
                    return posicao6 + "|6";
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
        private int posicao2 = 0;
        private int posicao3 = 0;
        private int posicao4 = 0;
        private int posicao5 = 0;
        private int posicao6 = 0;
        private int posicao7 = 0;
        public MontaJogosTorneioRegraCBTCom8Grupos()
        {
            posicao4 = new Random().Next(3, 5); // sorteia entre 3 e 4
            posicao5 = (posicao4 == 3) ? 4 : 3;

            List<int> listNumbers = new List<int>();
            int number;
            for (int i = 0; i < 4; i++)
            {
                do
                {
                    number = new Random().Next(5, 9);
                } while (listNumbers.Contains(number));
                listNumbers.Add(number);
            }

            posicao2 = listNumbers[0];
            posicao3 = listNumbers[1];
            posicao6 = listNumbers[2];
            posicao7 = listNumbers[3];
        }
        public override string GetPosicaoPorOrdemJogo(int ordemJogo)
        {
            
            // 2|3 onde 2 é a colocação dos primeiros colocados em cada grupo e 3 é o segundo colocado do grupo do 3o colocado entre os primeiros;
            // 2-S|3 quando tem S ao lado do primeiro número, quer dizer que a regra nesse caso é igual ao do exemplo do número 3;
            switch (ordemJogo)
            {
                case 1:
                    return "1|8";
                case 2:
                    return posicao2 + "|2";
                case 3:
                    return posicao3 + "|3";
                case 4:
                    return posicao4 + "|5";
                case 5:
                    return posicao5 + "|6";
                case 6:
                    return posicao6 + "|4";
                case 7:
                    return posicao7 + "|1";
                case 8:
                    return "2|7";
                default:
                    return "0|0";
            }
        }
    }

    public class MontaJogosTorneioRegraCBTCom9Grupos : MontaJogosTorneioRegraCBT
    {
        private int posicao3  = 0;
        private int posicao4  = 0;
        private int posicao5  = 0;
        private int posicao8  = 0;
        private int posicao9  = 0;
        private int posicao12 = 0;
        private int posicao13 = 0;

        public MontaJogosTorneioRegraCBTCom9Grupos()
        {
            posicao8 = new Random().Next(3, 5); // sorteia entre 3 e 4
            posicao9 = (posicao8 == 3) ? 4 : 3;

            List<int> listNumbers = new List<int>();
            int number;
            for (int i = 0; i < 5; i++)
            {
                do
                {
                    number = new Random().Next(5, 10);
                } while (listNumbers.Contains(number));
                listNumbers.Add(number);
            }

            posicao3 = listNumbers[0];
            posicao4 = listNumbers[1];
            posicao5 = listNumbers[2];
            posicao12 = listNumbers[3];
            posicao13 = listNumbers[4];
        }
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
                    return posicao3 + "|0";
                case 4:
                    return posicao4 + "|0";
                case 5:
                    return posicao5 + "|0";
                case 6:
                    return "2-S|0";
                case 7:
                    return "5-S|0";
                case 8:
                    return posicao8 + "|0";
                case 9:
                    return posicao9 + "|0";
                case 10:
                    return "4-S|0";
                case 11:
                    return "1-S|0";
                case 12:
                    return posicao12 + "|0";
                case 13:
                    return posicao13 + "|0";
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
        private int posicao3 = 0;
        private int posicao4 = 0;
        private int posicao5 = 0;
        private int posicao8 = 0;
        private int posicao9 = 0;
        private int posicao12 = 0;
        private int posicao13 = 0;

        public MontaJogosTorneioRegraCBTCom10Grupos()
        {
            posicao8 = new Random().Next(3, 5); // sorteia entre 3 e 4
            posicao9 = (posicao8 == 3) ? 4 : 3;

            List<int> listNumbers = new List<int>();
            int number;
            for (int i = 0; i < 5; i++)
            {
                do
                {
                    number = new Random().Next(5, 10);
                } while (listNumbers.Contains(number));
                listNumbers.Add(number);
            }

            posicao3 = listNumbers[0];
            posicao4 = listNumbers[1];
            posicao5 = listNumbers[2];
            posicao12 = listNumbers[3];
            posicao13 = listNumbers[4];
        }
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
                    return posicao3 + "|0";
                case 4:
                    return posicao4 + "|0";
                case 5:
                    return posicao5 + "|0";
                case 6:
                    return "2-S|10";
                case 7:
                    return "5-S|0";
                case 8:
                    return posicao8 + "|0";
                case 9:
                    return posicao9 + "|0";
                case 10:
                    return "4-S|0";
                case 11:
                    return "1-S|0";
                case 12:
                    return posicao12 + "|0";
                case 13:
                    return posicao13 + "|0";
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
        private int posicao3 = 0;
        private int posicao4 = 0;
        private int posicao5 = 0;
        private int posicao7 = 0;
        private int posicao8 = 0;
        private int posicao9 = 0;
        private int posicao12 = 0;
        private int posicao13 = 0;
        public MontaJogosTorneioRegraCBTCom11Grupos() {
            posicao8 = new Random().Next(3, 5); // sorteia entre 3 e 4
            posicao9 = (posicao8 == 3) ? 4 : 3;

            List<int> listNumbers = new List<int>();
            int number;
            for (int i = 0; i < 6; i++)
            {
                do
                {
                    number = new Random().Next(5, 12);
                } while ((listNumbers.Contains(number)) || (number == 10));
                listNumbers.Add(number);
            }

            posicao3 = listNumbers[0];
            posicao4 = listNumbers[1];
            posicao5 = listNumbers[2];
            posicao7 = listNumbers[3];
            posicao12 = listNumbers[4];
            posicao13 = listNumbers[5];
        }
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
                    return posicao3 + "|0";
                case 4:
                    return posicao4 + "|0";
                case 5:
                    return posicao5 + "|0";
                case 6:
                    return "2-S|10";
                case 7:
                    return posicao7 + "|5";
                case 8:
                    return posicao8 + "|0";
                case 9:
                    return posicao9 + "|0";
                case 10:
                    return "4-S|0";
                case 11:
                    return "1-S|11";
                case 12:
                    return posicao12 + "|0";
                case 13:
                    return posicao13 + "|0";
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
        private int posicao3 = 0;
        private int posicao4 = 0;
        private int posicao5 = 0;
        private int posicao7 = 0;
        private int posicao8 = 0;
        private int posicao9 = 0;
        private int posicao10 = 0;
        private int posicao12 = 0;
        private int posicao13 = 0;
        public MontaJogosTorneioRegraCBTCom12Grupos()
        {
            posicao8 = new Random().Next(3, 5); // sorteia entre 3 e 4
            posicao9 = (posicao8 == 3) ? 4 : 3;

            List<int> listNumbers = new List<int>();
            int number;
            for (int i = 0; i < 7; i++)
            {
                do
                {
                    number = new Random().Next(5, 13);
                } while ((listNumbers.Contains(number)) || (number == 10));
                listNumbers.Add(number);
            }

            posicao3 = listNumbers[0];
            posicao4 = listNumbers[1];
            posicao5 = listNumbers[2];
            posicao7 = listNumbers[3];
            posicao10 = listNumbers[4];
            posicao12 = listNumbers[5];
            posicao13 = listNumbers[6];
        }
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
                    return posicao3 + "|12";
                case 4:
                    return posicao4 + "|0";
                case 5:
                    return posicao5 + "|0";
                case 6:
                    return "2-S|10";
                case 7:
                    return posicao7 + "|5";
                case 8:
                    return "4|0";
                case 9:
                    return "3|0";
                case 10:
                    return posicao10 + "|4";
                case 11:
                    return "1-S|11";
                case 12:
                    return posicao12 + "|0";
                case 13:
                    return posicao13 + "|0";
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