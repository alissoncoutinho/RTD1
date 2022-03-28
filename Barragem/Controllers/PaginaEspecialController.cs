using Barragem.Class;
using Barragem.Context;
using Barragem.Models;
using System.Linq;
using System.Web.Mvc;

namespace Barragem.Controllers
{
    public class PaginaEspecialController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        private const string MSG_DOMINIO_NAO_ENCONTRADO = "Desculpe mas não encontramos um ranking com esse nome. Favor verificar se o nome do ranking foi digitado corretamente.";

        public ActionResult Index(int id)
        {
            return View();
        }

        public ActionResult LigaRedirect(string key)
        {
            var barragem = BuscarBarragemPorDominio(key, EnumPaginaEspecial.Liga);
            if (barragem == null)
                return RedirectToAction("Index", "Home", new { msg = MSG_DOMINIO_NAO_ENCONTRADO });

            Funcoes.CriarCookieBarragem(Response, Server, barragem.Id, barragem.nome);
            return RedirectToAction("Index", "PaginaEspecial", new { id = barragem.Id });
        }

        public ActionResult CircuitoRedirect(string key)
        {
            var barragem = BuscarBarragemPorDominio(key, EnumPaginaEspecial.Circuito);
            if (barragem == null)
                return RedirectToAction("Index", "Home", new { msg = MSG_DOMINIO_NAO_ENCONTRADO });

            return RedirectToAction("Index", "PaginaEspecial", new { id = barragem.Id });
        }

        public ActionResult FederacaoRedirect(string key)
        {
            var barragem = BuscarBarragemPorDominio(key, EnumPaginaEspecial.Federacao);
            if (barragem == null)
                return RedirectToAction("Index", "Home", new { msg = MSG_DOMINIO_NAO_ENCONTRADO });

            return RedirectToAction("Index", "PaginaEspecial", new { id = barragem.Id });
        }

        private BarragemView BuscarBarragemPorDominio(string dominio, EnumPaginaEspecial idPaginaEspecial)
        {
            return db.BarragemView
                        .FirstOrDefault(b => b.dominio.Equals(dominio, System.StringComparison.OrdinalIgnoreCase) && b.PaginaEspecialId == (int)idPaginaEspecial);
        }

    }
}