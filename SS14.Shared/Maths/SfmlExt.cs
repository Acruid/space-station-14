using SFML.System;
using System;
using OpenTK;
using OpenTK.Graphics;

namespace SS14.Shared.Maths
{
    public static class SfmlExt
    {
        // Vector2i
        public static int LengthSquared(this Vector2i vec) => vec.X * vec.X + vec.Y * vec.Y;
        public static float Length(this Vector2i vec)      => (float)Math.Sqrt(LengthSquared(vec));
        public static Vector2 ToFloat(this Vector2i vec)  => new Vector2(vec.X, vec.Y);

        // Vector2f
        public static float LengthSquared(this Vector2f vec) => vec.X * vec.X + vec.Y * vec.Y;
        public static float Length(this Vector2f vec)        => (float)Math.Sqrt(LengthSquared(vec));
        public static Vector2i Round(this Vector2f vec)      => new Vector2i((int)Math.Round(vec.X), (int)Math.Round(vec.Y));

        // Color
        public static uint ToInt(this Color4 color)
            => unchecked((uint)(
                (color.R << 16)
                | (color.G << 8)
                | (color.B << 0)
                | (color.A << 24)));

        public static Color4 IntToColor(uint color)
            => unchecked(new Color4(
                (byte)(color >> 16),
                (byte)(color >> 8),
                (byte)(color >> 0),
                (byte)(color >> 24)));
    }
}
