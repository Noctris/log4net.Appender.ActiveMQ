using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Log4net;
using log4net.Config;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
namespace Log4net.ActiveMQAppender.Demo
{
    class Program
    {
        private static IConnectionFactory _factory;
        private static IConnection _connection;
        private static ISession _session;
        private static IMessageConsumer _consumer;
        private static IDestination _topics = new ActiveMQTopic("TP2.>");
        static void Main(string[] args)
        {

            XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("log4net.xml"));
            _factory = new ConnectionFactory("failover:(tcp://mq01.tp2net.lan:1414)");
            using (_connection = _factory.CreateConnection())
            {

                Console.WriteLine("Starting Connection");
                _connection.Start();
                using (_session = _connection.CreateSession())
                {
                    using (_consumer = _session.CreateConsumer(_topics, "2>1"))
                    {

                        _consumer.Listener += new MessageListener(_consumer_Listener);
                        while (true)
                        {
                            
                            Console.WriteLine("Type something to log");
                            string _input = Console.ReadLine();
                            if (!_input.StartsWith("quit"))
                            {
                                for (int i = 0; i < 6; i++)
                                {
                                    log4net.LogManager.GetLogger("MyLogger").Error(_input);    
                                }
                                
                            }
                            else
                            {
                                break;
                            }

                        }


                    }




                }




            }
            
        }

        static void _consumer_Listener(IMessage message)
        {
            log4net.Core.LoggingEvent _event = (message as IObjectMessage).Body as log4net.Core.LoggingEvent;

          
            Console.WriteLine("RX LogEvent: {0} - {1} - {2} - {3} - {4}", _event.Properties["log4net:HostName"], _event.TimeStamp, _event.Level, _event.LoggerName, _event.RenderedMessage);
            foreach (System.Collections.DictionaryEntry o in _event.Properties)
            {
                Console.WriteLine("Property {0} - Value {1}", o.Key, o.Value);
               
                
            }


        }
    }
}
