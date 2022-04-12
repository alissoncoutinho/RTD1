using Barragem.Helper;
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
        public TorneioDestaqueBanner Banner { get; set; }
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

            private string _linkInscricao;
            public string LinkInscricao
            {
                get { return _linkInscricao.ToHttp(); }
                set { _linkInscricao = value; }
            }

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

        public class TorneioDestaqueBanner
        {
            public List<TorneioDestaqueBannerItem> TorneiosBanner { get; set; }

            public class TorneioDestaqueBannerItem
            {
                public string Nome { get; set; }
                public string DataInicial { get; set; }
                public string DataFinal { get; set; }
                public string LinkInscricao { get; set; }
                public EnumStatusInscricao IdStatusInscricaoTorneio { get; set; }
                public EnumModalidadeTorneio IdModalidade { get; set; }
                public int Pontuacao { get; set; }
                public string Local { get; set; }
                public string UrlImagemBanner { get; set; }
                public string UrlImagemBannerMobile { get; set; }
            }
        }

        public class ImagemBanner
        {
            private List<ImagemBannerItem> _imagens;

            public ImagemBanner()
            {
                _imagens = new List<ImagemBannerItem>()
                {
                    {new ImagemBannerItem(){ Modalidade=EnumModalidadeTorneio.TENIS, UrlImagem="/Content/paginaespecial/images/banner-1.png",UrlImagemMobile="/Content/paginaespecial/images/mobile-1.png" } },
                    {new ImagemBannerItem(){ Modalidade=EnumModalidadeTorneio.TENIS, UrlImagem="/Content/paginaespecial/images/banner-4.png",UrlImagemMobile="/Content/paginaespecial/images/mobile-4.png" } },
                    {new ImagemBannerItem(){ Modalidade=EnumModalidadeTorneio.TENIS, UrlImagem="/Content/paginaespecial/images/banner-5.png",UrlImagemMobile="/Content/paginaespecial/images/mobile-5.png" } },
                    {new ImagemBannerItem(){ Modalidade=EnumModalidadeTorneio.BEACH_TENNIS, UrlImagem="/Content/paginaespecial/images/banner-2.png",UrlImagemMobile="/Content/paginaespecial/images/mobile-2.png" } },
                    {new ImagemBannerItem(){ Modalidade=EnumModalidadeTorneio.BEACH_TENNIS, UrlImagem="/Content/paginaespecial/images/banner-6.png",UrlImagemMobile="/Content/paginaespecial/images/mobile-9.png" } },
                    {new ImagemBannerItem(){ Modalidade=EnumModalidadeTorneio.BEACH_TENNIS, UrlImagem="/Content/paginaespecial/images/banner-7.png",UrlImagemMobile="/Content/paginaespecial/images/mobile-8.png" } },
                    {new ImagemBannerItem(){ Modalidade=EnumModalidadeTorneio.KIDS, UrlImagem="/Content/paginaespecial/images/banner-3.png",UrlImagemMobile="/Content/paginaespecial/images/mobile-7.png" } },
                    {new ImagemBannerItem(){ Modalidade=EnumModalidadeTorneio.KIDS, UrlImagem="/Content/paginaespecial/images/banner-8.png",UrlImagemMobile="/Content/paginaespecial/images/mobile-3.png" } },
                    {new ImagemBannerItem(){ Modalidade=EnumModalidadeTorneio.KIDS, UrlImagem="/Content/paginaespecial/images/banner-9.png",UrlImagemMobile="/Content/paginaespecial/images/mobile-6.png" } }
                };
            }

            public ImagemBannerItem ObterImagemBanner(EnumModalidadeTorneio modalidade)
            {
                var random = new Random();
                var imagensDisponiveis = _imagens.Where(x => x.UltimaUtilizada == false && x.Modalidade == modalidade).OrderBy(o => o.TotalUsos).ToList();
                var numeroImagem = random.Next(1, imagensDisponiveis.Count());

                var imagemEscolhida = imagensDisponiveis[numeroImagem - 1];

                AtualizarDadosLista(modalidade, imagemEscolhida);

                return imagemEscolhida;
            }

            private void AtualizarDadosLista(EnumModalidadeTorneio modalidade, ImagemBannerItem imagemEscolhida)
            {
                imagemEscolhida.UltimaUtilizada = true;
                imagemEscolhida.TotalUsos++;

                foreach (var item in _imagens.Where(x => x.Modalidade == modalidade && x.UrlImagem != imagemEscolhida.UrlImagem))
                {
                    item.UltimaUtilizada = false;
                }
            }

            public class ImagemBannerItem
            {
                public EnumModalidadeTorneio Modalidade { get; set; }
                public string UrlImagem { get; set; }
                public string UrlImagemMobile { get; set; }
                public short TotalUsos { get; set; }
                public bool UltimaUtilizada { get; set; }
            }
        }
    }
}