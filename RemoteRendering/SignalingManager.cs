using Godot;
using Godot.RemoteRendering.Signaling;
using Godot.WebRTC;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;


namespace Godot.RemoteRendering
{
    [GlobalClass]
    public partial class SignalingManager : Node {
        private readonly WebSocketSignaling m_signal;
        //private VideoStreamSender sender;
        private List<VideoStreamSender> _senders = new List<VideoStreamSender>();
        private readonly Dictionary<string, PeerConnection> _mapConnectionIdAndPeer = new();
        
        public event Action onStart;
        public event Action<string> onCreatedConnection;
        public event Action<string> onDeletedConnection;
        public event Action<string, string> onGotOffer;
        public event Action<string, string> onGotAnswer;
        public event Action<string> onConnect;
        public event Action<string> onDisconnect;

        private VideoStreamSender _sender = new(30);

        static WebSocketSignaling CreateSignaling() {
            return new WebSocketSignaling(SynchronizationContext.Current);
        }

        public SignalingManager() {
            m_signal = CreateSignaling();
            m_signal.OnStart += OnStart;
            m_signal.OnCreateConnection += OnCreateConnection;
            m_signal.OnDestroyConnection += OnDestroyConnection;
            m_signal.OnOffer += OnOffer;
            m_signal.OnAnswer += OnAnswer;
            m_signal.OnIceCandidate += OnIceCandidate;
            
            //sender = new VideoStreamSender();
            
        }

        public override void _Ready()
        {
            base._Ready();
            m_signal.Start();
            PluginUtils.InitLogger(RtcLogLevel.RTC_LOG_ERROR);
            AddChild(_sender);
        }

        public void Run() {
            
        }

        public void Stop() {
            
        }

        public void CreateConnection(string connectionId) {
            m_signal.OpenConnection(connectionId);
        }

        public void DeleteConnection(string connectionId) {

        }

        void OnStart(WebSocketSignaling signaling)
        {
            GD.Print("On Start!");
            onStart?.Invoke();
        }

        void OnCreateConnection(WebSocketSignaling signaling, string connectionId, bool polite)
        {
            GD.PrintRich("[color=green]On CreateConnection from " + connectionId + "[/color]");
            PeerConnection peer = CreatePeerConnection(connectionId);
            RtcTrackInit init = new RtcTrackInit{
                direction = RtcDirection.RTC_DIRECTION_SENDONLY,
                codec = RtcCodec.RTC_CODEC_H264,
                payloadType = 102,
                ssrc = 1,
                name = "video-stream",
                mid = "video-stream",
                msid = "stream1",
                trackId = "video-stream",
                profile = "profile-level-id=640032;packetization-mode=1;level-asymmetry-allowed=1"
            };
            VideoStreamSender sender = new VideoStreamSender(30);
            AddChild(sender);
            sender.AddVideo(peer.Id, init);
            _senders.Add(sender);
            DataChannel dc = peer.CreateDataChannel("ping-pong");
            peer.SetLocalDescription(null);
            onCreatedConnection?.Invoke(connectionId);
        }

        void OnDestroyConnection(WebSocketSignaling signaling, string connectionId)
        {
            DestroyConnection(connectionId);
        }

        void DestroyConnection(string connectionId)
        {
            //DeletePeerConnection(connectionId);
            onDeletedConnection?.Invoke(connectionId);
        }

        PeerConnection CreatePeerConnection(string connectionId)
        {
            if (_mapConnectionIdAndPeer.TryGetValue(connectionId, out var peer))
            {
                _mapConnectionIdAndPeer.Remove(connectionId);
                peer.Dispose();
            }
            string[] servers = { "stun:stun.l.google.com:19302" };
            RtcConfiguration config = new RtcConfiguration();
            config.iceServersCount = servers.Length;
            config.iceServers = Marshal.AllocHGlobal(IntPtr.Size * servers.Length);
            config.disableAutoNegotiation = true;
            for (int i = 0; i < servers.Length; i++)
            {
                IntPtr serverPtr = Marshal.StringToHGlobalAnsi(servers[i]);
                Marshal.WriteIntPtr(config.iceServers, i * IntPtr.Size, serverPtr);
            }
            peer = new PeerConnection(connectionId, ref config);
            _mapConnectionIdAndPeer.Add(connectionId, peer);

            peer.OnSendOffer += m_signal.SendOffer;
            peer.OnSendAnswer += m_signal.SendAnswer;
            peer.OnSendCandidate += m_signal.SendCandidate;

            return peer;
        }

        void DeletePeerConnection(string connectionId)
        {
            
        }

        void OnAnswer(WebSocketSignaling signaling, DescData e)
        {
            GD.PrintRich("[color=green]I'm Got An Answer![/color]");
            var connectionId = e.connectionId;
            if (!_mapConnectionIdAndPeer.TryGetValue(connectionId, out var pc))
            {
                GD.PrintErr("connectionId: " + connectionId + " -----Not found PeerConnection");
                return;
            }
            pc.SetRemoteDescription(e.sdp, "answer");
        }

        void OnIceCandidate(WebSocketSignaling signaling, CandidateData e)
        {
            GD.PrintRich("[color=green]I Got An IceCandidate![/color]");
            var connectionId = e.connectionId;
            if (!_mapConnectionIdAndPeer.TryGetValue(connectionId, out var pc))
            {
                GD.PrintErr("connectionId: " + connectionId + " -----Not found PeerConnection");
                return;
            }
            GD.Print(e.candidate);
            GD.Print(e.sdpMid);
            pc.AddRemoteCandidate(e.candidate, e.sdpMid);
        }

        void OnOffer(WebSocketSignaling signaling, DescData e)
        {
            GD.PrintRich("[color=green]I Got An Offer![/color]");
            GD.Print(e.sdp);
            var connectionId = e.connectionId;
            if (!_mapConnectionIdAndPeer.TryGetValue(connectionId, out var pc))
            {
                pc = CreatePeerConnection(connectionId);
            }
            RtcTrackInit init = new RtcTrackInit{
                direction = RtcDirection.RTC_DIRECTION_SENDONLY,
                codec = RtcCodec.RTC_CODEC_H264,
                payloadType = 102,
                ssrc = 1,
                name = "video-stream",
                mid = "video-stream",
                msid = "stream1",
                trackId = "video-stream",
                //profile = "profile-level-id=640032;packetization-mode=1;level-asymmetry-allowed=1"
            };
            //VideoStreamSender sender = new VideoStreamSender();
            //AddChild(sender);
            _sender.AddVideo(pc.Id, init);
            //_senders.Add(sender);
            //DataChannel dc = pc.CreateDataChannel("input");
            pc.OnGotRemoteDescription(e.sdp, "offer");
        }

        void SendOffer(string id, string sdp)
        {
            
        }

    }
}