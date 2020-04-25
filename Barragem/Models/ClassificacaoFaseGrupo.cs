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
        public string nome { get; set; }
        public int confrontoDireto { get; set; }
        public InscricaoTorneio inscricao { get; set; }
    }
}