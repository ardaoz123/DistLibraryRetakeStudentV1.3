using System.Linq;
using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Text;
using LibClient;
using Microsoft.Extensions.Configuration;

namespace LibClient
{
    public struct Setting
    {
        public int ServerPortNumber { get; set; }
        public string ServerIPAddress { get; set; }

    }

    public class Output
    {
        public string Client_id { get; set; } // the id of the client that requests the book
        public string BookName { get; set; } // the name of the book to be reqyested
        public string Status { get; set; } // final status received from the server
        public string Error { get; set; } // True if errors received from the server
        public string BorrowerName { get; set; } // the name of the borrower in case the status is borrowed, otherwise null
        public string ReturnDate { get; set; } // the email of the borrower in case the status is borrowed, otherwise null
    }

    abstract class AbsSequentialClient
    {
        protected Setting settings;

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

            Console.Out.WriteLine("[Client] {0} : {1}", type, msg);
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

                settings.ServerIPAddress = Config.GetSection("ServerIPAddress").Value;
                settings.ServerPortNumber = Int32.Parse(Config.GetSection("ServerPortNumber").Value);
                // settings.ServerListeningQueue = Int32.Parse(Config.GetSection("ServerListeningQueue").Value);
            }
            catch (Exception e) { report("[Exception]", e.Message); }
        }

        protected abstract void createSocketAndConnect();
        public abstract Output handleConntectionAndMessagesToServer();
        protected abstract Message processMessage(Message message);

    }




    class SequentialClient : AbsSequentialClient
    {
        public Output result;
        public Socket clientSocket;
        public IPEndPoint serverEndPoint;
        public IPAddress ipAddress;

        public string client_id;
        private string bookName;

        //This field is optional to use. 
        private int delayTime;
        /// <summary>
        /// Initializes the client based on the given parameters and seeting file.
        /// </summary>
        /// <param name="id">id of the clients provided by the simulator</param>
        /// <param name="bookName">name of the book to be requested from the server, provided by the simulator</param>
        public SequentialClient(int id, string bookName)
        {
            GetConfigurationValue();

            // this.delayTime = 100;
            this.bookName = bookName;
            this.client_id = "Client " + id.ToString();
            this.result = new Output();
            result.Client_id = this.client_id;
        }


        /// <summary>
        /// Optional method. Can be used for testing to delay the output time.
        /// </summary>
        // public void delay()
        // {
        //     int m = 10;
        //     for (int i = 0; i <= m; i++)
        //     {
        //         Console.Out.Write("{0} .. ", i);
        //         Thread.Sleep(delayTime);
        //     }
        //     Console.WriteLine("\n");
        // }

        /// <summary>
        /// Connect socket settings and connect to the helpers.
        /// </summary>
        protected override void createSocketAndConnect()
        {
            System.Console.WriteLine("createSocketAndConnect");
            //todo: To meet the assignment requirement, finish the implementation of this method.
            ipAddress = IPAddress.Parse(settings.ServerIPAddress);
            serverEndPoint = new IPEndPoint(ipAddress, settings.ServerPortNumber);
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                clientSocket.Connect(serverEndPoint);
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR");
            }

        }

        /// <summary>
        /// This method starts the socketserver after initializion and handles all the communications with the server. 
        /// Note: The signature of this method must not change.
        /// </summary>
        /// <returns>The final result of the request that will be written to output file</returns>
        public override Output handleConntectionAndMessagesToServer()
        {
            System.Console.WriteLine("handleConntectionAndMessagesToServer");
            this.report("starting:", this.client_id + " ; " + this.bookName);
            createSocketAndConnect();

            byte[] bufferServer = new byte[1000];
            byte[] msgServer = new byte[1000];
            Message ServerMessage = new Message();
            string ServerResponse;
            string ClientMessage;

            try
            {
                System.Console.WriteLine("server type content check");
                ServerMessage.Type = MessageType.Hello;
                ServerMessage.Content = client_id;

                System.Console.WriteLine(ServerMessage.Type);
                System.Console.WriteLine(ServerMessage.Content);
                
                string jsonServer = JsonSerializer.Serialize(ServerMessage);
                // create byte message to send
                msgServer = Encoding.ASCII.GetBytes(jsonServer);
                clientSocket.Send(msgServer);
                System.Console.WriteLine("client socket msg send");
                System.Console.WriteLine(jsonServer);
            
                int d = clientSocket.Receive(bufferServer);
                System.Console.WriteLine("client socket msg receive");
                System.Console.WriteLine(bufferServer);
                ServerResponse = Encoding.ASCII.GetString(bufferServer, 0, d);
                ServerMessage = JsonSerializer.Deserialize<Message>(ServerResponse);
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR");
            }

            // todo: To meet the assignment requirement, finish the implementation of this method.

                try
                {
                    ServerMessage.Type = MessageType.BookInquiry;
                    ServerMessage.Content = bookName;

                    ClientMessage = JsonSerializer.Serialize(ServerMessage);
                    msgServer = Encoding.ASCII.GetBytes(ClientMessage);
                    clientSocket.Send(msgServer);

                    int c = clientSocket.Receive(bufferServer);
                    ServerResponse = Encoding.ASCII.GetString(bufferServer, 0, c);
                    ServerMessage = JsonSerializer.Deserialize<Message>(ServerResponse);
                    ServerResponse = null;
                    var resultBook = processMessage(ServerMessage);
                    System.Console.WriteLine(this.result.BookName);
                    clientSocket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("ERROR");
                }
            return this.result;
        }



        /// <summary>
        /// Process the messages of the server. Depending on the logic, type and content of a message the client may return different message values.
        /// </summary>
        /// <param name="message">Received message to be processed</param>
        /// <returns>The message that needs to be sent back as the reply.</returns>
        protected override Message processMessage(Message message)
        {
            System.Console.WriteLine(message.Content);
            System.Console.WriteLine("processMessage");
            Message processedMsgResult = new Message();
            //todo: To meet the assignment requirement, finish the implementation of this method.
            try
            {
                switch (message.Type)
                {
                    case LibClient.MessageType.BookInquiryReply:
                    System.Console.WriteLine("bookinquiry reply");
                        BookData bookInfo = JsonSerializer.Deserialize<BookData>(message.Content);
                        result.Client_id = this.client_id;
                        result.BookName = this.bookName;
                        result.Status = bookInfo.Status;
                        result.Error = null;
                        result.BorrowerName = bookInfo.BorrowedBy;
                        result.ReturnDate = bookInfo.ReturnDate;
                        break;

                    case LibClient.MessageType.Error:
                        System.Console.WriteLine("Error");
                        break;

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR");
            }

            return processedMsgResult;
        }
    }
}

