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
               name: "LandingPageLiga",
               url: "liga-{key}",
               defaults: new { controller = "LandingPage", action = "LigaRedirect", key = UrlParameter.Optional }
           );

            routes.MapRoute(
               name: "LandingPageCircuito",
               url: "circuito-{key}",
               defaults: new { controller = "LandingPage", action = "CircuitoRedirect", key = UrlParameter.Optional }
           );

            routes.MapRoute(
               name: "LandingPageFederacao",
               url: "federacao-{key}",
               defaults: new { controller = "LandingPage", action = "FederacaoRedirect", key = UrlParameter.Optional }
           );
            
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

        }
    }
}