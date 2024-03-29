﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Barragem
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Barragens",
                url: "ranking-{id}",
                defaults: new { controller = "Home", action = "IndexBarragem", id = UrlParameter.Optional }
            );

            routes.MapRoute(
               name: "Torneios",
               url: "torneio-{id}",
               defaults: new { controller = "Home", action = "IndexTorneioRedirect", id = UrlParameter.Optional }
           );

            routes.MapRoute(
               name: "PaginaEspecialLiga",
               url: "liga-{key}",
               defaults: new { controller = "PaginaEspecial", action = "LigaRedirect", key = UrlParameter.Optional }
           );

            routes.MapRoute(
               name: "PaginaEspecialCircuito",
               url: "circuito-{key}",
               defaults: new { controller = "PaginaEspecial", action = "CircuitoRedirect", key = UrlParameter.Optional }
           );

            routes.MapRoute(
               name: "PaginaEspecialFederacao",
               url: "federacao-{key}",
               defaults: new { controller = "PaginaEspecial", action = "FederacaoRedirect", key = UrlParameter.Optional }
           );
            
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

        }
    }
}