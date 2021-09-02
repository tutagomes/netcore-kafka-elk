using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Iniciador.Models;
using MassTransit.KafkaIntegration;

namespace Iniciador.Controllers
{
    public class Mensagem {
        
        public string Type { get; set; }

		public Guid MessageId { get; set; }

		public string Message { get; set; }
        
        public DateTimeOffset Timestamp { get; set; }

    }
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITopicProducer<Mensagem> _producer;

        public HomeController(ILogger<HomeController> logger, ITopicProducer<Mensagem> producer)
        {
            _logger = logger;
            _producer = producer;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [Route("/")]
        public IActionResult Criar()
        {
            var generatedGuid = Guid.NewGuid(); 
            _logger.LogInformation("Recebida mensagem de ID: '" + generatedGuid + "'");

            _logger.LogInformation("Criando Publicacao");
            var mensagem = new Mensagem{
                Type = "Inicio",
                MessageId = generatedGuid,
                Message = "Mensagem Aleatoria",
                Timestamp = new DateTimeOffset()
            };
            _producer.Produce(mensagem);
            _logger.LogInformation("Publicacao de ID: '" + generatedGuid + "' Feita");
            return Ok();
        }
    }
}
