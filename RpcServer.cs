using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace rpc_base
{
    public abstract class RpcServer: IDisposable,IRpcServer<string>
    {
        private static string _consumerEndpoint;
        private static string _hostName;
        private EventHandler<BasicDeliverEventArgs> _consumerOnReceived;
        private EventingBasicConsumer _consumer;
        public RpcServer(Dictionary<string, string> config)
        {
            _consumerEndpoint = config["ConsumerEnpoint"];
            _hostName = config["HostName"];
        }
        public void startServer()
        {
            var factory = new ConnectionFactory() {HostName = _hostName};
            using (var connection = factory.CreateConnection())
                
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue:_consumerEndpoint, durable: false,
                    exclusive: false, autoDelete: false, arguments: null);
                channel.BasicQos(0, 1, false);
                _consumer = new EventingBasicConsumer(channel);
                channel.BasicConsume(queue: _consumerEndpoint,
                    autoAck: false, consumer: _consumer);
                
                Console.WriteLine($"[x] Awaiting RPC requests at {_consumerEndpoint}");

                _consumerOnReceived = async (model, ea) =>
                {
                    string response = null;

                    var body = ea.Body;
                    var props = ea.BasicProperties;
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;

                    try
                    {
                        var message = Encoding.UTF8.GetString(body);
                        response = await onMessage(message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(" [.] " + e.Message);
                        response = "";
                    }
                    finally
                    {
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        channel.BasicPublish(exchange: "", routingKey: props.ReplyTo,
                            basicProperties: replyProps, body: responseBytes);
                        channel.BasicAck(deliveryTag: ea.DeliveryTag,
                            multiple: false);
                    }
                };
                _consumer.Received += _consumerOnReceived;

                Thread.Sleep(Timeout.Infinite); // sleep forever
            }
        }

        public void Dispose()
        {
            Console.WriteLine($"[x] Close RPC Server at {_consumerEndpoint}");
        }

        public abstract Task<string> onMessage(string message);
    }
}