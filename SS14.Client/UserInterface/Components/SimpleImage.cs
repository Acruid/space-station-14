﻿using SFML.Graphics;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Components
{
    /// <summary>
    ///     Displays an image on the screen. This is the same as a Screen, except this shrinks to fit
    ///     an image, where as a Screen stretches to fit an image.
    /// </summary>
    public class SimpleImage : Screen
    {
        /// <summary>
        ///     Sprite to draw inside of this control.
        /// </summary>
        public string Sprite
        {
            set => BackgroundImage = new Sprite(_resourceCache.GetSprite(value));
        }

        /// <inheritdoc />
        protected override void OnCalcRect()
        {
            // shrink to fit image size
            var fr = BackgroundImage.GetLocalBounds();
            _size = new Vector2i((int) fr.Width, (int) fr.Height);

            _clientArea = new Box2i(new Vector2i(), _size);
        }
    }
}
