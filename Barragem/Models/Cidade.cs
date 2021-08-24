using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Models
{
    [Table("Cidade")]
    public class Cidade
    {
        public int Id { get; set; }
        public string nome { get; set; }
        public string uf { get; set; }
    }

    public class CidadeAutocomplete
    {
        public int id { get; set; }
        public string label { get; set; }
        public string value { get; set; }
    }


    public class CidadeTemp
    {
        public int id { get; set; }
        public string nome { get; set; }
        public microrregiao microrregiao { get; set; }
    }


    public class microrregiao
    {
        public int id { get; set; }
        public mesorregiao mesorregiao { get; set; }
    }

    public class mesorregiao
    {
        public int id { get; set; }
        public UF uf { get; set; }
    }

    public class UF
    {
        public int id { get; set; }
        public string sigla { get; set; }
    }
}