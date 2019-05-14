using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace rpc_base.demo
{
    public class RpcClient: IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _replyQueueName;
        private readonly EventingBasicConsumer _consumer;
        private readonly BlockingCollection<string> _respQueue = new BlockingCollection<string>();
        private readonly IBasicProperties _props;

        private readonly string _producerEndpoint;
        private readonly string _hostName;
        private EventHandler<BasicDeliverEventArgs> _consumerOnReceived;

        public RpcClient(Dictionary<string, string> config)
        {
            _producerEndpoint = config["ProducerEnpoint"];
            _hostName = config["HostName"];

            var factory = new ConnectionFactory() {HostName = _hostName};

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _replyQueueName = _channel.QueueDeclare().QueueName;
            _consumer = new EventingBasicConsumer(_channel);

            _props = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            _props.CorrelationId = correlationId;
            _props.ReplyTo = _replyQueueName;

            Console.WriteLine($"rpc client start at {_producerEndpoint}");
            _consumerOnReceived = (model, ea) =>
            {
                var body = ea.Body;
                var response = Encoding.UTF8.GetString(body);
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    _respQueue.Add(response);
                }
            };
            _consumer.Received += _consumerOnReceived;
        }

        private string Call(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(
                exchange: "",
                routingKey: _producerEndpoint,
                basicProperties: _props,
                body: messageBytes);

            _channel.BasicConsume(
                consumer: _consumer,
                queue: _replyQueueName,
                autoAck: true);

            return _respQueue.Take();
        }

        public void Close()
        {
            _connection.Close();
        }

        public async Task<string> CallAsync(string input)
        {
            var task = await Task.Run(() => Call(input) );
            return task;
        }

        public void Dispose()
        {
            Console.WriteLine($"rpc client closed at {_producerEndpoint}");
            Close();
        }
    }
}