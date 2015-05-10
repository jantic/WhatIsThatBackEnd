using System.Collections.Generic;
using System.Web.Http.Controllers;
using Microsoft.WindowsAzure.Mobile.Service;
using whatisthatService.DataObjects;
using whatisthatService.Models;

namespace whatisthatService.Controllers
{
    public class SpeciesIdentifierController : TableController<TodoItem>
    {
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            var context = new WhatIsThatContext();
            DomainManager = new EntityDomainManager<TodoItem>(context, Request, Services);
        }

        // GET tables/GetSpeciesCandidates
        public List<SpeciesCandidate> GetSpeciesCandidates()
        {
            var candidates = new List<SpeciesCandidate> {new SpeciesCandidate("Derp", "Derpus Maximus", 1.0)};
            return candidates;
        }
    }
}