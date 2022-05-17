using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Models
{
    [Table("ClasseTorneio")]
    public class ClasseTorneio
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Nome")]
        public string nome { get; set; }
        public int nivel { get; set; }
        public int torneioId { get; set; }
        public bool isSegundaOpcao { get; set; }
        public bool isPrimeiraOpcao { get; set; }
        public bool isDupla { get; set; }
        public bool faseGrupo { get; set; }
        public bool faseMataMata { get; set; }
        public int qtddPassamFase { get; set; }
        public int qtddJogadoresPorGrupo { get; set; }
        public int maximoInscritos { get; set; }

        public int? categoriaId { get; set; }
        [ForeignKey("categoriaId")]
        public virtual Categoria Categoria { get; set; }
    }

    public class ClasseTorneioApp
    {
        public int Id { get; set; }
        public string nome { get; set; }
        public bool selected { get; set; }
        public bool faseGrupo { get; set; }
        public int grupoUser { get; set; }
        public int qtddRodadaFaseGrupo { get; set; }
        public int qtddRodadaMataMata { get; set; }
        public bool faseMataMata { get; set; }
        public int qtddGruposFaseGrupo { get; set; }
    }

    public class ClasseTorneioQtddInscrito
    {
        public int Id { get; set; }
        public int qtddInscritos { get; set; }
    }
}