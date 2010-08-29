
namespace log4net.Appender
{
    using System;
    using log4net.Core;
    using log4net.Appender;
    using Apache.NMS;
    using Apache.NMS.ActiveMQ;
    using Apache.NMS.ActiveMQ.Commands;
    using System.Threading;
    using log4net.Util;
    /// <summary>
    /// Send a message on an ActiveMQ topic when a specific logging event occurs
    /// </summary>
    /// <remarks>
    /// <para>
    /// The number of logging events buffered before starting to send depend on 
    /// the value of <see cref="BufferingAppenderSkeleton.BufferSize"/> option.
    /// the <see cref="ActiveMQAppender"/> keeps only the last
    /// <see cref="BufferingAppenderSkeleton.BufferSize"/> logging events in its buffer.
    /// This keeps memory requirements at a reasonable level.
    /// </para>
    /// </remarks>
    /// <author>Michel Van Hoof</author>
    public class ActiveMQAppender : BufferingAppenderSkeleton
    {
        

        public ActiveMQAppender()
        {
            LogLog.Debug("Starting an ActiveMQAppender");
        }
        #region Implementation of IOptionHandler

        public override void ActivateOptions()
        {
           
            base.ActivateOptions();
            //setup the activemq connection 
            LogLog.Debug("Entering ActiveOptions in ActiveMQAppender");
            _connectionfactory = new ConnectionFactory(_connectionurl, "LOG4NET-Appenders-ActiveMQ-" + System.Guid.NewGuid().ToString());
            _connection = _connectionfactory.CreateConnection();
            _session = _connection.CreateSession();
            _producer = _session.CreateProducer();
            //TODO: This should be happening async since it could be blocking
            LogLog.Debug("Starting Connection");
            _connection.Start();
            LogLog.Debug("ActiveMQ Configured");
            
        }

        #endregion

        override protected  void SendBuffer(LoggingEvent[] events)
        {
            LogLog.Debug("Calling Sendbuffer in ActiveMQAppender");
            BeginAsyncSend();

            if (!ThreadPool.QueueUserWorkItem(new WaitCallback(SendBufferCallback), events))
            {
                EndAsyncSend();
                ErrorHandler.Error(string.Format("Error Appender {0} failed to Threadpool.QueueUserItem logging events  in sendbuffer", _connection.ClientId));
            }
            
        }

        /// <summary>
        /// Send the contents of the buffer to the remote sink.
        /// </summary>
        /// <remarks>
        /// This method is designed to be used with the <see cref="ThreadPool"/>.
        /// This method expects to be passed an array of <see cref="LoggingEvent"/>
        /// objects in the state param.
        /// </remarks>
        /// <param name="state">the logging events to send</param>
        private void SendBufferCallback(object state)
        {
            
            try
            {
                LogLog.Debug("Entering SendBufferCallback");
                LoggingEvent[] events = (LoggingEvent[])state;

                // Producer the events here..
                if (_connection != null && _connection.IsStarted)
                {
                    for (int i = 0; i < events.Length; i++)
                    {
                        IObjectMessage oMessage = _session.CreateObjectMessage(events[i]);
                        _producer.RequestTimeout = TimeSpan.FromMilliseconds(500);
                        string oDestination = string.Format("{0}.{1}", _topicprefix, events[i].LoggerName);
                        IDestination _destination = _session.GetTopic(oDestination);
                        _producer.Send(_destination, oMessage);
                    }
                }
                else
                {
                    LogLog.Debug("Connection was null or not started");
                }
                
            }
            catch (Exception ex)
            {
                ErrorHandler.Error("Failed in SendBufferCallback", ex);
            }
            finally
            {
                EndAsyncSend();
            }
        }

        /// <summary>
        /// A work item is being queued into the thread pool
        /// </summary>
        private void BeginAsyncSend()
        {
            // The work queue is not empty
            m_workQueueEmptyEvent.Reset();

            // Increment the queued count
            Interlocked.Increment(ref m_queuedCallbackCount);
        }

        /// <summary>
        /// A work item from the thread pool has completed
        /// </summary>
        private void EndAsyncSend()
        {
            // Decrement the queued count
            if (Interlocked.Decrement(ref m_queuedCallbackCount) <= 0)
            {
                // If the work queue is empty then set the event
                m_workQueueEmptyEvent.Set();
            }
        }


        #region Private Instance Fields

        private string _connectionurl = "failover:(tcp://localhost:616161)";
        private string _topicprefix = "LOG4NET";

        private static IConnectionFactory _connectionfactory;
        private static IConnection _connection;
        private static ISession _session;
        private static IMessageProducer _producer;

        /// <summary>
        /// The number of queued callbacks currently waiting or executing
        /// </summary>
        private int m_queuedCallbackCount = 0;

        /// <summary>
        /// Event used to signal when there are no queued work items
        /// </summary>
        /// <remarks>
        /// This event is set when there are no queued work items. In this
        /// state it is safe to close the appender.
        /// </remarks>
        private ManualResetEvent m_workQueueEmptyEvent = new ManualResetEvent(true);

        #endregion


        #region Public Instance Properties

        public string ConnectionUri
        {
            get { return _connectionurl; }
            set { _connectionurl = value; }
        }

        public string TopicPrefix
        {
            get { return _topicprefix; }
            set { _topicprefix = value; }
        }

        #endregion
    }
}
