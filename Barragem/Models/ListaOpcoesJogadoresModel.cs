﻿using System.Collections.Generic;

namespace Barragem.Models
{
    public class ListaOpcoesJogadoresModel
    {
        public bool EhGrupoUnico { get; set; }
        public List<DadosJogosModel> Jogos { get; set; }
        public List<AutoCompleteOption> OpcoesJogador { get; set; }

        public class DadosJogosModel 
        {
            public int JogoId { get; set; }
            public int IdDesafiante { get; set; }
            public int IdDesafiado { get; set; }
        }
    }
}