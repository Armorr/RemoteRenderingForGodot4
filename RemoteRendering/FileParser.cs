using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;


namespace Godot.RemoteRendering
{

    public struct rtcNalUnitHeader
    {
        private byte _first;

        public rtcNalUnitHeader(byte[] data)
        {
            if (data.Length < 1)
                throw new ArgumentException("Invalid NALU data");

            _first = data[0];
        }

        public bool ForbiddenBit => (_first & 0x80) != 0;
        public byte NRI => (byte)((_first >> 5) & 0x03);
        public byte UnitType => (byte)(_first & 0x1F);

        public void SetForbiddenBit(bool isSet) => _first = (byte)((_first & 0x7F) | (isSet ? 0x80 : 0));
        public void SetNRI(byte nri) => _first = (byte)((_first & 0x9F) | ((nri & 0x03) << 5));
        public void SetUnitType(byte type) => _first = (byte)((_first & 0xE0) | (type & 0x1F));
    }

    public class FileParser
    {
        private ulong sampleTime_us = 0;
        private readonly ulong sampleDuration_us;
        private List<byte> sample = new List<byte>();
        private int counter = 0;
        private readonly string directory;
        private readonly string extension;
        private bool loop;
        private ulong loopTimestampOffset = 0;

        private List<byte> previousUnitType5 = null;
        private List<byte> previousUnitType7 = null;
        private List<byte> previousUnitType8 = null;

        public FileParser(string directory, string extension, ulong samplesPerSecond, bool loop)
        {
            this.directory = directory;
            this.extension = extension;
            this.sampleDuration_us = 1000 * 1000 / samplesPerSecond;
            this.loop = loop;
        }

        public void Start()
        {
            sampleTime_us = ulong.MaxValue - sampleDuration_us + 1;
            counter = -1;
            LoadNextSample();
        }

        public void Stop()
        {
            sample.Clear();
            sampleTime_us = 0;
            counter = -1;
        }

        public List<byte> GetSample()
        {
            return sample;
        }

        public ulong GetSampleTime_us()
        {
            return sampleTime_us;
        }

        public ulong GetSampleDuration_us()
        {
            return sampleDuration_us;
        }

        public void LoadNextSample()
        {
            string frame_id = (++counter).ToString();

            string url = Path.Combine(directory, $"sample-{frame_id}{extension}");
            //GD.Print("--------" + url);
            try
            {
                byte[] contents = File.ReadAllBytes(url);
                sample = contents.ToList();
                sampleTime_us += sampleDuration_us;
            }
            catch (Exception)
            {
                if (loop && counter > 0)
                {
                    loopTimestampOffset = sampleTime_us;
                    counter = -1;
                    LoadNextSample();
                }
                else
                {
                    sample.Clear();
                }
            }

            //GD.Print("--------" + sample.Count);

            // int i = 0;
            // while (i < sample.Count)
            // {
            //     if (i + 4 >= sample.Count)
            //         throw new InvalidOperationException("Invalid sample format");

            //     byte[] lengthBytes = new byte[4]; // Assuming 32-bit length field
            //     Array.Copy(sample.ToArray(), i, lengthBytes, 0, 4);
            //     Array.Reverse(lengthBytes);
            //     uint length = BitConverter.ToUInt32(lengthBytes, 0);
            //     //uint length = (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(sample.ToArray(), i));
            //     GD.Print("--------" + length);
            //     int naluStartIndex = i + 4;
            //     var naluEndIndex = naluStartIndex + length;

            //     if (naluEndIndex > sample.Count)
            //         throw new InvalidOperationException("Invalid sample format");

            //     byte[] naluData = new byte[length];
            //     Array.Copy(sample.ToArray(), naluStartIndex, naluData, 0, (int)length);

            //     rtcNalUnitHeader header = new rtcNalUnitHeader(naluData);
            //     byte type = header.UnitType;

            //     switch (type)
            //     {
            //         case 7:
            //             previousUnitType7 = new List<byte>(naluData);
            //             break;
            //         case 8:
            //             previousUnitType8 = new List<byte>(naluData);
            //             break;
            //         case 5:
            //             previousUnitType5 = new List<byte>(naluData);
            //             break;
            //     }

            //     i = (int)naluEndIndex;
            // }
        }

    }




}