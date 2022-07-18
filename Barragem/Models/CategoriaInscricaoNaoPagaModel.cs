using System;
using System.Collections.Generic;

namespace Barragem.Models
{
    public class CategoriaInscricaoNaoPagaModel
    {
        public CategoriaInscricaoNaoPagaModel()
        {
            Jogadores = new List<string>();
        }
        public int IdCategoria { get; set; }
        public string NomeCategoria { get; set; }
        public bool EhDupla { get; set; }
        public List<string> Jogadores { get; set; }
    }
}