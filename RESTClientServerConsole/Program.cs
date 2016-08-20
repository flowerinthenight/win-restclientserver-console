using CommandLine;
using CommandLine.Text;
using Grapevine;
using Grapevine.Client;
using Grapevine.Server;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RESTClientServerConsole
{
    public sealed class TrResource : RESTResource
    {
        [RESTRoute(Method = HttpMethod.GET, PathInfo = @"^/greet$")]
        public void HandleGetGreetRequest(HttpListenerContext context)
        {
            Console.WriteLine("URL: {0}", context.Request.RawUrl);
            SendTextResponse(context, "Hello world!");
        }

        [RESTRoute(Method = HttpMethod.GET, PathInfo = @"^/endpoint?.+$")]
        [RESTRoute(Method = HttpMethod.POST, PathInfo = @"^/endpoint?.+$")]
        public void HandleEndpointRequest(HttpListenerContext context)
        {
            Console.WriteLine("URL: {0}", context.Request.RawUrl);
            Console.WriteLine("Method: {0}", context.Request.HttpMethod);

            try
            {
                foreach (string k in context.Request.QueryString)
                {
                    Console.WriteLine("{0}: {1}", k, context.Request.QueryString[k]);
                }

                if (context.Request.HttpMethod.Equals("GET"))
                {
                    SendTextResponse(context, "GET");
                }

                if (context.Request.HttpMethod.Equals("POST"))
                {
                    SendTextResponse(context, "POST");
                }
            }
            catch (Exception e)
            {
                SendTextResponse(context, e.Message + "\n" + e.StackTrace);
            }
        }

        [RESTRoute]
        public void HandleAllGetRequests(HttpListenerContext context)
        {
            SendTextResponse(context, "ROOT NODE");
        }
    }

    class Options
    {
        [Option("server", DefaultValue = false, Required = false, HelpText = "Run as REST server.")]
        public bool RunAsServer { get; set; }

        [Option("host", DefaultValue = "localhost", Required = false, HelpText = "Set host IP.")]
        public string Host { get; set; }

        [Option("port", DefaultValue = "1234", Required = false, HelpText = "Set host port.")]
        public string Port { get; set; }

        [Option("url", DefaultValue = "/", Required = false,
            HelpText = @"URL after [host:port]. Should start with '/'.")]
        public string Url { get; set; }

        [Option("method", DefaultValue = "GET", Required = false, HelpText = "GET, POST.")]
        public string Method { get; set; }

        [Option("timeout", DefaultValue = -1, Required = false,
            HelpText = "Request timeout in milliseconds. When value is -1, client will use " +
            "the default timeout set in GrapeVine (1.21 seconds).")]
        public int Timeout { get; set; }

        [HelpOption]
        public string GetHelp()
        {
            return HelpText.AutoBuild(this, (HelpText current) =>
                HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var exitEvent = new ManualResetEvent(false);
            var options = new Options();

            if (CommandLine.Parser.Default.ParseArgumentsStrict(args, options, () => { Environment.Exit(-2); }))
            {
                if (options.RunAsServer)
                {
                    //
                    // As server
                    //
                    Console.CancelKeyPress += (sender, eventArgs) => {
                        eventArgs.Cancel = true;
                        exitEvent.Set();
                    };

                    Console.WriteLine("Run server on " + options.Host + ":" + options.Port);
                    Console.WriteLine("Press CTRL+C to terminate server.\n");
                    Console.WriteLine("Host: {0}:{1}", options.Host, options.Port);

                    try
                    {
                        var server = new RESTServer();
                        server.Host = options.Host;
                        server.Port = options.Port;
                        server.Start();

                        exitEvent.WaitOne();
                        server.Stop();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + "\n" + e.StackTrace);
                    }
                }
                else
                {
                    Dictionary<string, HttpMethod> method = new Dictionary<string, HttpMethod>()
                    {
                        { "GET", HttpMethod.GET },
                        { "POST", HttpMethod.POST }
                    };

                    //
                    // As client
                    //
                    try
                    {
                        var client = new RESTClient("http://" + options.Host + ":" + options.Port);
                        var request = new RESTRequest(options.Url);
                        request.Method = method[options.Method];

                        var response = client.Execute(request);
                        Console.WriteLine("Response: " + response.Content);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + "\n" + e.StackTrace);
                    }
                }
            }
        }
    }
}
