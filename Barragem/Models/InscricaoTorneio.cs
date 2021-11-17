using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Models
{
    public class InscricaoTorneio
    {

        public InscricaoTorneio() {
            cabecaChave = 100;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Participante")]
        public int userId { get; set; }

        [Display(Name = "Participante")]
        [ForeignKey("userId")]
        public virtual UserProfile participante { get; set; }

        [Display(Name = "Parceiro Dupla")]
        public int? parceiroDuplaId { get; set; }

        [Display(Name = "Parceiro Dupla")]
        [ForeignKey("parceiroDuplaId")]
        public virtual UserProfile parceiroDupla { get; set; }

        public bool isAtivo { get; set; }

        public bool? isSocio { get; set; }

        public bool? isFederado { get; set; }

        [Display(Name = "Classe")]
        [ForeignKey("classe")]
        public virtual ClasseTorneio classeTorneio { get; set; }

        public int classe { get; set; }

        [Display(Name = "Torneio")]
        [ForeignKey("torneioId")]
        public virtual Torneio torneio { get; set; }

        public int torneioId { get; set; }

        public int? colocacao { get; set; }

        public string statusPagamento { get; set; }

        public virtual string descricaoStatusPag{
            /* Código	Significado
                1	Aguardando pagamento: o comprador iniciou a transação, mas até o momento o PagSeguro não recebeu nenhuma informação sobre o pagamento.
                2	Em análise: o comprador optou por pagar com um cartão de crédito e o PagSeguro está analisando o risco da transação.
                3	Paga: a transação foi paga pelo comprador e o PagSeguro já recebeu uma confirmação da instituição financeira responsável pelo processamento.
                4	Disponível: a transação foi paga e chegou ao final de seu prazo de liberação sem ter sido retornada e sem que haja nenhuma disputa aberta.
                5	Em disputa: o comprador, dentro do prazo de liberação da transação, abriu uma disputa.
                6	Devolvida: o valor da transação foi devolvido para o comprador.
                7	Cancelada: a transação foi cancelada sem ter sido finalizada.
                8	Debitado: o valor da transação foi devolvido para o comprador.
                9	Retenção temporária: o comprador contestou o pagamento junto à operadora do cartão de crédito ou abriu uma demanda judicial ou administrativa (Procon).
                */
            get{
                if (statusPagamento == null){
                    return "";
                }
                if (statusPagamento.Equals("1")){
                    return "Aguardando pagamento";
                }
                if (statusPagamento.Equals("2"))
                {
                    return "Em análise";
                }
                if (statusPagamento.Equals("3"))
                {
                    return "Paga";
                }
                if (statusPagamento.Equals("4"))
                {
                    return "Disponível";
                }
                if (statusPagamento.Equals("5"))
                {
                    return "Em disputa";
                }
                if (statusPagamento.Equals("6"))
                {
                    return "Devolvida";
                }
                if (statusPagamento.Equals("7"))
                {
                    return "Cancelada";
                }
                if (statusPagamento.Equals("8"))
                {
                    return "Debitado";
                }
                if (statusPagamento.Equals("9"))
                {
                    return "Retenção Temporária";
                }
                return "";
            }

        } 

        public string formaPagamento { get; set; }

        public string observacao { get; set; }

        public double? valor { get; set; }

        [Display(Name = "Cabeça de Chave")]
        public int? cabecaChave { get; set; }

        public int? grupo { get; set; }

        public int pontuacaoFaseGrupo { get; set; }

        public int? Pontuacao { get; set; }

        public double? valorPendente { get; set; }

    }

    public class ColocacaoTorneio {
        public string getDescricaoColocacao(int? colocId)
        {
            if (colocId == null)
            {
                return "Sem informação";
            }
            else if (colocId == 0)
            {
                return "Campeão";
            }
            else if (colocId == 1)
            {
                return "Vice-Campeão";
            }
            else if (colocId == 2)
            {
                return "Semi-finais";
            }
            else if (colocId == 3)
            {
                return "Quartas de final";
            }
            else if (colocId == 4)
            {
                return "Oitavas de final";
            }
            else if (colocId == 5)
            {
                return "R2";
            }
            else
            {
                return "Primeira fase";
            }
        }
        public int? colocacaoId { get; set; }
        public string colocacao { get; set; }
        public string  nomeTorneio { get; set; }
        public string  classe { get; set; }
        public DateTime dataTorneio { get; set; }
        public int? pontuacao { get; set; }
        public string nomeLiga { get; set; }

    }

    public class Inscrito
    {
        public string foto { get; set; }
        public string nome { get; set; }
        public int userId { get; set; }
        public string classe { get; set; }
        public int classeId { get; set; }
        public string nomeDupla { get; set; }
        public string fotoDupla { get; set; }
        public bool? exibeBotaoFormarDupla { get; set; }
    }
   

}