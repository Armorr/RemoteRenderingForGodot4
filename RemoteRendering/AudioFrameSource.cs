
namespace Godot.RemoteRendering
{
    public partial class AudioFrameSource : MediaSource
    {

        private int _audioBusIndex;
        private AudioEffectCapture _capture;
        private int _channels;
        private int _mixRate;

        public AudioFrameSource()
        {
            
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            _audioBusIndex = AudioServer.GetBusIndex("Master");
            _capture = (AudioEffectCapture)AudioServer.GetBusEffect(_audioBusIndex, 0);
            _channels = AudioServer.GetBusChannels(_audioBusIndex);
            _mixRate = (int)AudioServer.GetMixRate();
            _capture.ClearBuffer();
        }

        public int GetChannels()
        {
            return _channels;
        }

        public int GetMixRate()
        {
            return _mixRate;
        }

        public void ClearBuffer()
        {
            _capture.ClearBuffer();
        }

        public Vector2[] GetAudioFrames(int num)
        {
            //GD.Print($"We have {(_capture.GetFramesAvailable())} frames now!");
            if (_capture.CanGetBuffer(num))
            {
                //GD.Print($"We have {num} frames now!");
                //GD.Print($"Get {(num > 480 ? 480 : num)} audio frame!");
                return _capture.GetBuffer(num);
            }

            return null;
        }
        
    }
}

