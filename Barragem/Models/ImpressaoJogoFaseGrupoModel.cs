using System.Collections.Generic;

namespace Barragem.Models
{
    public class ImpressaoJogoFaseGrupoModel
    {
        public string NomeTorneio { get; set; }
        public string NomeRanking { get; set; }
        public int IdBarragem { get; set; }
        public string NomeClasse { get; set; }
        public List<GrupoJogosModel> Grupos { get; set; }

        public class GrupoJogosModel 
        {
            public string Grupo { get; set; }
            public List<Jogo> Jogos { get; set; }
        }
    }
}