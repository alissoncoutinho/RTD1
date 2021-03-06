﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Models
{
    [Table("Barragem")]
    public class Barragens
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "nome")]
        public string nome { get; set; }

        [Display(Name = "email")]
        public string email { get; set; }

        [Display(Name = "situação")]
        public bool isAtiva { get; set; }

        [Display(Name = "em teste")]
        public bool isTeste { get; set; }

        [Display(Name = "regulamento")]
        [UIHint("tinymce_full_compressed"), AllowHtml]
        public string regulamento { get; set; }

        [UIHint("tinymce_full_compressed"), AllowHtml]
        [Display(Name = "quemsomos")]
        public string quemsomos { get; set; }

        [UIHint("tinymce_full_compressed"), AllowHtml]
        [Display(Name = "contato")]
        public string contato { get; set; }
        [Display(Name = "link pagseguro")]
        public string linkPagSeguro { get; set; }
        [Display(Name = "Classe única")]
        public bool isClasseUnica { get; set; }
        [Display(Name = "Domínio")]
        public string dominio { get; set; }
        [Display(Name = "Só Torneio")]
        public bool? soTorneio { get; set; }
        [Display(Name = "Email PagSeguro")]
        public string emailPagSeguro { get; set; }
        [Display(Name = "Token PagSeguro")]
        public string tokenPagSeguro { get; set; }
        [Display(Name = "Cidade")]
        public string cidade { get; set; }
        public double? valorPorUsuario { get; set; }
        [Display(Name = "Cpf Resp.")]
        public string cpfResponsavel { get; set; }
        [Display(Name = "Nome Resp.")]
        public string nomeResponsavel { get; set; }
        [Display(Name = "Suspensao por atraso")]
        public bool suspensaoPorAtraso { get; set; }
        [Display(Name = "Suspensao por WO")]
        public bool suspensaoPorWO { get; set; }

    }

    [Table("BarragemView")]
    public class BarragemView
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "nome")]
        public string nome { get; set; }

        [Display(Name = "link pagSeguro")]
        public string linkPagSeguro { get; set; }

        [Display(Name = "situação")]
        public bool isAtiva { get; set; }

        [Display(Name = "em teste")]
        public bool isTeste { get; set; }

        [Display(Name = "Classe única")]
        public bool isClasseUnica { get; set; }
        [Display(Name = "Domínio")]
        public string dominio { get; set; }
        public string email { get; set; }
        public bool? soTorneio { get; set; }
        public string emailPagSeguro { get; set; }
        public string tokenPagSeguro { get; set; }
        [Display(Name = "Cidade")]
        public string cidade { get; set; }
        public double? valorPorUsuario { get; set; }
        public string cpfResponsavel { get; set; }
        public string nomeResponsavel { get; set; }
        [Display(Name = "Suspensao por atraso")]
        public bool suspensaoPorAtraso { get; set; }
        [Display(Name = "Suspensao por WO")]
        public bool suspensaoPorWO { get; set; }
    }
}