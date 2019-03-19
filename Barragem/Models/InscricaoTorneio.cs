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

    }
}