using System;
using System.Web;

namespace Barragem.Helper
{
    public static class SessionHelper
    {
        public static int ObterIdBarragemUsuarioLogado(this HttpRequestBase request, bool podeRetornarErro = true)
        {
            HttpCookie cookie = request.Cookies["_barragemId"];
            if (cookie == null && podeRetornarErro)
            {
                throw new System.Exception("Não foi possível recuperar o identificador da barragem");
            }
            else if (cookie == null && !podeRetornarErro)
            {
                return 0;
            }
            else
            {
                int.TryParse(cookie.Value.ToString(), out int idBarragem);
                return idBarragem;
            }
        }

        public static int ObterIdBarragem(this HttpRequestBase request)
        {
            return request.ObterIdBarragemUsuarioLogado(false);
        }
    }
}