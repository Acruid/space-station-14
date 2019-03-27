﻿using SS14.Client.Graphics;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.EntitySystemMessages;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using SS14.Client.Graphics.Overlays;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;

namespace SS14.Client.GameObjects
{
    public class EffectSystem : EntitySystem
    {
        [Dependency] IGameTiming gameTiming;

        [Dependency] IResourceCache resourceCache;

        [Dependency] IEyeManager eyeManager;

        [Dependency] IOverlayManager overlayManager;

        private readonly List<Effect> _Effects = new List<Effect>();
        private TimeSpan lasttimeprocessed = TimeSpan.Zero;

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);

            var overlay = new EffectOverlay(this);
            overlayManager.AddOverlay(overlay);
        }

        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();

            RegisterMessageType<EffectSystemMessage>();
        }

        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            switch (message)
            {
                case EffectSystemMessage msg:
                    CreateEffect(msg);
                    break;
            }
        }

        public void CreateEffect(EffectSystemMessage message)
        {
            var gametime = gameTiming.CurTime;

            /*
            // TODO: Fix this, it doesn't work. Probably because CurTime isn't synchronized with the server.
            if (gametime > message.DeathTime) //Did we already die in transit? That's pretty troubling isn't it
            {
                Logger.Warning(string.Format("Effect using sprite {0} died in transit to the client", message.EffectSprite), message);
                return;
            }
            */

            //Create effect from creation message
            var effect = new Effect(message, resourceCache);

            //Remove this
            effect.Age = gametime;
            effect.Deathtime = effect.Age + TimeSpan.FromSeconds(4);

            //Age the effect through a single update to the previous update tick of the effect system
            //effect.Update((float)((lasttimeprocessed - effect.Age).TotalSeconds));

            _Effects.Add(effect);
        }

        public override void FrameUpdate(float frameTime)
        {
            lasttimeprocessed = IoCManager.Resolve<IGameTiming>().CurTime;

            for (int i = 0; i < _Effects.Count; i++)
            {
                var effect = _Effects[i];

                //Update variables of the effect via its deltas
                effect.Update(frameTime);

                //These effects have died
                if (effect.Age > effect.Deathtime)
                {
                    //Remove from the effects list and decrement the iterator
                    _Effects.Remove(effect);
                    i--;
                }
            }
        }

        private class Effect
        {
            /// <summary>
            /// Effect Sprite
            /// This is the sprite that will be drawn as the "effect".
            /// </summary>
            public Texture EffectSprite { get; set; }

            /// <summary>
            /// Effect position relative to the emit position
            /// </summary>
            public GridCoordinates Coordinates;

            /// <summary>
            /// Where the emitter was when the effect was first emitted
            /// </summary>
            public GridCoordinates EmitterCoordinates;

            /// <summary>
            /// Effect's x/y velocity
            /// </summary>
            public Vector2 Velocity = new Vector2(0, 0);

            /// <summary>
            /// Effect's x/y acceleration
            /// </summary>
            public Vector2 Acceleration = new Vector2(0, 0);

            /// <summary>
            /// Effect's radial velocity - relative to EmitterPosition
            /// </summary>
            public float RadialVelocity = 0f;

            /// <summary>
            /// Effect's radial acceleration
            /// </summary>
            public float RadialAcceleration = 0f;

            /// <summary>
            /// Effect's tangential velocity - relative to EmitterPosition
            /// </summary>
            public float TangentialVelocity = 0f;

            /// <summary>
            /// Effect's tangential acceleration
            /// </summary>
            public float TangentialAcceleration = 0f;

            /// <summary>
            /// Effect's spin about its center in radians
            /// </summary>
            public float Rotation = 0f;

            /// <summary>
            /// Rate of change of effect's spin
            /// </summary>
            public float RotationRate = 0f;

            /// <summary>
            /// Effect's current size
            /// </summary>
            public Vector2 Size = new Vector2(1f, 1f);

            /// <summary>
            /// Rate of change of effect's size change
            /// </summary>
            public float SizeDelta = 0f;

            /// <summary>
            /// Effect's current color
            /// </summary>
            public Vector4 Color = new Vector4(255, 255, 255, 255);

            /// <summary>
            /// Rate of change of effect's color
            /// </summary>
            public Vector4 ColorDelta = new Vector4(0, 0, 0, 0);

            /// <summary>
            ///     True if the effect is affected by lighting.
            /// </summary>
            public bool Shaded = true;

            /// <summary>
            /// Effect's age -- from 0f
            /// </summary>
            public TimeSpan Age = TimeSpan.Zero;

            /// <summary>
            /// Time after which the effect will "die"
            /// </summary>
            public TimeSpan Deathtime = TimeSpan.FromSeconds(1);

            public Effect(EffectSystemMessage effectcreation, IResourceCache resourceCache)
            {
                EffectSprite = resourceCache
                    .GetResource<TextureResource>(new ResourcePath("/Textures/") / effectcreation.EffectSprite).Texture;
                Coordinates = effectcreation.Coordinates;
                EmitterCoordinates = effectcreation.EmitterCoordinates;
                Velocity = effectcreation.Velocity;
                Acceleration = effectcreation.Acceleration;
                RadialVelocity = effectcreation.RadialVelocity;
                RadialAcceleration = effectcreation.RadialAcceleration;
                TangentialVelocity = effectcreation.TangentialVelocity;
                TangentialAcceleration = effectcreation.TangentialAcceleration;
                Age = effectcreation.Born;
                Deathtime = effectcreation.DeathTime;
                Rotation = effectcreation.Rotation;
                RotationRate = effectcreation.RotationRate;
                Size = effectcreation.Size;
                SizeDelta = effectcreation.SizeDelta;
                Color = effectcreation.Color;
                ColorDelta = effectcreation.ColorDelta;
                Shaded = effectcreation.Shaded;
            }

            public void Update(float frameTime)
            {
                Age += TimeSpan.FromSeconds(frameTime);
                if (Age >= Deathtime)
                    return;

                Velocity += Acceleration * frameTime;
                RadialVelocity += RadialAcceleration * frameTime;
                TangentialVelocity += TangentialAcceleration * frameTime;

                var deltaPosition = new Vector2(0f, 0f);

                //If we have an emitter we can do special effects around that emitter position
                if (EmitterCoordinates.IsValidLocation())
                {
                    //Calculate delta p due to radial velocity
                    var positionRelativeToEmitter =
                        Coordinates.ToWorld().Position - EmitterCoordinates.ToWorld().Position;
                    var deltaRadial = RadialVelocity * frameTime;
                    deltaPosition = positionRelativeToEmitter * (deltaRadial / positionRelativeToEmitter.Length);

                    //Calculate delta p due to tangential velocity
                    var radius = positionRelativeToEmitter.Length;
                    if (radius > 0)
                    {
                        var theta = (float) Math.Atan2(positionRelativeToEmitter.Y, positionRelativeToEmitter.X);
                        theta += TangentialVelocity * frameTime;
                        deltaPosition += new Vector2(radius * (float) Math.Cos(theta), radius * (float) Math.Sin(theta))
                                         - positionRelativeToEmitter;
                    }
                }

                //Calculate new position from our velocity as well as possible rotation/movement around emitter
                deltaPosition += Velocity * frameTime;
                Coordinates = new GridCoordinates(Coordinates.Position + deltaPosition, Coordinates.Grid);

                //Finish calculating new rotation, size, color
                Rotation += RotationRate * frameTime;
                Size += SizeDelta * frameTime;
                Color += ColorDelta * frameTime;
            }
        }

        private static Color ToColor(Vector4 color)
        {
            color = Vector4.Clamp(color / 255f, Vector4.Zero, Vector4.One);

            return new Color(color.X, color.Y, color.Z, color.W);
        }

        private sealed class EffectOverlay : Overlay
        {
            public override bool AlwaysDirty => true;
            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            private readonly Shader _unshadedShader;
            private readonly EffectSystem _owner;

            public EffectOverlay(EffectSystem owner) : base("EffectSystem")
            {
                _owner = owner;
                _unshadedShader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("unshaded").Instance();
            }

            protected override void Draw(DrawingHandle handle)
            {
                var map = _owner.eyeManager.CurrentMap;

                var shaded = (DrawingHandleWorld) handle;
                var unshaded = (DrawingHandleWorld) NewHandle(_unshadedShader);

                foreach (var effect in _owner._Effects)
                {
                    if (effect.Coordinates.MapId != map)
                    {
                        continue;
                    }

                    // NOTE TO FUTURE READERS:
                    // Yes, due to how this is implemented, unshaded is always on top of shaded.
                    // If you want to rework it to be properly defined, be my guest.
                    var usingHandle = effect.Shaded ? shaded : unshaded;

                    usingHandle.SetTransform(
                        effect.Coordinates.ToWorld().Position,
                        new Angle(-effect.Rotation), effect.Size);
                    var effectSprite = effect.EffectSprite;
                    usingHandle.DrawTexture(effectSprite, -((Vector2) effectSprite.Size / EyeManager.PIXELSPERMETER) / 2, ToColor(effect.Color));
                }
            }
        }
    }
}
