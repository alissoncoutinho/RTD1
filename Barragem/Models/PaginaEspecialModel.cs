using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
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
        public string AnoCalendario { get { return DateTime.Now.ToString("yy"); } }
        public List<RankingModel> Rankings { get; set; }
        public List<CalendarioModalidades> ModalidadesCalendario { get; set; }
        
        public class RankingModel
        {
            public int IdModalidade { get; set; }
            public string Modalidade { get; set; }
            public RankingModalidade Ranking { get; set; }
            public DateTime DataRanking { get; set; }

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

        public class CalendarioModalidades
        {
            public int IdModalidade { get; set; }
            public string Modalidade { get; set; }
        }

        public class CalendarioTorneioMes
        {
            public List<CalendarioTorneioItem> Torneios { get; set; }
        }

        public class CalendarioTorneioItem
        {
            public string Modalidade { get; set; }
            public string Nome { get; set; }
            public int Pontuacao { get; set; }
            public string StatusInscricaoTorneio { get; set; }
            public int IdStatusInscricaoTorneio { get; set; }
            public string Local { get; set; }
            public string LinkInscricao { get; set; }
            public DateTime DataInicial { get; set; }
            public DateTime DataFinal { get; set; }
            public string MesAbreviado
            {
                get
                {
                    CultureInfo culture_info = Thread.CurrentThread.CurrentCulture;
                    TextInfo text_info = culture_info.TextInfo;
                    return text_info.ToTitleCase(DataInicial.ToString("MMM"));
                }
            }
            public int Dia { get { return DataInicial.Day; } }
        }

    }
}