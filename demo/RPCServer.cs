using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;

namespace rpc_base.demo
{
    public class RPCServer: IDisposable
    {
        private static string _consumerEndpoint;
        private static string _hostName;
        private EventHandler<BasicDeliverEventArgs> _consumerOnReceived;
        private EventingBasicConsumer _consumer;
        public RPCServer(Dictionary<string, string> config)
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

                _consumerOnReceived = (model, ea) =>
                {
                    string response = null;

                    var body = ea.Body;
                    var props = ea.BasicProperties;
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;

                    try
                    {
                        var message = Encoding.UTF8.GetString(body);
                        int n = int.Parse(message);
                        Console.WriteLine(" [.] fib({0})", message);
                        response = fib(n).ToString();
                        Thread.Sleep(3000);
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
        
        private static int fib(int n)
        {
            if (n == 0 || n == 1)
            {
                return n;
            }

            return fib(n - 1) + fib(n - 2);
        }

        public void Dispose()
        {
            
        }
    }
}