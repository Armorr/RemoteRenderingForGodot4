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
    
    public partial class WebSocketSignaling : Node
    {
        private static HashSet<WebSocketSignaling> instances = new HashSet<WebSocketSignaling>();
        private WebSocketPeer m_webSocket;
        private readonly string m_url;
        private readonly float m_timeout;
        private readonly SynchronizationContext m_mainThreadContext;
        private bool m_running;
        private Thread m_signalingThread;
        private CancellationTokenSource _cancellationTokenSource;

        public string Url { get { return m_url; } }
        public bool printMessage = true;

        public bool isSample = false;

        public event OnStartHandler OnStart;
        public event OnConnectHandler OnCreateConnection;
        public event OnDisconnectHandler OnDestroyConnection;
        public event OnOfferHandler OnOffer;
        public event OnAnswerHandler OnAnswer;
        public event OnIceCandidateHandler OnIceCandidate;

        public WebSocketSignaling(SynchronizationContext mainThreadContext) {
            m_url = isSample ? "ws://219.224.167.248:8000/server" : "ws://219.224.167.226";
            m_timeout = 10.0f;
            m_mainThreadContext = mainThreadContext;

            if (instances.Any(x => x.Url == m_url))
            {
                GD.PrintErr($"Other {nameof(WebSocketSignaling)} exists with same URL:{m_url}. Signaling process may be in conflict.");
            }

            instances.Add(this);
        }

        ~WebSocketSignaling()
        {
            if (m_running)
                Stop();

            instances.Remove(this);
        }

        public void OpenConnection(string connectionId) {
            WSSend($"{{\"type\":\"connect\", \"connectionId\":\"{connectionId}\"}}");
        }

        public void CloseConnection(string connectionId) {
            WSSend($"{{\"type\":\"disconnect\", \"connectionId\":\"{connectionId}\"}}");
        }
    
        public void Stop() {
            if (m_running)
            {
                m_running = false;
                m_webSocket?.Close();

                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    if (!m_signalingThread.Join(1000))
                    {
                        // Thread didn't terminate in 1 second, consider other ways to handle it
                    }
                    m_signalingThread = null;
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }
        
        public void Start() {
            if (m_running)
                throw new InvalidOperationException("This object is already started.");
            m_running = true;
            _cancellationTokenSource = new CancellationTokenSource();
            m_signalingThread = new Thread(() => WSManage(_cancellationTokenSource.Token));
            m_signalingThread.Start();
        }

        public void SendOffer(string id, string sdp) {
            GD.PrintRich("[color=green]I'm Sending An Offer![/color]");
            if (isSample)
            {
                Collections.Dictionary<string, string> message = new Collections.Dictionary<string, string>
                {
                    { "id", id },
                    { "type", "offer" },
                    { "sdp", sdp }
                };
                WSSend(Json.Stringify(message));
            }
            else
            {
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
            GD.Print(answer);
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
            GD.Print(Json.Stringify(message));
            WSSend(Json.Stringify(message));
        }

        private void WSManage(CancellationToken cancellationToken)
        {
            WSCreate();
            GD.Print($"Signaling: Connected to WS {m_url} OK!");
            while (m_running && (!cancellationToken.IsCancellationRequested))
            {
                m_webSocket.Poll();
                var state = m_webSocket.GetReadyState();
                if (state == WebSocketPeer.State.Open) {
                    while (m_webSocket.GetAvailablePacketCount() > 0) {
                        string packet = m_webSocket.GetPacket().GetStringFromUtf8();
                        WSProcessMessage(packet);
                    }
                }
                else if (state == WebSocketPeer.State.Closing) {

                }
                else if (state == WebSocketPeer.State.Closed) {
                    GD.PrintErr($"Signaling: Connected Closed!");
                }
                Thread.Sleep(10);
            }

            GD.Print("Signaling: WS managing thread ended");
        }

        private void WSCreate()
        {
            m_webSocket = new WebSocketPeer();
            Monitor.Enter(m_webSocket);
            GD.Print($"Signaling: Connecting WS {m_url}");
            Error err = m_webSocket.ConnectToUrl(m_url);
            while (err != Error.Ok) {
                GD.Print("Connecting to Signaling Server failed, retrying!");
                Thread.Sleep((int)(m_timeout * 1000));
		        err = m_webSocket.ConnectToUrl(m_url);
            }
        }

        private void WSProcessMessage(string message) {
            if (printMessage) GD.Print($"Signaling: Receiving message: {message}");

            if (isSample) {
                var msg = (Collections.Dictionary)Json.ParseString(message);
                if (!msg.ContainsKey("id")) {
                    return;
                }
                string id = (string)msg["id"];
                if (!msg.ContainsKey("type")) {
                    return;
                }
                string type = (string)msg["type"];
                if (type == "request") {
                    m_mainThreadContext.Post(d => OnCreateConnection?.Invoke(this, id, true), null);
                }
                else if (type == "answer") {
                    DescData answer = new DescData
                    {
                        connectionId = (string)msg["id"],
                        sdp = (string)msg["sdp"]
                    };
                    m_mainThreadContext.Post(d => OnAnswer?.Invoke(this, answer), null);
                }
                return;
            }

            try
            {
                var msg = (Collections.Dictionary)Json.ParseString(message);

                if (msg.ContainsKey("type"))
                {
                    string type = (string)msg["type"];
                    if (type == "connect") {
                        m_mainThreadContext.Post(d => OnCreateConnection?.Invoke(this, (string)msg["connectionId"], (bool)msg["polite"]), null);
                    }
                    else if (type == "disconnect") {
                        m_mainThreadContext.Post(d => OnDestroyConnection?.Invoke(this, (string)msg["connectionId"]), null);
                    }
                    else if (type == "offer") {
                        Collections.Dictionary<string, Variant> data = (Collections.Dictionary<string, Variant>)msg["data"];
                        DescData offer = new DescData() {
                            connectionId = (string)msg["from"],
                            sdp = (string)data["sdp"],
                            polite = (bool)data["polite"]
                        };
                        m_mainThreadContext.Post(d => OnOffer?.Invoke(this, offer), null);
                    }
                    else if (type == "answer") {
                        Collections.Dictionary<string, Variant> data = (Collections.Dictionary<string, Variant>)msg["data"];
                        DescData answer = new DescData
                        {
                            connectionId = (string)msg["from"],
                            sdp = (string)data["sdp"]
                        };
                        m_mainThreadContext.Post(d => OnAnswer?.Invoke(this, answer), null);
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
                        m_mainThreadContext.Post(d => OnIceCandidate?.Invoke(this, candidate), null);
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
            if (m_webSocket == null || m_webSocket.GetReadyState() != WebSocketPeer.State.Open) {
                GD.Print("Signaling: WS is not connected. Unable to send message");
                return;
            }
            if (data is string s) {
                m_webSocket.SendText(s);
            }
            else {
                m_webSocket.SendText(Json.Stringify((Variant)data));
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

