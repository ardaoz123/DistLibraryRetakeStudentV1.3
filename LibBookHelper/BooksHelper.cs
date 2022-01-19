using System;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using LibData;
using Microsoft.Extensions.Configuration;

namespace BookHelperSolution
{
    public struct Setting
    {
        public int BookHelperPortNumber { get; set; }
        public string BookHelperIPAddress { get; set; }
        public int ServerListeningQueue { get; set; }
    }

    abstract class AbsSequentialServerHelper
    {
        protected Setting settings;
        protected string booksDataFile;

        /// <summary>
        /// Report method can be used to print message to console in standaard formaat. 
        /// It is not mandatory to use it, but highly recommended.
        /// </summary>
        /// <param name="type">For example: [Exception], [Error], [Info] etc</param>
        /// <param name="msg"> In case of [Exception] the message of the exection can be passed. Same is valud for other types</param>
        protected void report(string type, string msg)
        {
            // Console.Clear();
            Console.Out.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>");
            if (!String.IsNullOrEmpty(msg))
            {
                msg = msg.Replace(@"\u0022", " ");
            }

            Console.Out.WriteLine("[Server Helper] {0} : {1}", type, msg);
        }

        /// <summary>
        /// This methid loads required settings.
        /// </summary>
        protected void GetConfigurationValue()
        {
            settings = new Setting();
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;
                IConfiguration Config = new ConfigurationBuilder()
                    .SetBasePath(Path.GetFullPath(Path.Combine(path, @"../../../../")))
                    .AddJsonFile("appsettings.json")
                    .Build();

                settings.BookHelperIPAddress = Config.GetSection("BookHelperIPAddress").Value;
                settings.BookHelperPortNumber = Int32.Parse(Config.GetSection("BookHelperPortNumber").Value);
                settings.ServerListeningQueue = Int32.Parse(Config.GetSection("ServerListeningQueue").Value);
            }
            catch (Exception e) { report("[Exception]", e.Message); }
        }

        protected abstract void loadDataFromJson();
        protected abstract void createSocket();
        public abstract void handelListening();
        protected abstract Message processMessage(Message message);

    }

    class SequentialServerHelper : AbsSequentialServerHelper
    {
        // check all the required parameters for the server. How are they initialized? 
        public Socket listener;
        public IPEndPoint listeningPoint;
        public IPAddress ipAddress;
        public List<BookData> booksList;


        public SequentialServerHelper() : base()
        {
            booksDataFile = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../../") + "Books.json");
            GetConfigurationValue();
            loadDataFromJson();
        }

        /// <summary>
        /// This method loads data items provided in booksDataFile into booksList.
        /// </summary>
        protected override void loadDataFromJson()
        {
            //todo: To meet the assignment requirement, implement this method 
            string jsonBooks = "Books.json";
            
            try
            {
                string jsonStringBooks = File.ReadAllText(jsonBooks);
                booksList = JsonSerializer.Deserialize<List<BookData>>(jsonStringBooks);
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR");
            }
        }

        /// <summary>
        /// This method establishes required socket: listener.
        /// </summary>
        protected override void createSocket()
        {
            //todo: To meet the assignment requirement, implement this method
            try
            {
                IPAddress ipAddress = IPAddress.Parse(settings.BookHelperIPAddress);
                IPEndPoint localEndpoint = new IPEndPoint(ipAddress, settings.BookHelperPortNumber);

                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                listener.Bind(localEndpoint);
                listener.Listen(5);
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR");
            }

        }

        /// <summary>
        /// This method is optional. It delays the execution for a short period of time.
        /// Note: Can be used only for testing purposes.
        /// </summary>
        void delay()
        {
            int m = 10;
            for (int i = 0; i <= m; i++)
            {
                Console.Out.Write("{0} .. ", i);
                Thread.Sleep(200);
            }
            Console.WriteLine("\n");
        }

        /// <summary>
        /// This method handles all the communications with the LibServer.
        /// </summary>
        public override void handelListening()
        {
            createSocket();
            Socket newSock = listener.Accept();

            //todo: To meet the assignment requirement, finish the implementation of this method 
            byte[] buffer = new byte[1000];
            byte[] msgBookResult = new byte[1000];
            string data = null;
            string jsonMsgResult;

            while (newSock.Connected)
            {
                int b = newSock.Receive(buffer);
                data = Encoding.ASCII.GetString(buffer, 0, b);

                Message messageToRec = JsonSerializer.Deserialize<Message>(data);
                data = null;

                var jsonMsgResultReply =  processMessage(messageToRec);

                jsonMsgResult = JsonSerializer.Serialize(jsonMsgResultReply);
                msgBookResult = Encoding.ASCII.GetBytes(jsonMsgResult);
                newSock.Send(msgBookResult);
            }
            
        }

        /// <summary>
        /// Given the message received from the Server, this method processes the message and returns a reply.
        /// </summary>
        /// <param name="message">The message received from the LibServer.</param>
        /// <returns>The message that needs to be sent back as the reply.</returns>
        protected override Message processMessage(Message message)
        {
            Message reply = new Message();
            //todo: To meet the assignment requirement, finish the implementation of this method .
            try
            {
                if(message.Type == LibData.MessageType.BookInquiry)
                {
                    bool bookFound = false;
                    foreach (var book in booksList)
                        {
                            if (message.Content == book.Title)
                            {
                                reply.Type = MessageType.BookInquiryReply;
                                reply.Content = JsonSerializer.Serialize(book);
                                bookFound = true;
                            }
                        }

                    if(!bookFound)
                    {
                        reply.Type = MessageType.NotFound;
                        reply.Content = message.Content;
                    }
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR");
            }
            return reply;
        }
    }
}
