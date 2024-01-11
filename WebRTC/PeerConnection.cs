using Godot;
using System;
using System.Runtime.InteropServices;
using Godot.Collections;

namespace Godot.WebRTC
{
    [StructLayout(LayoutKind.Sequential)] 
    public struct RtcConfiguration
    {
        public IntPtr iceServers;
        public int iceServersCount;
        public IntPtr proxyServer;
        public IntPtr bindAddress;
        public RtcCertificateType certificateType;
        public RtcTransportPolicy iceTransportPolicy;
        [MarshalAs(UnmanagedType.I1)]
        public bool enableIceTcp;
        [MarshalAs(UnmanagedType.I1)]
        public bool enableIceUdpMux;
        [MarshalAs(UnmanagedType.I1)]
        public bool disableAutoNegotiation;
        [MarshalAs(UnmanagedType.I1)]
        public bool forceMediaTransport;
        public ushort portRangeBegin;
        public ushort portRangeEnd;
        public int mtu;
        public int maxMessageSize;
    }

    public class Description
    {
        public string sdp;
        public string type;

        public Description(string sdp, string type)
        {
            this.sdp = sdp;
            this.type = type;
        }
    }

    public class Candidate
    {
        public string cand;
        public string mid;

        public Candidate(string cand, string mid)
        {
            this.cand = cand;
            this.mid = mid;
        }
    }

    public delegate void OnSendOfferHandler(string connectionId, string sdp);
    public delegate void OnSendCandidateHandler(string connectionId, string candidate, string mid);
    public delegate void OnSendAnswerHandler(string connectionId, string answer);

    public class PeerConnection : IDisposable
    {
        public Action<Description> LocalDescriptionCreated { get; set; }
        public Action<Candidate> LocalCandidateCreated { get; set; }
        public Action<RtcState> StateChanged { get; set; }
        public Action<RtcIceState> IceStateChanged { get; set; }
        public Action<RtcGatheringState> GatheringStateChanged { get; set; }
        public Action<RtcSignalingState> SignalingStateChanged {get; set; }
        public int Id { get; private set; }
        private bool disposed;
        private RtcConfiguration config;
        private string connectionId;

        private readonly Dictionary<int, DataChannel> _mapIdDataChannel = new Dictionary<int, DataChannel>();

        public event OnSendOfferHandler OnSendOffer;
        public event OnSendCandidateHandler OnSendCandidate;
        public event OnSendAnswerHandler OnSendAnswer;
        
        private RtcDescriptionCallbackFunc onLocalDescription;
        private RtcCandidateCallbackFunc onCandidate;
        private RtcStateChangeCallbackFunc onStateChange;
        private RtcGatheringStateCallbackFunc onGatheringStateChange;
        private RtcSignalingStateCallbackFunc onSignalingStateChange;
        private RtcIceStateChangeCallbackFunc onIceStateChange;
        private RtcDataChannelCallbackFunc onDataChannel;

        public PeerConnection(string connectionId, ref RtcConfiguration config)
        {
            disposed = false;
            this.config = config;
            this.connectionId = connectionId;
            Id = NativeMethods.rtcCreatePeerConnection(ref config);
            InitCallback();
        }

        ~PeerConnection()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!disposed) {
                NativeMethods.rtcDeletePeerConnection(Id);
                GD.Print("Disposing PeerConnection!");
            }
                
            disposed = true;
        }

        public void InitCallback()
        {
            GD.Print("Initialing PeerConnection Callback!");
            onLocalDescription = OnLocalDescription;
            onCandidate = OnLocalCandidate;
            onStateChange = OnStateChange;
            onGatheringStateChange = OnGatheringStateChange;
            onSignalingStateChange = OnSignalingStateChange;
            onIceStateChange = OnIceStateChange;
            onDataChannel = OnDataChannel;

            if (NativeMethods.rtcSetLocalDescriptionCallback(Id, onLocalDescription) < 0)
                throw new Exception("Error from rtcSetLocalDescriptionCallback.");

            if (NativeMethods.rtcSetLocalCandidateCallback(Id, onCandidate) < 0)
                throw new Exception("Error from rtcSetLocalCandidateCallback.");

            if (NativeMethods.rtcSetStateChangeCallback(Id, onStateChange) < 0)
                throw new Exception("Error from rtcSetStateChangeCallback.");

            if (NativeMethods.rtcSetGatheringStateChangeCallback(Id, onGatheringStateChange) < 0)
                throw new Exception("Error from rtcSetGatheringStateChangeCallback.");

            if (NativeMethods.rtcSetSignalingStateChangeCallback(Id, onSignalingStateChange) < 0)
                throw new Exception("Error from rtcSetSignalingStateChangeCallback.");
            
            if (NativeMethods.rtcSetIceStateChangeCallback(Id, onIceStateChange) < 0)
                throw new Exception("Error from rtcSetSignalingStateChangeCallback.");
            
            if (NativeMethods.rtcSetDataChannelCallback(Id, onDataChannel) < 0)
                throw new Exception("Error from rtcSetDataChannelCallback.");
        }

        public void SetUserPointer(IntPtr ptr)
        {
            NativeMethods.rtcSetUserPointer(Id, ptr);
        }

        public void SetLocalDescription(string type)
        {
            GD.Print("Setting LocalDescription!");
            if (NativeMethods.rtcSetLocalDescription(Id, type) < 0)
                throw new Exception("Error from RtcSetLocalDescription.");
        }

        public void SetRemoteDescription(string sdp, string type)
        {
            if (NativeMethods.rtcSetRemoteDescription(Id, sdp, type) < 0)
                throw new Exception("Error from RtcSetRemoteDescription.");
        }

        public void AddRemoteCandidate(string cand, string mid)
        {
            if (NativeMethods.rtcAddRemoteCandidate(Id, cand, mid) < 0)
                throw new Exception("Error from RtcAddRemoteCandidate.");
        }

        public string GetLocalDescriptionSdp()
        {
            // Assuming 8 KB would be enough for a SDP message.
            int bufferSize = 8 * 1024;
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
            int error = NativeMethods.rtcGetLocalDescription(Id, buffer, bufferSize);
            if (error < 0)
                throw new Exception($"Error from rtcGetLocalDescriptionSdp. (error: {error})");
            string sdp = Marshal.PtrToStringAnsi(buffer);
            Marshal.FreeHGlobal(buffer);
            return sdp;
        }

        public DataChannel CreateDataChannel(string label)
        {
            int dc = NativeMethods.rtcCreateDataChannel(Id, label);
            if (dc < 0)
                throw new Exception("Error from rtcCreateDataChannel.");
            DataChannel dataChannel = new DataChannel(dc);
            _mapIdDataChannel.Add(dc, dataChannel);
            return dataChannel;
        }

        public DataChannel CreateDataChannelEx(string label, RtcDataChannelInit init)
        {
            int dc = NativeMethods.rtcCreateDataChannelEx(Id, label, ref init);
            if (dc < 0)
                throw new Exception("Error from rtcCreateDataChannelEx.");

            return new DataChannel(dc);
        }

        public Track AddTrack(string mediaDescriptionSdp)
        {
            int tr = NativeMethods.rtcAddTrack(Id, mediaDescriptionSdp);
            if (tr < 0)
                throw new Exception("Error from AddTrack.");
            return new Track(tr);
        }

        public Track AddTrackEx(RtcTrackInit init)
        {
            int tr = NativeMethods.rtcAddTrackEx(Id, ref init);
            if (tr < 0)
                throw new Exception("Error from AddTrackEx.");

            return new Track(tr);
        }

        public void OnLocalDescription(int pc, string sdp, string type, IntPtr ptr)
        {
            LocalDescriptionCreated?.Invoke(new Description(sdp, type));
            GD.PrintRich("[color=yellow]PC OnLocalDescription[/color]");
            if (type == "answer")
            {
                //GD.Print(sdp);
                OnSendAnswer?.Invoke(connectionId, sdp);
            }
            else if (type == "offer")
            {
                OnSendOffer?.Invoke(connectionId, sdp);
            }
        }

        public void OnLocalCandidate(int pc, string cand, string mid, IntPtr ptr)
        {
            LocalCandidateCreated?.Invoke(new Candidate(cand, mid));
            GD.PrintRich("[color=yellow]PC OnLocalCandidate[/color]");
            OnSendCandidate?.Invoke(connectionId, cand, mid);
        }

        public void OnStateChange(int pc, RtcState state, IntPtr ptr)
        {
            StateChanged?.Invoke(state);
            GD.PrintRich("[color=yellow]PC OnStateChange : " + state + "[/color]");
            if (state == RtcState.RTC_CLOSED || state == RtcState.RTC_DISCONNECTED || state == RtcState.RTC_FAILED) {
                
            }
        }

        public void OnGatheringStateChange(int pc, RtcGatheringState state, IntPtr ptr)
        {
            GatheringStateChanged?.Invoke(state);
            GD.PrintRich("[color=yellow]PC OnGatheringStateChange : " + state + "[/color]");
            if (state == RtcGatheringState.RTC_GATHERING_COMPLETE) {
                //string sdp = GetLocalDescriptionSdp();
                //GD.Print(sdp);
                //OnSendOffer?.Invoke(connectionId, sdp);
                //OnSendAnswer?.Invoke(connectionId, sdp);
                SetLocalDescription("offer");
            }
        }

        public void OnSignalingStateChange(int pc, RtcSignalingState state, IntPtr ptr)
        {
            SignalingStateChanged?.Invoke(state);
            GD.PrintRich("[color=yellow]PC OnSignalingStateChange : " + state + "[/color]");
            if (state == RtcSignalingState.RTC_SIGNALING_HAVE_REMOTE_OFFER)
            {
                SetLocalDescription("answer");
            }
        }

        public void OnIceStateChange(int pc, RtcIceState state, IntPtr ptr)
        {
            IceStateChanged?.Invoke(state);
            GD.PrintRich("[color=yellow]PC OnIceStateChange : " + state + "[/color]");
        }

        public void OnGotRemoteDescription(string sdp, string type)
        {
            SetRemoteDescription(sdp, type);
            //SetLocalDescription(null);
        }

        public void OnDataChannel(int pc, int dc, IntPtr ptr)
        {
            GD.PrintRich("[color=red]DataChannel!!![/color]");
            DataChannel dataChannel = new DataChannel(dc);
            _mapIdDataChannel.Add(dc, dataChannel);
        }

    }
}