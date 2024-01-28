
namespace Godot.RemoteRendering
{
    public partial class VideoFrameSource : MediaSource
    {
        private Viewport _viewport;
        private Image _frameImage;

        public VideoFrameSource()
        {
            
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            _viewport = GetTree().CurrentScene.GetViewport();
            _frameImage = _viewport.GetTexture().GetImage();
        }

        public int GetImageWidth()
        {
            return _frameImage.GetWidth();
        }

        public int GetImageHeight()
        {
            return _frameImage.GetHeight();
        }

        public Image GetFrameData()
        {
            return _frameImage;
        }

        public Image.Format GetFrameFormat()
        {
            return _frameImage.GetFormat();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            _frameImage = _viewport.GetTexture().GetImage();
        }
    }
}