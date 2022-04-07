using System.Net;
using WatsonWebsocket;
using System.Diagnostics;
using System.Net.Sockets;

using NetCoreServer;

class WSThreadHandler
{
    WatsonWsServer Server;

    private Task ConnectionsTask;

    public int port => 0;

    public WSThreadHandler(string addr, int port)
    {
        Server = new WatsonWsServer(addr, port);

        Server.Logger = (s) => { Console.WriteLine($"LOG: {s}"); };

        Server.ClientConnected += Server_ClientConnected;
        Server.ClientDisconnected += Server_ClientDisconnected;
        Server.MessageReceived += Server_MessageReceived;

        ConnectionsTask = Server.StartAsync();


        ConnectionsTask.ContinueWith((t) =>
        {

            Console.WriteLine("Server stopped");
            if (ConnectionsTask.Exception != null)
            {
                Console.WriteLine("Server stopped with exception");
                Console.WriteLine(ConnectionsTask.Exception.ToString());
            }
        });
    }

    private void Server_MessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        var data = e.Data;
        var strdata = System.Text.Encoding.UTF8.GetString(data);
        Console.WriteLine($"Message from client: {strdata}!");
        Server.SendAsync(e.IpPort, "😺" + strdata.Replace('l', 'w').ToUpper() + "?!");
    }

    private void Server_ClientDisconnected(object? sender, ClientDisconnectedEventArgs e)
    {
        Console.WriteLine($"Disconnected from client: {e.IpPort}!");
    }

    private void Server_ClientConnected(object? sender, ClientConnectedEventArgs e)
    {
        Console.WriteLine($"Connected to client: {e.IpPort}!");
    }
}

class WSC
{
    WatsonWsClient Client;

    private Task ConnectionTask;

    public WSC(string addr, int port)
    {
        Client = new WatsonWsClient(addr, port, false);

        Client.Logger = (s) => { Console.WriteLine($"LOG: {s}"); };

        Client.MessageReceived += Client_MessageReceived;
        Client.ServerDisconnected += Client_ClientDisconnected;

        Client.ServerConnected += Client_ServerConnected;

        ConnectionTask = //Client.StartWithTimeoutAsync(1);
        new Task(() =>
        {
            Client.Start();

        });

        ConnectionTask.ContinueWith((t) =>
        {
            Console.WriteLine("Connection task completed!");
            if (ConnectionTask.Exception != null)
            {
                Console.WriteLine("Connection task completed with exception!");
                Console.WriteLine(ConnectionTask.Exception.ToString());
            }
        });

        Console.WriteLine("started client");
    }

    private void Client_ServerConnected(object? sender, EventArgs e)
    {
        Console.WriteLine($"Connected to server!");

        Client.SendAsync("Hello, World!");
    }

    private void Client_ClientDisconnected(object? sender, EventArgs e)
    {
        Console.WriteLine($"Disconnected from server!");
    }

    private void Client_MessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        var data = e.Data;
        var strdata = System.Text.Encoding.UTF8.GetString(data);
        Console.WriteLine($"Message from server: {strdata}!");
        Client.SendAsync("😺" + strdata.Replace('l', 'w').ToUpper() + "?!");
    }
}



class NCS_Server
{

    private class Session: WsSession {
        public Session(WsServer server) : base(server) {}

        public override void OnWsConnected(HttpRequest request)
        {
            Console.WriteLine($"Chat WebSocket session with Id {Id} connected!");

            // Send invite message
            string message = "Hello from WebSocket chat! Please send a message or '!' to disconnect the client!";
            SendTextAsync(message);
        }

        public override void OnWsDisconnected()
        {
            Console.WriteLine($"Chat WebSocket session with Id {Id} disconnected!");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            string message = System.Text.Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            Console.WriteLine("Incoming: " + message);


            SendTextAsync("😺" + message.Replace('l', 'w').ToUpper() + "?!");

            // If the buffer starts with '!' the disconnect the current session
            if (message == "!")
                Close(1000);
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat WebSocket session caught an error with code {error}");
        }
    }

    private class Server : WsServer
    {
        public Server(IPAddress address, int port) : base(address, port) { }

        protected override TcpSession CreateSession() { return new Session(this); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat WebSocket server caught an error with code {error}");
        }
    }

    private Server WS;

    public NCS_Server(string addr, int port)
    {
        WS = new Server(IPAddress.Parse(addr), port);

        var good = WS.Start();

        

        if (good)
            Console.WriteLine($"Server started on {WS.Address}, {((IPEndPoint)WS.Endpoint).Port}!");
        else
            Console.WriteLine("Server failed to start!");
    }

}

class NCS_Client {

    class Client : WsClient {

        public Client(string address, int port) : base(address, port) {}
        public override void OnWsConnected(HttpResponse response)
        {
            Console.WriteLine($"Chat WebSocket client connected a new session with Id {Id}");
            var good = SendTextAsync("Hello, World!");

            if (!good)
                Console.WriteLine("Failed to send message!");
        }

        public override void OnWsConnecting(HttpRequest request)
        {
            request.SetBegin("GET", "/");
            request.SetHeader("Host", "localhost");
            request.SetHeader("Origin", "http://localhost");
            request.SetHeader("Upgrade", "websocket");
            request.SetHeader("Connection", "Upgrade");
            request.SetHeader("Sec-WebSocket-Key", Convert.ToBase64String(WsNonce));
            request.SetHeader("Sec-WebSocket-Protocol", "chat, superchat");
            request.SetHeader("Sec-WebSocket-Version", "13");
            request.SetBody();
        }

        public override void OnWsDisconnected()
        {
            Console.WriteLine($"Chat WebSocket client disconnected a session with Id {Id}");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            Console.WriteLine($"Incoming: {System.Text.Encoding.UTF8.GetString(buffer, (int)offset, (int)size)}");
        }

        protected override void OnDisconnected()
        {
            base.OnDisconnected();

            Console.WriteLine($"Chat WebSocket client disconnected a session with Id {Id}");

            // Wait for a while...
            // Thread.Sleep(1000);
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat WebSocket client caught an error with code {error}");
        }
    }


    private Client WS;

    public NCS_Client(string addr, int port)
    {
        WS = new NCS_Client.Client(addr, port);

        WS.ConnectAsync();

        // WS.SendTextAsync("Hello, World!");
    }

}