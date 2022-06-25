﻿namespace Barragem.Models
{
    public class DadosAlteracaoParceiroDuplaModel
    {
        public int JogadorAlterado { get; set; }
        public int IdJogador { get; set; }
        public int IdTorneio { get; set; }
        public int IdClasse { get; set; }
    }

    public class DadosModalAlteracaoParceiroDuplaModel
    {
        public int IdTorneio { get; set; }
        public int IdClasse { get; set; }
    }
}