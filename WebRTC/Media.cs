using System;
using System.Runtime.InteropServices;


namespace Godot.WebRTC
{
    public enum RtcObuPacketization
    {
        RTC_OBU_PACKETIZED_OBU = 0,
        RTC_OBU_PACKETIZED_TEMPORAL_UNIT = 1,
    }

    public enum RtcNalUnitSeparator
    {
        RTC_NAL_SEPARATOR_LENGTH = 0,               // first 4 bytes are NAL unit length
        RTC_NAL_SEPARATOR_LONG_START_SEQUENCE = 1,  // 0x00, 0x00, 0x00, 0x01
        RTC_NAL_SEPARATOR_SHORT_START_SEQUENCE = 2, // 0x00, 0x00, 0x01
        RTC_NAL_SEPARATOR_START_SEQUENCE = 3,       // long or short start sequence
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RtcPacketizationHandlerInit
    {
        public uint ssrc;
        [MarshalAs(UnmanagedType.LPStr)]
        public string cname;
        public byte payloadType;
        public uint clockRate;
        public ushort sequenceNumber;
        public uint timestamp;

        public RtcNalUnitSeparator nalUnitSeparator;
        public ushort maxFragmentSize;
        public RtcObuPacketization obuPacketization;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RtcSsrcForTypeInit
    {
        public uint ssrc;
        [MarshalAs(UnmanagedType.LPStr)]
        public string name;
        [MarshalAs(UnmanagedType.LPStr)]
        public string msid;
        [MarshalAs(UnmanagedType.LPStr)]
        public string trackId;
    }


    public class Media
    {
        
    }

}