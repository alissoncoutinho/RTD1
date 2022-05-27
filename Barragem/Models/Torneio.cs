using System;
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
        public Torneio()
        {
            this.isMaisUmaClasse = false;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string nome { get; set; }

        [Display(Name = "Observação")]
        public string observacao { get; set; }

        [Display(Name = "Data Fim da Inscrição")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Data fim da inscrição é obrigatório")]
        public DateTime dataFimInscricoes { get; set; }

        [Display(Name = "Data Início")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Data Início é obrigatório")]
        public DateTime dataInicio { get; set; }

        [Display(Name = "Data Fim")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Data Fim é obrigatório")]
        public DateTime dataFim { get; set; }

        [Display(Name = "Periodicidade dos jogos")]
        public int periodicidadeJogos { get; set; }

        [Display(Name = "Premiação")]
        public string premiacao { get; set; }

        [Display(Name = "Qtdd de classe")]
        public int qtddClasses { get; set; }

        [Display(Name = "Valor da Inscrição")]
        public double? valor { get; set; }

        [Display(Name = "Valor de 2 Inscrições")]
        public double? valor2 { get; set; }

        [Display(Name = "Valor de 3 Inscrições")]
        public double? valor3 { get; set; }

        [Display(Name = "Valor de 4 Inscrições")]
        public double? valor4 { get; set; }

        [Display(Name = "Qtdd Categoria por Jogador")]
        public int qtddCategoriasPorJogador { get; set; }

        [Display(Name = "Valor com desconto")]
        public double? valorSocio { get; set; }

        [Display(Name = "Valor para +1 classe")]
        public double? valorMaisClasses { get; set; }

        [Display(Name = "Valor desconto +1 classe")]
        public double? valorMaisClassesSocio { get; set; }

        [Display(Name = "Tem Repescagem")]
        public bool temRepescagem { get; set; }

        [Display(Name = "Ativo")]
        public bool isAtivo { get; set; }

        [Display(Name = "Torneio Aberto")]
        public bool isOpen { get; set; }

        [Display(Name = "Divulgar apenas na cidade")]
        public bool divulgaCidade { get; set; }

        public string divulgacao { get; set; }

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

        public IList<int> liga { get; set; }

        public IList<int> classes { get; set; }

        public String TipoTorneio { get; set; }

        public string descontoPara { get; set; }
        public bool? isDesconto { get; set; }

        [Display(Name = "Desconto para Federado")]
        public double? valorDescontoFederado { get; set; }

        [UIHint("tinymce_full_compressed"), AllowHtml]
        [Display(Name = "Regulamento")]
        public string regulamento { get; set; }

        [Display(Name = "Torneio Foi Pago?")]
        public bool torneioFoiPago { get; set; }

        [UIHint("tinymce_full_compressed"), AllowHtml]
        [Display(Name = "Contato")]
        public string contato { get; set; }

        [Display(Name = "Tem limite de inscrição?")]
        public bool? temLimiteDeInscricao { get; set; }

        public bool? inscricaoSoPeloSite { get; set; }

        public int StatusInscricao { get; set; }
    }


    public class TorneioApp
    {
        public int Id { get; set; }
        public int logoId { get; set; }
        public string nome { get; set; }
        public DateTime dataInicio { get; set; }
        public DateTime dataFim { get; set; }
        public string cidade { get; set; }
        public string premiacao { get; set; }
        public string contato { get; set; }
        public string regulamento { get; set; }
        public double? valor { get; set; }
        public double? valorSocio { get; set; }
        public DateTime dataFimInscricoes { get; set; }
        public List<Patrocinador> patrocinadores { get; set; }
        public string pontuacaoLiga { get; set; }
        public string nomeLiga { get; set; }
        public bool? inscricaoSoPeloSite { get; set; }
        public bool? isBeachTennis { get; set; }
        public bool? temPIX { get; set; }
    }

    public class Patrocinador
    {
        public int Id { get; set; }
        public string urlImagem { get; set; }
        public string urllink { get; set; }
        public int torneioId { get; set; }
    }

    public class CriterioDesempateFaseGrupo
    {
        [UIHint("tinymce_full_compressed"), AllowHtml]
        public string descricao { get; set; }
    }

    public class PontuacaoLiga
    {
        public string descricao { get; set; }
        public string pontuacao { get; set; }
    }

    public class TabelaApp
    {
        public List<ClasseTorneioApp> classes { get; set; }
        public List<ClassificacaoFaseGrupoApp> classificacaoFaseGrupoApp { get; set; }
        public string descricaoFase { get; set; }
        public List<MeuJogo> jogos { get; set; }
        public int faseTorneio { get; set; }
        public bool isFaseGrupo { get; set; }
        public int userGrupo { get; set; }
    }

    public class TorneioClassesApp
    {
        public Torneio torneio { get; set; }
        public List<ClasseTorneio> classesTorneio { get; set; }
    }

    public class MensagemRetorno
    {
        public int id { get; set; }
        public string mensagem { get; set; }
        public string tipo { get; set; } // erro ou ok
        public string nomePagina { get; set; }
    }

    public class CobrancaTorneio
    {
        public int qtddInscritos { get; set; }
        public int valorASerPago { get; set; }
        public int valorDescontoParaRanking { get; set; }
        public QrCodeCobrancaTorneio qrCode { get; set; }
    }

    public class QrCodeCobrancaTorneio
    {
        public string text { get; set; }
        public string link { get; set; }
        public string erroGerarQrCode { get; set; }
    }

    public enum StatusInscricaoPainelTorneio
    {
        ABERTA = 1,
        PAUSADA = 2,
        LIBERADA_ATE = 3
    }
}