using Barragem.Context;
using System;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Security;
using WebMatrix.WebData;
using Barragem.Models;
using System.Data;
using System.Data.Entity;
using System.Linq;


namespace Barragem.Helpers
{
    public static class HtmlHelpers
    {
        public static MvcHtmlString MontarJogo(this HtmlHelper htmlHelper, Jogo jogo=null)
        {
            string retorno = "teste";
            return new MvcHtmlString(retorno);
        }

    }
    
}
