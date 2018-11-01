﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Models
{
    [Table("Torneio")]
    public class Torneio
    {
        public Torneio(){
            this.isMaisUmaClasse = false;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string nome { get; set; }

        [Display(Name = "Observação")]
        public string observacao { get; set; }

        [Display(Name = "Data Fim da Inscrição")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Este campo é obrigatório")]
        public DateTime dataFimInscricoes { get; set; }

        [Display(Name = "Data Início")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Este campo é obrigatório")]
        public DateTime dataInicio { get; set; }

        [Display(Name = "Data Fim")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Este campo é obrigatório")]
        public DateTime dataFim { get; set; }

        [Display(Name = "Periodicidade dos jogos")]
        public int periodicidadeJogos { get; set; }

        [Display(Name = "Premiação")]
        public string premiacao { get; set; }

        [Display(Name = "Qtdd de classe")]
        public int qtddClasses { get; set; }

        [Display(Name = "Valor da Inscrição")]
        public double? valor { get; set; }

        [Display(Name = "Valor para +1 classe")]
        public double? valorMaisClasses { get; set; }

        [Display(Name = "Tem Repescagem")]
        public bool temRepescagem { get; set; }

        [Display(Name = "Ativo")]
        public bool isAtivo { get; set; }

        [Display(Name = "Torneio Aberto")]
        public bool isOpen { get; set; }

        [Display(Name = "Divulgar apenas na cidade")]
        public bool divulgaCidade { get; set; }

        [Display(Name = "Inscrição +1 classe")]
        public bool isMaisUmaClasse { get; set; }

        public int barragemId { get; set; }

        [ForeignKey("barragemId")]
        public virtual BarragemView barragem { get; set; }

        [Display(Name = "Liberar Tabela")]
        public bool liberarTabela { get; set; }

        [Display(Name = "Liberar Tabela de Inscrições")]
        public bool liberaTabelaInscricao { get; set; }

        [Display(Name = "Gratuito para Sócio")]
        public bool isGratuitoSocio { get; set; }

        [Display(Name = "Jogador NÃO lança resultado")]
        public bool jogadorNaoLancaResult { get; set; }
        [Display(Name = "Liberar Escolha de Duplas")]
        public bool liberarEscolhaDuplas { get; set; }
        [Display(Name = "Cidade")]
        public string cidade { get; set; }
        [Display(Name = "Local")]
        public string local { get; set; }

        [UIHint("tinymce_full_compressed"), AllowHtml]
        [Display(Name = "Dados Bancários")]
        public string dadosBancarios { get; set; }


    }


}