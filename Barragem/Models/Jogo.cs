﻿using Barragem.Class;
using Barragem.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    [Table("Jogo")]
    public class Jogo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Rodada")]
        [Required(ErrorMessage = "Campo rodada obrigatório")]
        public int rodada_id { get; set; }

        public int? cabecaChave { get; set; }

        public int? cabecaChaveDesafiante { get; set; }

        [Display(Name = "Data")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}", ConvertEmptyStringToNull = true)]
        public DateTime? dataCadastroResultado { get; set; }

        public DateTime? dataLimiteJogo { get; set; }

        [Display(Name = "Data do jogo")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}", ConvertEmptyStringToNull = true)]
        public DateTime? dataJogo { get; set; }

        [Display(Name = "Hora")]
        public string horaJogo { get; set; }

        [Display(Name = "Local")]
        public string localJogo { get; set; }

        public string usuarioInformResultado { get; set; }

        [Display(Name = "Desafiante")]
        public int desafiante_id { get; set; }
        [Display(Name = "Desafiado")]
        public int desafiado_id { get; set; }

        [Display(Name = "Desafiante2")]
        public int? desafiante2_id { get; set; }
        [Display(Name = "Desafiado")]
        public int? desafiado2_id { get; set; }

        public int qtddGames1setDesafiante { get; set; }
        public int qtddGames2setDesafiante { get; set; }
        public int qtddGames3setDesafiante { get; set; }

        public int qtddGames1setDesafiado { get; set; }
        public int qtddGames2setDesafiado { get; set; }
        public int qtddGames3setDesafiado { get; set; }

        [Display(Name = "Vencedor")]
        public int idVencedor { get; set; }

        [Display(Name = "Perderdor")]
        public int idPerderdor { get; set; }
        [Display(Name = "Desafiado")]
        [ForeignKey("desafiado_id")]
        public virtual UserProfile desafiado { get; set; }

        [Display(Name = "Desafiante")]
        [ForeignKey("desafiante_id")]
        public virtual UserProfile desafiante { get; set; }

        [Display(Name = "Desafiado2")]
        [ForeignKey("desafiado2_id")]
        public virtual UserProfile desafiado2 { get; set; }

        [Display(Name = "Desafiante2")]
        [ForeignKey("desafiante2_id")]
        public virtual UserProfile desafiante2 { get; set; }

        [Display(Name = "Rodada")]
        [ForeignKey("rodada_id")]
        public virtual Rodada rodada { get; set; }

        public int situacao_Id { get; set; }

        [Display(Name = "Situação")]
        [ForeignKey("situacao_Id")]
        public virtual SituacaoJogo situacao { get; set; }

        public string quadra { get; set; }

        public int? torneioId { get; set; }

        public bool? isPrimeiroJogoTorneio { get; set; }

        public bool? isRepescagem { get; set; }

        public int? faseTorneio { get; set; }

        public int rodadaFaseGrupo { get; set; }

        public int? grupoFaseGrupo { get; set; }

        public virtual string descricaoFaseTorneio
        {
            get
            {
                return FaseTorneio.ObterAbreviacao(grupoFaseGrupo, faseTorneio, rodadaFaseGrupo);
            }

        }

        public virtual string dataHorarioQuadra
        {
            get
            {
                string descricao = "Aguardando";
                if ((situacao_Id == 4) || (situacao_Id == 5))
                {
                    descricao = "Finalizado";
                }
                if (dataJogo != null)
                {
                    descricao = ((DateTime)dataJogo).ToShortDateString();
                    descricao = descricao + " " + horaJogo + " ";
                }
                if ((quadra != null) && (quadra != "0") && (quadra != "100"))
                {
                    descricao = descricao + " quadra " + quadra;
                }

                return descricao;
            }

        }

        public int? classeTorneio { get; set; }

        [ForeignKey("classeTorneio")]
        public virtual ClasseTorneio classe { get; set; }

        public int? ordemJogo { get; set; }

        public virtual int setsJogados
        {
            get
            {
                var qtddSetsJogados = 0;
                if (qtddGames1setDesafiado > 0 || qtddGames1setDesafiante > 0)
                {
                    qtddSetsJogados++;
                }
                if (qtddGames2setDesafiado > 0 || qtddGames2setDesafiante > 0)
                {
                    qtddSetsJogados++;
                }
                if (qtddGames3setDesafiado > 0 || qtddGames3setDesafiante > 0)
                {
                    qtddSetsJogados++;
                }
                return qtddSetsJogados;
            }

        }
        public virtual int gamesJogados
        {
            get
            {
                return qtddGames1setDesafiado + qtddGames1setDesafiante + qtddGames2setDesafiado + qtddGames2setDesafiante + qtddGames3setDesafiado + qtddGames3setDesafiante;
            }

        }

        public virtual int gamesGanhosDesafiante
        {
            get
            {
                return qtddGames1setDesafiante + qtddGames2setDesafiante + qtddGames3setDesafiante;
            }

        }

        public virtual int gamesGanhosDesafiado
        {
            get
            {
                return qtddGames1setDesafiado + qtddGames2setDesafiado + qtddGames3setDesafiado;
            }

        }

        public virtual int qtddSetsGanhosDesafiante
        {
            get
            {
                var qtddSetsGanhos = 0;
                if (qtddGames1setDesafiante > qtddGames1setDesafiado)
                {
                    qtddSetsGanhos++;
                }
                if (qtddGames2setDesafiante > qtddGames2setDesafiado)
                {
                    qtddSetsGanhos++;
                }
                if (qtddGames3setDesafiante > qtddGames3setDesafiado)
                {
                    qtddSetsGanhos++;
                }
                return qtddSetsGanhos;
            }

        }

        public virtual int qtddSetsGanhosDesafiado
        {
            get
            {
                var qtddSetsGanhos = 0;
                if (qtddGames1setDesafiado > qtddGames1setDesafiante)
                {
                    qtddSetsGanhos++;
                }
                if (qtddGames2setDesafiado > qtddGames2setDesafiante)
                {
                    qtddSetsGanhos++;
                }
                if (qtddGames3setDesafiado > qtddGames3setDesafiante)
                {
                    qtddSetsGanhos++;
                }
                return qtddSetsGanhos;
            }

        }

        public virtual int idDoVencedor
        {
            get
            {
                if (qtddSetsGanhosDesafiado > qtddSetsGanhosDesafiante)
                {
                    return desafiado_id;
                }
                if (qtddSetsGanhosDesafiado < qtddSetsGanhosDesafiante)
                {
                    return desafiante_id;
                }
                return 0;
            }

        }

        public virtual int idDoVencedorDupla
        {
            get
            {
                if ((qtddSetsGanhosDesafiado > qtddSetsGanhosDesafiante) && (desafiado2_id != null))
                {
                    return (int)desafiado2_id;
                }
                if ((qtddSetsGanhosDesafiado < qtddSetsGanhosDesafiante) && (desafiante2_id != null))
                {
                    return (int)desafiante2_id;
                }
                return 0;
            }

        }

        public virtual int idDoPerdedor
        {
            get
            {
                if (qtddSetsGanhosDesafiado > qtddSetsGanhosDesafiante)
                {
                    return desafiante_id;
                }
                if (qtddSetsGanhosDesafiado < qtddSetsGanhosDesafiante)
                {
                    return desafiado_id;
                }
                return 0;
            }

        }

        public void AlterarJogoParaPendente()
        {
            situacao_Id = 1;
            ZerarPontuacao();
        }

        public void AlterarSituacaoJogoParaWO(TipoJogador tipoJogadorVencedor)
        {
            situacao_Id = 5; //WO
            if (tipoJogadorVencedor == TipoJogador.DESAFIANTE)
            {
                qtddGames1setDesafiado = 1;
                qtddGames2setDesafiado = 1;
                qtddGames1setDesafiante = 6;
                qtddGames2setDesafiante = 6;

            }
            else
            {
                qtddGames1setDesafiado = 6;
                qtddGames2setDesafiado = 6;
                qtddGames1setDesafiante = 1;
                qtddGames2setDesafiante = 1;
            }
            qtddGames3setDesafiado = 0;
            qtddGames3setDesafiante = 0;
        }

        public void ZerarPontuacao()
        {
            qtddGames1setDesafiado = 0;
            qtddGames2setDesafiado = 0;
            qtddGames3setDesafiado = 0;
            qtddGames1setDesafiante = 0;
            qtddGames2setDesafiante = 0;
            qtddGames3setDesafiante = 0;
        }

        public void AlterarJogoParaBye(TipoJogador tipoJogadorEfetuarTroca)
        {
            situacao_Id = 5; //WO
            qtddGames1setDesafiado = 6;
            qtddGames2setDesafiado = 6;
            qtddGames1setDesafiante = 1;
            qtddGames2setDesafiante = 1;

            if (tipoJogadorEfetuarTroca == TipoJogador.DESAFIADO)
            {
                //Se for o desafiado então troca o desafiante para desafiado
                desafiado_id = desafiante_id;
                desafiado2_id = desafiante2_id;
                desafiante_id = 10;
                desafiante2_id = null;
            }
            else
            {
                desafiante_id = 10;
                desafiante2_id = null;
            }
        }

        public void AlterarJogador(TipoJogador tipoJogadorEfetuarTroca, int idJogador, int? idJogadorDupla = null)
        {
            if (tipoJogadorEfetuarTroca == TipoJogador.DESAFIANTE)
            {
                desafiante_id = idJogador;
                if (idJogadorDupla > 0)
                {
                    desafiante2_id = idJogadorDupla;
                }
                else
                {
                    desafiante2_id = null;
                }
            }
            else 
            {
                desafiado_id = idJogador;
                if (idJogadorDupla > 0)
                {
                    desafiado2_id = idJogadorDupla;
                }
                else
                {
                    desafiado2_id = null;
                }
            }
            
        }

        public string ObterNomeJogador(TipoJogador tipoJogador)
        {
            if (tipoJogador == TipoJogador.DESAFIANTE)
            {
                if (desafiante_id == Constantes.Jogo.BYE)
                {
                    return "bye";
                }
                else if (desafiante_id == Constantes.Jogo.AGUARDANDO_JOGADOR)
                {
                    return "Aguardando jogador";
                }
                else
                {
                    if (desafiante2_id > 0)
                    {
                        return $"{desafiante?.nome}/{desafiante2?.nome}";
                    }
                    else
                    {
                        return desafiante?.nome;
                    }
                }
            }
            else
            {
                if (desafiado_id == Constantes.Jogo.AGUARDANDO_JOGADOR)
                {
                    return "Aguardando jogador";
                }
                else
                {
                    if (desafiado2_id > 0)
                    {
                        return $"{desafiado?.nome}/{desafiado2?.nome}";
                    }
                    else
                    {
                        return desafiado?.nome;
                    }
                }
            }
        }
    }

    public class MeuJogo
    {

        public int Id { get; set; }

        public bool naoPodelancarResultado { get; set; }

        public string rodada { get; set; }

        public string temporada { get; set; }

        public DateTime dataFinalRodada { get; set; }

        public DateTime? dataJogo { get; set; }

        public string horaJogo { get; set; }

        public string localJogo { get; set; }

        public int idDesafiante { get; set; }

        public int idDesafiado { get; set; }

        public int? idDesafianteDupla { get; set; }

        public int? idDesafiadoDupla { get; set; }

        public int qtddGames1setDesafiante { get; set; }
        public int qtddGames2setDesafiante { get; set; }
        public int qtddGames3setDesafiante { get; set; }

        public int qtddGames1setDesafiado { get; set; }
        public int qtddGames2setDesafiado { get; set; }
        public int qtddGames3setDesafiado { get; set; }

        public string nomeDesafiado { get; set; }

        public string nomeDesafiadoDupla { get; set; }

        public int posicaoDesafiado { get; set; }

        public string nomeDesafiante { get; set; }

        public string nomeDesafianteDupla { get; set; }

        public string fotoDesafiado { get; set; }

        public string fotoDesafiante { get; set; }

        public string fotoDesafiadoDupla { get; set; }

        public string fotoDesafianteDupla { get; set; }

        public int posicaoDesafiante { get; set; }

        public string situacao { get; set; }

        public int qtddSetsGanhosDesafiante { get; set; }

        public int qtddSetsGanhosDesafiado { get; set; }

        public int idDoVencedor { get; set; }

        public string linkWhatsapp { get; set; }

        public string nomeWhatsapp { get; set; }

        public string numeroWhatsapp { get; set; }


    }

    public class JogoRodada
    {

        public int Id { get; set; }
        public string nomeRodada { get; set; }
        public DateTime? dataJogo { get; set; }
        public string horaJogo { get; set; }
        public string localJogo { get; set; }
        public string nomeDesafiante { get; set; }
        public string nomeDesafianteDupla { get; set; }
        public string nomeDesafiado { get; set; }
        public string nomeDesafiadoDupla { get; set; }
        public int idDesafiante { get; set; }
        public int? idDesafianteDupla { get; set; }
        public int idDesafiado { get; set; }
        public int? idDesafiadoDupla { get; set; }
        public int rankingDesafiante { get; set; }
        public int rankingDesafiado { get; set; }
        public double pontuacaoDesafiante { get; set; }
        public double pontuacaoDesafiado { get; set; }
        public string fotoDesafiado { get; set; }
        public string fotoDesafiadoDupla { get; set; }
        public string fotoDesafiante { get; set; }
        public string fotoDesafianteDupla { get; set; }
        public int qtddGames1setDesafiante { get; set; }
        public int qtddGames2setDesafiante { get; set; }
        public int qtddGames3setDesafiante { get; set; }

        public int qtddGames1setDesafiado { get; set; }
        public int qtddGames2setDesafiado { get; set; }
        public int qtddGames3setDesafiado { get; set; }
        public int idVencedor { get; set; }
        public int idDoVencedor { get; set; }
        public string situacao { get; set; }
        public string nomeClasse { get; set; }
        public string descricaoFase { get; set; }
    }

    public class HeadToHead
    {

        public int Id { get; set; }
        public int idDesafiante { get; set; }
        public int idDesafiado { get; set; }
        public int qtddVitoriasDesafiante { get; set; }
        public int qtddVitoriasDesafiado { get; set; }
        public int idadeDesafiante { get; set; }
        public int idadeDesafiado { get; set; }
        public string alturaDesafiante { get; set; }
        public string alturaDesafiado { get; set; }
        public string naturalidadeDesafiante { get; set; }
        public string naturalidadeDesafiado { get; set; }
        public string lateralDesafiante { get; set; }
        public string lateralDesafiado { get; set; }
        public string inicioRankingDesafiante { get; set; }
        public string inicioRankingDesafiado { get; set; }
        public string melhorPosicaoDesafiante { get; set; }
        public string melhorPosicaoDesafiado { get; set; }

    }

    public class JogoTeste
    {
        public string jogo { get; set; }

    }

    public enum TipoJogador
    {
        DESAFIADO = 1,
        DESAFIANTE = 2
    }
}