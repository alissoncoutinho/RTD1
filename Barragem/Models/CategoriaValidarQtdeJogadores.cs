namespace Barragem.Models
{
    public class CategoriaValidarQtdeJogadores
    {
        public int IdTorneio { get; set; }
        public int IdClasse { get; set; }
        public string NomeClasse { get; set; }
        public int QtdeInscricoes { get; set; }
        public TipoValidacaoCategoria Tipo { get; set; }
    }

    public enum TipoValidacaoCategoria
    {
        MENOS_DE_SEIS_JOGADORES = 1,
        MAIS_DE_CINCO_JOGADORES = 2
    }

    public class CategoriaValidarQtdeJogadoresRequestModel
    {
        public int IdTorneio { get; set; }
        public int IdClasse { get; set; }
        public int ConfigSelecionadaClasse { get; set; }
    }


}