using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MassTransit;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using MassTransit.KafkaIntegration;

namespace Observador.Consumidor
{
    public class Mensagem {

        public string Type { get; set; }

		public Guid MessageId { get; set; }

		public string Message { get; set; }
        
        public DateTimeOffset Timestamp { get; set; }
    }
    public class MensagemErro {

        public int CodigoErro { get; set; }
        
        public string Type { get; set; }

		public Guid MessageId { get; set; }

		public string Message { get; set; }
        
        public DateTimeOffset Timestamp { get; set; }
    }
    // Classe responsável por consumir a nossa mensagem. 
    public class TopicConsumer :
        IConsumer<Mensagem>
    {
        readonly ILogger<TopicConsumer> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ITopicProducer<MensagemErro> _producer;

        public TopicConsumer(ILogger<TopicConsumer> logger, IServiceScopeFactory serviceScopeFactory, ITopicProducer<MensagemErro> producer)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _producer = producer;
        }

        public Task Consume(ConsumeContext<Mensagem> context)
        {
            // Recuperando o Context de Recebimento para acessarmos a Chave da mensagem, assim como outras propriedades como OffSet
            var receiveContext = (MassTransit.KafkaIntegration.Contexts.KafkaReceiveContext<Confluent.Kafka.Ignore, Mensagem>)context.ReceiveContext;
            // Como o isValid não existe na mensagem original, sempre retornará o valor padrão, False
            _logger.LogInformation("Recebida Mensagem de ID: {Text} - Conteudo: {Text}", context.Message.MessageId, context.Message.Message);

            var mensagem = new MensagemErro();
            Random rnd = new Random();
            
            // Gerando probabilidade de erro
            var prob = rnd.Next(1, 11);
            if (prob > 5) {
                // Busca o serviço Scoped para obtenção do Bus de Produção de Mensagem
                // using var scope = _serviceScopeFactory.CreateScope();
                // Recupera o serviço responsável por produzir mensagens do tipo <string, Mensagem>
                // var producer = scope.ServiceProvider.GetService<ITopicProducer<MensagemErro>>();
                _logger.LogError("Erro ao processar mensagem de ID: {Text}! Enviado para fila de erros...", context.Message.MessageId);
                mensagem.Type = context.Message.Type;
                mensagem.MessageId = context.Message.MessageId;
                mensagem.Timestamp = context.Message.Timestamp;
                mensagem.Message = context.Message.Message;
                mensagem.CodigoErro = prob;
                _producer.Produce(mensagem);
                _logger.LogInformation("Publicacao de Erro em ID: '" + context.Message.MessageId + "' Feita");
            }

            return Task.CompletedTask;
        }
    }
}
