using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Barragem.Models;
using Barragem.Context;
using System.Transactions;
using Newtonsoft.Json;

namespace Barragem.Controllers
{
    public class CidadeController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();


        public ActionResult getCidade(string q)
        {
           var cidades = db.Cidade.Where(c=> c.nome.ToUpper().StartsWith(q.ToUpper())).ToList();
            List<CidadeAutocomplete> listCidade = new List<CidadeAutocomplete>();
            foreach (var cidade in cidades)
            {
               listCidade.Add(new CidadeAutocomplete { id = cidade.Id, label = cidade.nome +"-"+cidade.uf, value = cidade.nome + "-" + cidade.uf });
            }
            string json = JsonConvert.SerializeObject(listCidade, Formatting.Indented);
            json = HttpContext.Request.Params["callback"] + "(" + json + ")";
            //return Json(json, JsonRequestBehavior.AllowGet);
            return new ContentResult { Content = json, ContentType = "application/json" };
        }


        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}