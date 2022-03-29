using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public class PaginaEspecialModel
    {
        
        public EnumPaginaEspecial TipoPaginaEspecial { get; set; }
        public int IdBarragem { get; set; }
        public string NomeBarragem { get; set; }
        public List<Patrocinio> Patrocinadores { get; set; }
        public string Regulamento { get; set; }
        public string Contato { get; set; }
        public string UrlLogo { get; set; }
        public string TextoFilieSeOuQuemSomos { get; set; }
        public string TituloFilieSeOuQuemSomos { get; set; }
        public string ImagemRodape { get; set; }

        public List<RankingModel> Rankings { get; set; }

        public class RankingModel
        {
            public int IdModalidade { get; set; }
            public string Modalidade { get; set; }
            public RankingModalidade Ranking { get; set; }

            public class RankingModalidade
            {
                public string NomeLiga { get; set; }
                public List<CategoriaModalidade> Categoria { get; set; }

                public class CategoriaModalidade
                {
                    public int IdCategoria { get; set; }
                    public string NomeCategoria { get; set; }
                    public List<Jogador> Jogadores { get; set; }

                    public class Jogador 
                    {
                        public int Posicao { get; set; }
                        public string NomeJogador { get; set; }
                        public int Pontuacao { get; set; }
                    }
                }
            }
        }
    }
}