using System;
using System.Web.Mvc;

namespace Barragem.Models
{
    public class PainelTorneioModel
    {
        public int TorneioId { get; set; }
        public bool IsAtivo { get; set; }
        public string LinkParaCopia { get; set; }
        public bool LiberaVisualizacaoTabela { get; set; }
        public bool LiberaVisualizacaoInscritos { get; set; }
        public SelectList ListaOpcoesStatusInscricao { get; set; }
        public SelectList ListaOpcoesDivulgacao { get; set; }
        public DateTime DataFimInscricoes { get; set; }

        public int InscIndividuais { get; set; }
        public int InscIndividuaisSocios { get; set; }
        public int InscIndividuaisFederados { get; set; }
        public int TotalPagantes { get; set; }
        public int PagoNoCartao { get; set; }
        public double ValorPago { get; set; }
    }
}