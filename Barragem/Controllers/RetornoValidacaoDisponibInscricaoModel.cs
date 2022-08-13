using System.Collections.Generic;

namespace Barragem.Controllers
{
    public class RetornoValidacaoDisponibInscricaoModel
    {
        public RetornoValidacaoDisponibInscricaoModel()
        {
            conteudo = new List<FormacaoDuplaInscricao>();
        }

        public RetornoValidacaoDisponibInscricaoModel(string mensagemErro)
        {
            conteudo = new List<FormacaoDuplaInscricao>();
            AplicarStatusErro(mensagemErro);
        }

        public List<FormacaoDuplaInscricao> conteudo { get; set; }
        public string erro { get; private set; }
        public string status { get; private set; }

        public void AplicarStatusEsgotado()
        {
            status = "ESGOTADO";
        }

        public void AplicarStatusOk()
        {
            status = "OK";
        }

        public void AplicarStatusEscolhaDupla(bool temVagas)
        {
            status = temVagas ? "ESCOLHER_DUPLA_OK" : "ESCOLHER_DUPLA";
        }

        public void AplicarStatusErro(string mensagemErro)
        {
            status = "ERRO";
            erro = mensagemErro;
            conteudo.Clear();
        }
    }
}