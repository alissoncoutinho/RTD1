namespace Barragem.Helper
{
    public class Constantes
    {
        public class Jogo
        {
            public const int BYE = 10;
            public const int AGUARDANDO_JOGADOR = 0;

            public class Situacao
            {
                public const int PENDENTE = 1;
                public const int MARCADO = 2;
                public const int EM_ANDAMENTO = 3;
                public const int FINALIZADO = 4;
                public const int WO = 5;
                public const int DESISTENCIA = 6;
            }
        }
    }
}