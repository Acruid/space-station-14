﻿using OpenTK;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Sprites;
using SS14.Shared;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Lighting
{
    public interface ILight : ILightArea
    {
        int Radius { get; set; }
        Color Color { get; set; }
        Vector4 ColorVec { get; }
        Sprite Mask { get; set; }
        LocalCoordinates Coordinates { get; set; }
        RenderImage RenderTarget { get; }
        LightState LightState { get; set; }
        LightMode LightMode { get; set; }
        void Update(float frametime);
    }
}
