using Futronic.Devices.FS26;
using SuperWebSocket;
using System;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Threading;

namespace ConsoleApp1
{
    class Program
    {
        private static WebSocketServer wsServer;
        private static String fingerprintBase64;
        private static ManualResetEvent threadEvent;
        private static Thread fingerPrintReaderThread = null;
        private static ManualResetEvent socketThreadEvent = new ManualResetEvent(false);

        private static WebSocketSession webSession = null;
        private static Boolean detectarDigital = false;
        private static Thread thread;

        static void Main(string[] args)
        {
            thread = new Thread(StartSocket);
            thread.Start();
        }

        private static void StartSocket()
        {
            wsServer = new WebSocketServer();
            int port = 60;
            wsServer.Setup(port);
            wsServer.NewSessionConnected += WsServer_NewSessionConnected;
            wsServer.NewMessageReceived += WsServer_NewMessageReceived;
            wsServer.NewDataReceived += WsServer_NewDataReceived;
            wsServer.SessionClosed += WsServer_SessionClosed;
            wsServer.Start();
            socketThreadEvent.WaitOne();
          
        }

        private static void WsServer_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
        {
            Console.WriteLine("SessionClosed");
        }

        private static void WsServer_NewDataReceived(WebSocketSession session, byte[] value)
        {
            Console.WriteLine("NewDataReceived");
        }

        private static void WsServer_NewMessageReceived(WebSocketSession session, string value)
        {
            if (value == "1")
            {
                detectarDigital = true;
                if (fingerPrintReaderThread != null && fingerPrintReaderThread.IsAlive)
                {

                } else
                {
                    threadEvent = new ManualResetEvent(false);
                    fingerPrintReaderThread = new Thread(StartFingerPrintReader);
                    fingerPrintReaderThread.Start();
                }
            }
        }


        private static void enviarDigital(String digital)
        {
            
                Console.WriteLine("Enviando");
                webSession.Send(digital);
                detectarDigital = false;
            
            
        }

        private static void StartFingerPrintReader()
        {
            var accessor = new DeviceAccessor();

            using (var device = accessor.AccessFingerprintDevice())
            {
                device.SwitchLedState(true, true);

                device.FingerDetected += (sender, eventArgs) =>
                {
                    Console.WriteLine("digital detectada");
                    if(detectarDigital)
                    {
                        detectarDigital = false;
                        device.SwitchLedState(false, true);

                        var fingerprint = device.ReadFingerprint();
                        //var tempFile = Path.GetTempFileName();
                        //var tmpBmpFile = Path.ChangeExtension(tempFile, "bmp");

                        ImageConverter converter = new ImageConverter();
                        var bitMapFingerPrint = (byte[])converter.ConvertTo(fingerprint, typeof(byte[]));

                        var data = new NameValueCollection();
                        data["biometria"] = Convert.ToBase64String(bitMapFingerPrint);
                        fingerprintBase64 = data["biometria"];
                        webSession.Send(fingerprintBase64);
                    }


                    //threadEvent.Set();



                    //fingerprint.Save(tmpBmpFile);
                };

                device.FingerReleased += (sender, eventArgs) =>
                {
                    Console.WriteLine("Finger Released!");

                    device.SwitchLedState(true, true);
                };

                device.StartFingerDetection();
                threadEvent.WaitOne();
                device.SwitchLedState(true, true);

            }
        }

        private static void WsServer_NewSessionConnected(WebSocketSession session)
        {
            webSession = session;
        }
    }
}