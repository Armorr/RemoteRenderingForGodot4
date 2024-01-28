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
        
        private readonly WebSocketSignaling _signal;

        private List<VideoStreamSender> _senders = new ();
        private readonly Dictionary<string, PeerConnection> _mapConnectionIdAndPeer = new ();
        private InputReceiver _inputReceiver = new ();
        
        public event Action onStart;
        public event Action<string> onCreatedConnection;
        public event Action<string> onDeletedConnection;
        public event Action<string, string> onGotOffer;
        public event Action<string, string> onGotAnswer;
        public event Action<string> onConnect;
        public event Action<string> onDisconnect;

        private VideoStreamSender _videoSender = new(30);
        private AudioStreamSender _audioSender = new();
        private static readonly string[] IceServers = { "stun:stun.l.google.com:19302" };

        static WebSocketSignaling CreateSignaling() {
            return new WebSocketSignaling(SynchronizationContext.Current);
        }

        public SignalingManager() {
            _signal = CreateSignaling();
            _signal.OnStart += OnStart;
            _signal.OnCreateConnection += OnCreateConnection;
            _signal.OnDestroyConnection += OnDestroyConnection;
            _signal.OnOffer += OnOffer;
            _signal.OnAnswer += OnAnswer;
            _signal.OnIceCandidate += OnIceCandidate;
        }

        public override void _Ready()
        {
            base._Ready();
            _signal.Start();
            PluginUtils.InitLogger(RtcLogLevel.RTC_LOG_ERROR);
            AddChild(_videoSender);
            AddChild(_audioSender);
        }

        public void Run() {
            
        }

        public void Stop() {
            
        }

        public void CreateConnection(string connectionId) {
            _signal.OpenConnection(connectionId);
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
            // TODO
            onCreatedConnection?.Invoke(connectionId);
        }

        void OnDestroyConnection(WebSocketSignaling signaling, string connectionId)
        {
            DeletePeerConnection(connectionId);
            onDeletedConnection?.Invoke(connectionId);
        }

        PeerConnection CreatePeerConnection(string connectionId)
        {
            if (_mapConnectionIdAndPeer.TryGetValue(connectionId, out var peer))
            {
                _mapConnectionIdAndPeer.Remove(connectionId);
                peer.Dispose();
            }
            RtcConfiguration config = new RtcConfiguration();
            config.iceServersCount = IceServers.Length;
            config.iceServers = Marshal.AllocHGlobal(IntPtr.Size * IceServers.Length);
            config.disableAutoNegotiation = true;
            config.forceMediaTransport = true;
            for (int i = 0; i < IceServers.Length; i++)
            {
                IntPtr serverPtr = Marshal.StringToHGlobalAnsi(IceServers[i]);
                Marshal.WriteIntPtr(config.iceServers, i * IntPtr.Size, serverPtr);
            }
            peer = new PeerConnection(connectionId, ref config);
            _mapConnectionIdAndPeer.Add(connectionId, peer);

            peer.OnSendOffer += _signal.SendOffer;
            peer.OnSendAnswer += _signal.SendAnswer;
            peer.OnSendCandidate += _signal.SendCandidate;
            peer.DataChannelForInput += InputReceiverAddChannel;

            return peer;
        }

        void DeletePeerConnection(string connectionId)
        {
            // TODO
        }

        void OnAnswer(WebSocketSignaling signaling, DescData e)
        {
            GD.PrintRich("[color=green]I Got An Answer![/color]");
            var connectionId = e.connectionId;
            if (!_mapConnectionIdAndPeer.TryGetValue(connectionId, out var pc))
            {
                GD.PrintErr("connectionId: " + connectionId + "-Not found PeerConnection");
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
                GD.PrintErr("connectionId: " + connectionId + "-Not found PeerConnection");
                return;
            }
            pc.AddRemoteCandidate(e.candidate, e.sdpMid);
        }

        void OnOffer(WebSocketSignaling signaling, DescData e)
        {
            GD.PrintRich("[color=green]I Got An Offer![/color]");
            var connectionId = e.connectionId;
            if (!_mapConnectionIdAndPeer.TryGetValue(connectionId, out var pc))
            {
                pc = CreatePeerConnection(connectionId);
            }
            RtcTrackInit video = new RtcTrackInit{
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
            RtcTrackInit audio = new RtcTrackInit{
                direction = RtcDirection.RTC_DIRECTION_SENDONLY,
                codec = RtcCodec.RTC_CODEC_OPUS,
                payloadType = 111,
                ssrc = 2,
                name = "audio-stream",
                mid = "audio-stream",
                msid = "stream1",
                trackId = "audio-stream",
                //profile = "profile-level-id=640032;packetization-mode=1;level-asymmetry-allowed=1"
            };
            _videoSender.AddVideo(pc, video);
            _audioSender.AddAudio(pc, audio);
            pc.OnGotRemoteDescription(e.sdp, "offer");
        }

        void SendOffer(string id, string sdp)
        {
            
        }

        private void InputReceiverAddChannel(DataChannel dc)
        {
            _inputReceiver.SetChannel(dc);
        }

    }
}