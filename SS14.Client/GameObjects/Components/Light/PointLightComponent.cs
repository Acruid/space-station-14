﻿using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using SS14.Client.Graphics.Lighting;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.Enums;
using SS14.Shared.Log;
using YamlDotNet.RepresentationModel;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.GameObjects
{
    public class PointLightComponent : Component
    {
        public override string Name => "PointLight";
        public override uint? NetID => NetIDs.POINT_LIGHT;
        public override Type StateType => typeof(PointLightComponentState);

        private ILight Light { get; set; }

        public Color Color
        {
            get => Light.Color;
            set => Light.Color = value;
        }

        private Vector2 _offset = Vector2.Zero;
        public Vector2 Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                UpdateLightPosition();
            }
        }

        public int Radius
        {
            get => Light.Radius;
            set => Light.Radius = value;
        }

        private string _mask;
        protected string Mask
        {
            get => _mask;
            set
            {
                _mask = value;

                var sprMask = IoCManager.Resolve<IResourceCache>().GetSprite(value);
                Light.Mask = sprMask.Texture;
            }
        }

        public LightModeClass ModeClass
        {
            get => Light.LightMode.LightModeClass;
            set => IoCManager.Resolve<ILightManager>().SetLightMode(value, Light);
        }

        public LightMode Mode => Light.LightMode;

        public LightState State
        {
            get => Light.LightState;
            set => Light.LightState = value;
        }

        /// <inheritdoc />
        public override void LoadParameters(YamlMappingNode mapping)
        {
            var mgr = IoCManager.Resolve<ILightManager>();
            Light = mgr.CreateLight();

            YamlNode node;
            if (mapping.TryGetNode("offset", out node))
            {
                Offset = node.AsVector2();
            }

            if (mapping.TryGetNode("radius", out node))
            {
                Radius = node.AsInt();
            }

            if (mapping.TryGetNode("color", out node))
            {
                Color = node.AsHexColor();
            }

            if (mapping.TryGetNode("mask", out node))
            {
                Mask = node.AsString();
            }

            if (mapping.TryGetNode("state", out node))
            {
                State = node.AsEnum<LightState>();
            }

            if (mapping.TryGetNode("mode", out node))
            {
                ModeClass = node.AsEnum<LightModeClass>();
            }
            else
            {
                ModeClass = LightModeClass.Constant;
            }
        }

        /// <inheritdoc />
        public override void Startup()
        {
            base.Startup();
            Owner.GetComponent<ITransformComponent>().OnMove += OnMove;
            UpdateLightPosition();

            var mgr = IoCManager.Resolve<ILightManager>();
            mgr.AddLight(Light);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            Owner.GetComponent<ITransformComponent>().OnMove -= OnMove;
            IoCManager.Resolve<ILightManager>().RemoveLight(Light);
            base.Shutdown();
        }

        private void OnMove(object sender, MoveEventArgs args)
        {
            if(args.NewPosition.IsValidLocation())
                UpdateLightPosition(args.NewPosition);
        }

        protected void UpdateLightPosition(LocalCoordinates newPosition)
        {
            Light.Coordinates = new LocalCoordinates(newPosition.Position + Offset, newPosition.Grid);
            Logger.Debug($"Light: {Owner.Uid} Pos: {newPosition}");
        }

        protected void UpdateLightPosition()
        {
            var transform = Owner.GetComponent<ITransformComponent>();
            UpdateLightPosition(transform.LocalPosition);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            Light.Update(frameTime);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (PointLightComponentState)state;
            State = newState.State;
            Color = newState.Color;
            ModeClass = newState.Mode;
        }
    }
}
