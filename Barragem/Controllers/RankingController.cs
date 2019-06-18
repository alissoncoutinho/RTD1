﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Barragem.Models;
using Barragem.Context;
using WebMatrix.WebData;

namespace Barragem.Controllers
{
    [Authorize]
    public class RankingController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();

        //
        // GET: /Rancking/
        [AllowAnonymous]
        public ActionResult Index(int id=0, string nome="")
        {
            List<Rancking> rancking;
            BarragemView bV = null;
            try{
                if ((id == 0) && (nome == "") && (User.Identity.Name != "")){
                    UserProfile usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                    bV = usuario.barragem;
                    id = db.Rancking.Where(r => r.rodada.isAberta == false && r.rodada.isRodadaCarga == false && r.rodada.barragemId == bV.Id).Max(r => r.rodada_id);
                    ViewBag.IdBarragem = bV.Id;
                    ViewBag.NomeBarragem = bV.nome;
                }else if (nome != ""){
                    bV = db.BarragemView.Where(b => b.dominio == nome).FirstOrDefault();
                    id = db.Rancking.Where(r => r.rodada.isAberta == false && r.rodada.isRodadaCarga == false && r.rodada.barragemId == bV.Id).Max(r => r.rodada_id);
                    ViewBag.IdBarragem = bV.Id;
                    ViewBag.NomeBarragem = bV.nome;
                }else if (id == 0){
                    return RedirectToAction("Login", "Account");
                }else{
                    HttpCookie cookie = new HttpCookie("_barragemId");
                    var barragemId = Convert.ToInt32(cookie.Value.ToString());
                    bV = db.BarragemView.Find(barragemId);
                }
            }
            catch (Exception e){
                return RedirectToAction("Login", "Account");
            }
            rancking = db.Rancking.Include(r => r.userProfile).Include(r => r.rodada).
                Where(r => r.rodada_id == id && r.posicao > 0 && r.userProfile.situacao != "desativado" && r.userProfile.situacao != "inativo").OrderBy(r=>r.classe.nivel).ThenBy(r => r.posicaoClasse).ToList();
            ViewBag.Classes = db.Classe.Where(c => c.barragemId == bV.Id && c.ativa == true).ToList();
            if (rancking.Count() > 0){
                var barragem = rancking[0].rodada.barragemId;
                ViewBag.Rodada = rancking[0].rodada.codigoSeq;
                ViewBag.RodadaId = rancking[0].rodada.Id;
                ViewBag.dataRodada = (rancking[0].rodada.dataFim + "").Substring(0, 10);

                if (rancking[0].rodada.temporadaId != null){
                    var temporadaId = rancking[0].rodada.temporadaId;
                    var rodadaId = rancking[0].rodada.Id;
                    var qtddRodada = db.Rodada.Where(r => r.temporadaId == temporadaId && r.Id <= rodadaId && r.barragemId==barragem).Count();
                    ViewBag.Temporada = rancking[0].rodada.temporada.nome + " - Rodada " + qtddRodada + " de " + 
                        rancking[0].rodada.temporada.qtddRodadas;
                } else { ViewBag.Temporada = ""; }
            }

            return View(rancking);
        }

        
       public ActionResult listarRancking(int idRodadaAtual) {
           UserProfile usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
           int barragemId = 0;
           if (usuario==null){
               if (Request.Cookies["_barragemId"] != null){
                   HttpCookie cookie = new HttpCookie("_barragemId");
                   barragemId = Convert.ToInt32(cookie.Value.ToString());
               } else {
                   barragemId = 1; 
               }
           } else {
               barragemId = usuario.barragemId;
           }
           var listaRancking = db.Rodada.Where(c=>c.isAberta==false && c.isRodadaCarga==false && c.Id!=idRodadaAtual && c.barragemId == barragemId).OrderByDescending(c=>c.Id).ToList();
           return View(listaRancking);
        }

        public ActionResult historicoRanking(int idJogador)
        {
            List<Rancking> ranckingJogador = db.Rancking.Where(r => r.userProfile_id == idJogador && r.rodada.isRodadaCarga == false).OrderByDescending(r => r.rodada_id).ToList();
            return View(ranckingJogador);
        }

        public ActionResult RegraPontuacao()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}