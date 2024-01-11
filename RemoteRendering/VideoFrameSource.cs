using System;
using Godot;
using FFmpeg.AutoGen;

namespace Godot.RemoteRendering
{
    [GlobalClass]
    public partial class VideoFrameSource : Node3D
    {
        private Viewport viewport;
        private Image frameImage;

        public VideoFrameSource()
        {
            
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            GD.Print("EnterTree2");
            viewport = GetTree().CurrentScene.GetViewport();
            frameImage = viewport.GetTexture().GetImage();
        }

        public int GetImageWidth()
        {
            return frameImage.GetWidth();
        }

        public int GetImageHeight()
        {
            return frameImage.GetHeight();
        }

        public Image GetFrameData()
        {
            return frameImage;
        }

        public Image.Format GetFrameFormat()
        {
            return frameImage.GetFormat();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            frameImage = viewport.GetTexture().GetImage();
        }
    }
}