using System.Net;

int argn = 0;
bool is_server = false;

if (args.Length > 1) {
    if (args[argn++] == "--mode") {
        if (args[argn] == "server") {
            is_server   = true;
        }
    }
}


if (is_server) {
    var ws = new NCS_Server("127.0.0.1", 0);

    await Task.Delay(1000);

    // HttpListener server = new HttpListener();
    // server.Prefixes.Add("http://*:8087/");
    // server.Start();
    Console.WriteLine("Hello, World!\nPress enter to exit...");

} else {
    var wsc = new NCS_Client("127.0.0.1", 10_000);

    Console.WriteLine("Hello, World!\nPress enter to exit...");


}
Console.Read();
