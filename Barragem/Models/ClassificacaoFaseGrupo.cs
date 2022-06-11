using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public class ClassificacaoFaseGrupo
    {
        public int userId { get; set; }
        public int saldoSets { get; set; }
        public int saldoGames { get; set; }
        public float averageSets { get; set; }
        public float averageGames { get; set; }
        public string nome { get; set; }
        public string nomeDupla { get; set; }
        public int confrontoDireto { get; set; }
        public InscricaoTorneio inscricao { get; set; }
    }

    public class ClassificacaoFaseGrupoApp
    {
        public int userId { get; set; }
        public int saldoSets { get; set; }
        public int saldoGames { get; set; }
        public string nome { get; set; }
        public string nomeDupla { get; set; }
        public int confrontoDireto { get; set; }
        public int pontuacao { get; set; }
    }

    public class ClassificadosEmCadaGrupo
    {
        public int userId { get; set; }
        public string nomeUser { get; set; }
        public int? userIdParceiro { get; set; }
        public string nomeParceiro { get; set; }
        public int userId2oColocado { get; set; }
        public string nome2oColocado { get; set; }
        public int? userIdParceiro2oColocado { get; set; }
        public string nomeParceiro2oColocado { get; set; }
        public float averageSets { get; set; }
        public float averageGames { get; set; }
        public int saldoSets { get; set; }
        public int saldoGames { get; set; }
        public int pontuacao { get; set; }
        public int grupo { get; set; }
        public int PontuacaoRanking { get; set; }
    }

    
}