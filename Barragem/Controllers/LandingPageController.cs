using Barragem.Class;
using Barragem.Context;
using Barragem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Controllers
{
    public class LandingPageController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        private const string MSG_DOMINIO_NAO_ENCONTRADO = "Desculpe mas não encontramos um ranking com esse nome. Favor verificar se o nome do ranking foi digitado corretamente.";

        public ActionResult Index(int id)
        {
            return View();
        }

        public ActionResult LigaRedirect(string key)
        {
            var barragem = BuscarBarragemPorDominio(key, "liga");
            if (barragem == null)
                return RedirectToAction("Index", "Home", new { msg = MSG_DOMINIO_NAO_ENCONTRADO });

            Funcoes.CriarCookieBarragem(Response, Server, barragem.Id, barragem.nome);
            return RedirectToAction("Index", "LandingPage", new { id = barragem.Id });

        }

        public ActionResult CircuitoRedirect(string key)
        {
            var barragem = BuscarBarragemPorDominio(key, "circuito");
            if (barragem == null)
                return RedirectToAction("Index", "Home", new { msg = MSG_DOMINIO_NAO_ENCONTRADO });

            return RedirectToAction("Index", "LandingPage", new { id = 0 });
        }

        public ActionResult FederacaoRedirect(string key)
        {
            var barragem = BuscarBarragemPorDominio(key, "federacao");
            if (barragem == null)
                return RedirectToAction("Index", "Home", new { msg = MSG_DOMINIO_NAO_ENCONTRADO });

            return RedirectToAction("Index", "LandingPage", new { id = 0 });
        }

        private BarragemView BuscarBarragemPorDominio(string dominio, string nomePaginaEspecial)
        {

            //TODO: quando criar o campo de nomePaginaEspecial no BD filtrar na querie da view, necessário atualizar view
            return db.BarragemView.FirstOrDefault(b => b.dominio.ToLower().Equals(dominio.ToLower()));
        }

    }
}