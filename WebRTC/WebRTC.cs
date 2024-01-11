using Godot;
using System;
using System.Runtime.InteropServices;

namespace Godot.WebRTC
{

    public enum RtcState : int
    {
        RTC_NEW = 0,
        RTC_CONNECTING = 1,
        RTC_CONNECTED = 2,
        RTC_DISCONNECTED = 3,
        RTC_FAILED = 4,
        RTC_CLOSED = 5
    }

    public enum RtcIceState : int
    {
        RTC_ICE_NEW = 0,
        RTC_ICE_CHECKING = 1,
        RTC_ICE_CONNECTED = 2,
        RTC_ICE_COMPLETED = 3,
        RTC_ICE_FAILED = 4,
        RTC_ICE_DISCONNECTED = 5,
        RTC_ICE_CLOSED = 6
    }

    public enum RtcGatheringState : int
    {
        RTC_GATHERING_NEW = 0,
        RTC_GATHERING_INPROGRESS = 1,
        RTC_GATHERING_COMPLETE = 2
    }

    public enum RtcSignalingState : int
    {
        RTC_SIGNALING_STABLE = 0,
        RTC_SIGNALING_HAVE_LOCAL_OFFER = 1,
        RTC_SIGNALING_HAVE_REMOTE_OFFER = 2,
        RTC_SIGNALING_HAVE_LOCAL_PRANSWER = 3,
        RTC_SIGNALING_HAVE_REMOTE_PRANSWER = 4,
    }

    public enum RtcLogLevel : int
    {
        RTC_LOG_NONE = 0,
        RTC_LOG_FATAL = 1,
        RTC_LOG_ERROR = 2,
        RTC_LOG_WARNING = 3,
        RTC_LOG_INFO = 4,
        RTC_LOG_DEBUG = 5,
        RTC_LOG_VERBOSE = 6
    }

    public enum RtcCertificateType : int
    {
        RTC_CERTIFICATE_DEFAULT = 0, // ECDSA
        RTC_CERTIFICATE_ECDSA = 1,
        RTC_CERTIFICATE_RSA = 2,
    }

    public enum RtcCodec : int
    {
        // video
        RTC_CODEC_H264 = 0,
        RTC_CODEC_VP8 = 1,
        RTC_CODEC_VP9 = 2,
        RTC_CODEC_H265 = 3,
        RTC_CODEC_AV1 = 4,

        // audio
        RTC_CODEC_OPUS = 128,
        RTC_CODEC_PCMU = 129,
        RTC_CODEC_PCMA = 130,
        RTC_CODEC_AAC = 131,
    }

    public enum RtcDirection : int
    {
        RTC_DIRECTION_UNKNOWN = 0,
        RTC_DIRECTION_SENDONLY = 1,
        RTC_DIRECTION_RECVONLY = 2,
        RTC_DIRECTION_SENDRECV = 3,
        RTC_DIRECTION_INACTIVE = 4
    }

    public enum RtcTransportPolicy : int 
    {
        RTC_TRANSPORT_POLICY_ALL = 0,
        RTC_TRANSPORT_POLICY_RELAY = 1
    }



    public static class WebRTC {
        internal const string Lib = "datachannel";
        
    }


    public delegate void RtcLogCallbackFunc(RtcLogLevel level, [MarshalAs(UnmanagedType.LPStr)] string message);
    public delegate void RtcDescriptionCallbackFunc(int pc, [MarshalAs(UnmanagedType.LPStr)] string sdp, [MarshalAs(UnmanagedType.LPStr)] string type, IntPtr ptr);
    public delegate void RtcCandidateCallbackFunc(int pc, [MarshalAs(UnmanagedType.LPStr)] string cand, [MarshalAs(UnmanagedType.LPStr)] string mid, IntPtr ptr);
    public delegate void RtcStateChangeCallbackFunc(int pc, RtcState state, IntPtr ptr);
    public delegate void RtcIceStateChangeCallbackFunc(int pc, RtcIceState state, IntPtr ptr);
    public delegate void RtcGatheringStateCallbackFunc(int pc, RtcGatheringState state, IntPtr ptr);
    public delegate void RtcSignalingStateCallbackFunc(int pc, RtcSignalingState state, IntPtr ptr);
    public delegate void RtcDataChannelCallbackFunc(int pc, int dc, IntPtr ptr);
    public delegate void RtcTrackCallbackFunc(int pc, int tr, IntPtr ptr);
    public delegate void RtcOpenCallbackFunc(int id, IntPtr ptr);
    public delegate void RtcClosedCallbackFunc(int id, IntPtr ptr);
    public delegate void RtcErrorCallbackFunc(int id, [MarshalAs(UnmanagedType.LPStr)] string error, IntPtr ptr);
    public delegate void RtcMessageCallbackFunc(int id, IntPtr message, int size, IntPtr ptr);
    public delegate void RtcInterceptorCallbackFunc(int id, IntPtr meesage, int size, IntPtr ptr);
    public delegate void RtcBufferedAmountLowCallbackFunc(int id, IntPtr ptr);
    public delegate void RtcAvailableCallbackFunc(int id, IntPtr ptr);


    internal static class NativeMethods
    {
        [DllImport(WebRTC.Lib)]public static extern void rtcPreload();
        [DllImport(WebRTC.Lib)]public static extern void rtcCleanup();
        [DllImport(WebRTC.Lib)]public static extern void rtcInitLogger(RtcLogLevel level, RtcLogCallbackFunc cb);
        [DllImport(WebRTC.Lib)]public static extern void rtcSetUserPointer(int id, IntPtr ptr);

        // PeerConnection
        [DllImport(WebRTC.Lib)]public static extern int rtcCreatePeerConnection(ref RtcConfiguration config);
        [DllImport(WebRTC.Lib)]public static extern int rtcClosePeerConnection(int pc);
        [DllImport(WebRTC.Lib)]public static extern int rtcDeletePeerConnection(int pc);

        [DllImport(WebRTC.Lib)]public static extern int rtcSetLocalDescriptionCallback(int pc, RtcDescriptionCallbackFunc cb);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetLocalCandidateCallback(int pc, RtcCandidateCallbackFunc cb);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetStateChangeCallback(int pc, RtcStateChangeCallbackFunc cb);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetIceStateChangeCallback(int pc, RtcIceStateChangeCallbackFunc cb);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetGatheringStateChangeCallback(int pc, RtcGatheringStateCallbackFunc cb);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetSignalingStateChangeCallback(int pc, RtcSignalingStateCallbackFunc cb);

        [DllImport(WebRTC.Lib)]public static extern int rtcSetLocalDescription(int pc, string type);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetRemoteDescription(int pc, string sdp, string type);
        [DllImport(WebRTC.Lib)]public static extern int rtcAddRemoteCandidate(int pc, string cand, string mid);
        [DllImport(WebRTC.Lib)]public static extern int rtcGetLocalDescription(int pc, IntPtr buffer, int size);
        [DllImport(WebRTC.Lib)]public static extern int rtcGetRemoteDescription(int pc, IntPtr buffer, int size);

        // DataChannel, Track, and WebSocket common API

        [DllImport(WebRTC.Lib)]public static extern int rtcSetOpenCallback(int id, RtcOpenCallbackFunc cb);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetClosedCallback(int id, RtcClosedCallbackFunc cb);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetErrorCallback(int id, RtcErrorCallbackFunc cb);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetMessageCallback(int id, RtcMessageCallbackFunc cb);
        [DllImport(WebRTC.Lib)]public static extern int rtcSendMessage(int id, IntPtr data, int size);
        [DllImport(WebRTC.Lib)]public static extern int rtcClose(int id);
        [DllImport(WebRTC.Lib)]public static extern int rtcDelete(int id);
        [DllImport(WebRTC.Lib)]public static extern int rtcIsOpen(int id);
        [DllImport(WebRTC.Lib)]public static extern int rtcIsClosed(int id);

        // DataChannel
        [DllImport(WebRTC.Lib)]public static extern int rtcSetDataChannelCallback(int pc, RtcDataChannelCallbackFunc cb);
        [DllImport(WebRTC.Lib)]public static extern int rtcCreateDataChannel(int pc, string label);
        [DllImport(WebRTC.Lib)]public static extern int rtcCreateDataChannelEx(int pc, string label, ref RtcDataChannelInit init);
        [DllImport(WebRTC.Lib)]public static extern int rtcDeleteDataChannel(int dc);
        [DllImport(WebRTC.Lib)]public static extern int rtcGetDataChannelStream(int dc);
        [DllImport(WebRTC.Lib)]public static extern int rtcGetDataChannelLabel(int dc, IntPtr buffer, int size);
        [DllImport(WebRTC.Lib)]public static extern int rtcGetDataChannelProtocol(int dc, IntPtr buffer, int size);
        [DllImport(WebRTC.Lib)]public static extern int rtcGetDataChannelReliability(int dc, IntPtr re);

        // Track
        [DllImport(WebRTC.Lib)]public static extern int rtcSetTrackCallback(int pc, RtcTrackCallbackFunc cb);
        [DllImport(WebRTC.Lib)]public static extern int rtcAddTrack(int pc, string mediaDescriptionSdp);
        [DllImport(WebRTC.Lib)]public static extern int rtcAddTrackEx(int pc, ref RtcTrackInit trackInit);
        [DllImport(WebRTC.Lib)]public static extern int rtcDeleteTrack(int tr);
        [DllImport(WebRTC.Lib)]public static extern int rtcGetTrackDescription(int tr, IntPtr buffer, int size);
        [DllImport(WebRTC.Lib)]public static extern int rtcGetTrackMid(int tr, IntPtr buffer, int size);
        [DllImport(WebRTC.Lib)]public static extern int rtcGetTrackDirection(int tr, ref RtcDirection direction);

        // Media
        [DllImport(WebRTC.Lib)]public static extern IntPtr rtcCreateOpaqueMessage(IntPtr data, int size);
        [DllImport(WebRTC.Lib)]public static extern void rtcDeleteOpaqueMessage(IntPtr msg);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetMediaInterceptorCallback(int id, RtcInterceptorCallbackFunc cb);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetH264PacketizationHandler(int tr, ref RtcPacketizationHandlerInit init);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetH265PacketizationHandler(int tr, ref RtcPacketizationHandlerInit init);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetAV1PacketizationHandler(int tr, ref RtcPacketizationHandlerInit init);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetOpusPacketizationHandler(int tr, ref RtcPacketizationHandlerInit init);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetAACPacketizationHandler(int tr, ref RtcPacketizationHandlerInit init);
        [DllImport(WebRTC.Lib)]public static extern int rtcChainRtcpSrReporter(int tr);
        [DllImport(WebRTC.Lib)]public static extern int rtcChainRtcpNackResponder(int tr, uint maxStoredPacketsCount);
        [DllImport(WebRTC.Lib)]public static extern int rtcTransformSecondsToTimestamp(int id, double seconds, ref uint timestamp);
        [DllImport(WebRTC.Lib)]public static extern int rtcTransformTimestampToSeconds(int id, uint timestamp, ref double seconds);
        [DllImport(WebRTC.Lib)]public static extern int rtcGetCurrentTrackTimestamp(int id, ref uint timestamp);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetTrackRtpTimestamp(int id, uint timestamp);
        [DllImport(WebRTC.Lib)]public static extern int rtcGetLastTrackSenderReportTimestamp(int id, ref uint timestamp);
        [DllImport(WebRTC.Lib)]public static extern int rtcSetNeedsToSendRtcpSr(int id);

        // WebSocket
        [DllImport(WebRTC.Lib)]public static extern int rtcCreateWebSocket(string url);
        [DllImport(WebRTC.Lib)]public static extern int rtcCreateWebSocketEx(string url, IntPtr config);
        [DllImport(WebRTC.Lib)]public static extern int rtcDeleteWebSocket(int ws);
        [DllImport(WebRTC.Lib)]public static extern int rtcGetWebSocketRemoteAddress(int ws, IntPtr buffer, int size);
        [DllImport(WebRTC.Lib)]public static extern int rtcGetWebSocketPath(int ws, IntPtr buffer, int size);
        
    }
    

}
