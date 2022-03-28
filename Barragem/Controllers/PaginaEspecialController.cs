using Barragem.Class;
using Barragem.Context;
using Barragem.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Barragem.Controllers
{
    public class PaginaEspecialController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        private const string MSG_DOMINIO_NAO_ENCONTRADO = "Desculpe mas não encontramos um ranking com esse nome. Favor verificar se o nome do ranking foi digitado corretamente.";

        public ActionResult Index(int idBarragem, EnumPaginaEspecial idPaginaEspecial)
        {
            var barragem = BuscarBarragemPorId(idBarragem, idPaginaEspecial);
            var patrocinadores = BuscarPatrocinadores();
            var model = new PaginaEspecialModel()
            {
                TipoPaginaEspecial = idPaginaEspecial,
                IdBarragem = barragem.Id,
                NomeBarragem = barragem.nome,
                Regulamento = barragem.regulamento,
                Contato = barragem.contato,
                Patrocinadores = patrocinadores,
                TituloFilieSeOuQuemSomos = idPaginaEspecial == EnumPaginaEspecial.Federacao ? "Filie-se" : "Quem Somos",
                TextoFilieSeOuQuemSomos = barragem.quemsomos

            };

            return View(model);
        }

        public ActionResult LigaRedirect(string key)
        {
            var barragem = BuscarBarragemPorDominio(key, EnumPaginaEspecial.Liga);
            if (barragem == null)
                return RedirectToAction("Index", "Home", new { msg = MSG_DOMINIO_NAO_ENCONTRADO });
            //Funcoes.CriarCookieBarragem(Response, Server, barragem.Id, barragem.nome);
            return RedirectToAction("Index", "PaginaEspecial", new { idBarragem = barragem.Id, idPaginaEspecial = (int)EnumPaginaEspecial.Liga });
        }

        public ActionResult CircuitoRedirect(string key)
        {
            var barragem = BuscarBarragemPorDominio(key, EnumPaginaEspecial.Circuito);
            if (barragem == null)
                return RedirectToAction("Index", "Home", new { msg = MSG_DOMINIO_NAO_ENCONTRADO });

            return RedirectToAction("Index", "PaginaEspecial", new { idBarragem = barragem.Id, idPaginaEspecial = (int)EnumPaginaEspecial.Circuito });
        }

        public ActionResult FederacaoRedirect(string key)
        {
            var barragem = BuscarBarragemPorDominio(key, EnumPaginaEspecial.Federacao);
            if (barragem == null)
                return RedirectToAction("Index", "Home", new { msg = MSG_DOMINIO_NAO_ENCONTRADO });

            return RedirectToAction("Index", "PaginaEspecial", new { idBarragem = barragem.Id, idPaginaEspecial = (int)EnumPaginaEspecial.Federacao });
        }

        private BarragemView BuscarBarragemPorDominio(string dominio, EnumPaginaEspecial idPaginaEspecial)
        {
            return db.BarragemView
                        .FirstOrDefault(b => b.dominio.Equals(dominio, System.StringComparison.OrdinalIgnoreCase) && b.PaginaEspecialId == (int)idPaginaEspecial);
        }

        private Barragens BuscarBarragemPorId(int id, EnumPaginaEspecial idPaginaEspecial)
        {
            return db.Barragens
                        .FirstOrDefault(b => b.Id == id && b.PaginaEspecialId == (int)idPaginaEspecial);
        }

        private List<Patrocinio> BuscarPatrocinadores()
        {
            return db.Patrocinio.ToList();
        }
    }
}