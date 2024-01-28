using Godot;
using Godot.WebRTC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Godot.RemoteRendering.Signaling
{
    public delegate void OnStartHandler(WebSocketSignaling signaling);
    public delegate void OnConnectHandler(WebSocketSignaling signaling, string connectionId, bool polite);
    public delegate void OnDisconnectHandler(WebSocketSignaling signaling, string connectionId);
    public delegate void OnOfferHandler(WebSocketSignaling signaling, DescData e);
    public delegate void OnAnswerHandler(WebSocketSignaling signaling, DescData e);
    public delegate void OnIceCandidateHandler(WebSocketSignaling signaling, CandidateData e);
    
    public class WebSocketSignaling
    {
        private static readonly HashSet<WebSocketSignaling> Instances = new ();
        private WebSocketPeer _webSocket;
        private readonly string _url;
        private readonly float _timeout;
        private readonly SynchronizationContext _mainThreadContext;
        private bool _running;
        private bool _connected;
        private Thread _signalingThread;
        private CancellationTokenSource _cancellationTokenSource;

        public string Url { get { return _url; } }
        public bool printMessage = true;

        public event OnStartHandler OnStart;
        public event OnConnectHandler OnCreateConnection;
        public event OnDisconnectHandler OnDestroyConnection;
        public event OnOfferHandler OnOffer;
        public event OnAnswerHandler OnAnswer;
        public event OnIceCandidateHandler OnIceCandidate;

        public WebSocketSignaling(SynchronizationContext mainThreadContext)
        {
            _url = "ws://219.224.167.177";
            _timeout = 10.0f;
            _connected = false;
            _mainThreadContext = mainThreadContext;

            if (Instances.Any(x => x.Url == _url))
            {
                GD.PrintErr($"Other {nameof(WebSocketSignaling)} exists with same URL:{_url}. Signaling process may be in conflict.");
                return;
            }

            Instances.Add(this);
        }

        ~WebSocketSignaling()
        {
            if (_running)
                Stop();

            Instances.Remove(this);
        }

        public void OpenConnection(string connectionId) {
            WSSend($"{{\"type\":\"connect\", \"connectionId\":\"{connectionId}\"}}");
        }

        public void CloseConnection(string connectionId) {
            WSSend($"{{\"type\":\"disconnect\", \"connectionId\":\"{connectionId}\"}}");
        }
    
        public void Stop() {
            if (_running)
            {
                _running = false;
                _webSocket?.Close();

                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    if (!_signalingThread.Join(1000))
                    {
                        // Thread didn't terminate in 1 second, consider other ways to handle it
                    }
                    _signalingThread = null;
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }
        
        public void Start() {
            if (_running)
                throw new InvalidOperationException("This object is already started.");
            _running = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _signalingThread = new Thread(() => WSManage(_cancellationTokenSource.Token));
            _signalingThread.Start();
        }

        public void SendOffer(string id, string sdp) {
            GD.PrintRich("[color=green]I'm Sending An Offer![/color]");
            Collections.Dictionary<string, string> data = new Collections.Dictionary<string, string>
            {
                { "connectionId", id },
                { "sdp", sdp }
            };
            Collections.Dictionary<string, Variant> message = new Collections.Dictionary<string, Variant>
            {
                { "data" , data },
                { "type", "offer" }
            };
            //GD.Print(Json.Stringify(message));
            WSSend(Json.Stringify(message));
        }

        public void SendAnswer(string id, string answer) {
            GD.PrintRich("[color=green]I'm Sending An Answer![/color]");
            Collections.Dictionary<string, string> data = new Collections.Dictionary<string, string>
            {
                { "connectionId", id },
                { "sdp", answer }
            };
            Collections.Dictionary<string, Variant> message = new Collections.Dictionary<string, Variant>
            {
                { "data" , data },
                { "type", "answer" }
            };
            //GD.Print(answer);
            WSSend(Json.Stringify(message));
        }

        public void SendCandidate(string id, string candidate, string mid) {
            GD.PrintRich("[color=green]I'm Sending An Candidate![/color]");
            Collections.Dictionary<string, Variant> data = new Collections.Dictionary<string, Variant>
            {
                { "connectionId", id },
                { "candidate", candidate },
                { "sdpMid", mid },
                { "sdpMLineIndex", 0 }
            };
            Collections.Dictionary<string, Variant> message = new Collections.Dictionary<string, Variant>
            {
                { "data" , data },
                { "type", "candidate" }
            };
            //GD.Print(Json.Stringify(message));
            WSSend(Json.Stringify(message));
        }

        private void WSManage(CancellationToken cancellationToken)
        {
            WSCreate();
            GD.Print($"Signaling: Connected to WS {_url} OK!");
            while (_running && (!cancellationToken.IsCancellationRequested))
            {
                _webSocket.Poll();
                var state = _webSocket.GetReadyState();
                if (state == WebSocketPeer.State.Open) {
                    if (!_connected)
                    {
                        GD.Print("WebSocket Connect OK!");
                        _connected = true;
                    }
                    while (_webSocket.GetAvailablePacketCount() > 0) {
                        string packet = _webSocket.GetPacket().GetStringFromUtf8();
                        WSProcessMessage(packet);
                    }
                }
                else if (state == WebSocketPeer.State.Closing) {

                }
                else if (state == WebSocketPeer.State.Closed) {
                    GD.PrintErr($"Signaling: Connected Closed!");
                    break;
                }
                else if (state == WebSocketPeer.State.Connecting)
                {
                    GD.Print($"Signaling: Still Connecting!");
                    Thread.Sleep(2000);
                }
                Thread.Sleep(100);
            }
            GD.Print("Signaling: WS managing thread ended");
        }

        private void WSCreate()
        {
            _webSocket = new WebSocketPeer();
            GD.Print($"Signaling: Connecting WS {_url}");
            _webSocket.ConnectToUrl(_url);
        }

        private void WSProcessMessage(string message) {
            if (printMessage) GD.Print($"Signaling: Receiving message: {message}");
            
            try
            {
                var msg = (Collections.Dictionary)Json.ParseString(message);
                if (msg.ContainsKey((Variant)"type"))
                {
                    string type = (string)msg["type"];
                    if (type == "connect") {
                        _mainThreadContext.Post(d => OnCreateConnection?.Invoke(this, (string)msg["connectionId"], (bool)msg["polite"]), null);
                    }
                    else if (type == "disconnect") {
                        _mainThreadContext.Post(d => OnDestroyConnection?.Invoke(this, (string)msg["connectionId"]), null);
                    }
                    else if (type == "offer") {
                        Collections.Dictionary<string, Variant> data = (Collections.Dictionary<string, Variant>)msg["data"];
                        DescData offer = new DescData
                        {
                            connectionId = (string)msg["from"],
                            sdp = (string)data["sdp"],
                            polite = (bool)data["polite"]
                        };
                        _mainThreadContext.Post(d => OnOffer?.Invoke(this, offer), null);
                    }
                    else if (type == "answer") {
                        Collections.Dictionary<string, Variant> data = (Collections.Dictionary<string, Variant>)msg["data"];
                        DescData answer = new DescData
                        {
                            connectionId = (string)msg["from"],
                            sdp = (string)data["sdp"]
                        };
                        _mainThreadContext.Post(d => OnAnswer?.Invoke(this, answer), null);
                    }
                    else if (type == "candidate") {
                        Collections.Dictionary<string, Variant> data = (Collections.Dictionary<string, Variant>)msg["data"];
                        CandidateData candidate = new CandidateData
                        {
                            connectionId = (string)msg["from"],
                            candidate = (string)data["candidate"],
                            sdpMLineIndex = (int)data["sdpMLineIndex"],
                            sdpMid = (string)data["sdpMid"]
                        };
                        _mainThreadContext.Post(d => OnIceCandidate?.Invoke(this, candidate), null);
                    }
                    else if (type == "error") {
                        GD.PrintErr("Get an ERROR message: " + message);
                    }
                }
            }
            catch (Exception ex)
            {
                GD.Print("Signaling: Failed to parse message: " + ex);
            }
        }

        private void WSSend(object data) {
            if (_webSocket == null || _webSocket.GetReadyState() != WebSocketPeer.State.Open) {
                GD.Print("Signaling: WS is not connected. Unable to send message");
                return;
            }
            if (data is string s) {
                _webSocket.SendText(s);
            }
            else {
                _webSocket.SendText(Json.Stringify((Variant)data));
            }
        }

        private void WSConnected() {
            
        }

        private void WSError() {
            
        }

        private void WSClosed() {
            
        }
    
    }




}

