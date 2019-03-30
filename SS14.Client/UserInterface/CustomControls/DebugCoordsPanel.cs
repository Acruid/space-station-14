﻿using System.Text;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Input;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Reflection;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.State;
using SS14.Client.Player;
using SS14.Client.State.States;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Utility;

namespace SS14.Client.UserInterface.CustomControls
{
    internal class DebugCoordsPanel : Panel
    {
        [Dependency] readonly IPlayerManager playerManager;
        [Dependency] readonly IEyeManager eyeManager;
        [Dependency] readonly IInputManager inputManager;
        [Dependency] readonly IResourceCache resourceCache;
        [Dependency] readonly IStateManager stateManager;

        [Dependency] private readonly IDisplayManager _displayManager;

        private Label contents;

        protected override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);

            SizeFlagsHorizontal = SizeFlags.None;

            contents = new Label
            {
                FontOverride =
                    new VectorFont(resourceCache.GetResource<FontResource>(new ResourcePath("/Fonts/CALIBRI.TTF")), 12),
                FontColorShadowOverride = Color.Black,
                MarginTop = 5,
                MarginLeft = 5
            };
            AddChild(contents);

            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = new Color(67, 105, 255, 138),
            };

            MouseFilter = contents.MouseFilter = MouseFilterMode.Ignore;
        }

        protected override void FrameUpdate(RenderFrameEventArgs args)
        {
            if (!VisibleInTree)
            {
                return;
            }

            var stringBuilder = new StringBuilder();

            var mouseScreenPos = inputManager.MouseScreenPosition;
            var screenSize = _displayManager.ScreenSize;

            int mouseWorldMap;
            int mouseWorldGrid;
            GridCoordinates mouseWorldPos;
            ScreenCoordinates worldToScreen;
            IEntity mouseEntity = null;
            try
            {
                var coords = eyeManager.ScreenToWorld(new ScreenCoordinates(mouseScreenPos));
                mouseWorldMap = (int) IoCManager.Resolve<IMapManager>().GetGrid(coords.GridID).ParentMap.Index;
                mouseWorldGrid = (int) coords.GridID;
                mouseWorldPos = coords;
                worldToScreen = eyeManager.WorldToScreen(coords);
                if (stateManager.CurrentState is GameScreen gameScreen)
                {
                    mouseEntity = gameScreen.GetEntityUnderPosition(coords);
                }
            }
            catch
            {
                mouseWorldPos = eyeManager.ScreenToWorld(mouseScreenPos);
                mouseWorldGrid = 0;
                mouseWorldMap = 0;
                worldToScreen = new ScreenCoordinates();
            }

            stringBuilder.AppendFormat(@"Positioning Debug:
Screen Size: {0}
Mouse Pos:
    Screen: {1}
    World: {2}
    W2S: {3}
    Grid: {4}
    Map: {5}
    Entity: {6}
    GUI: {7}
", screenSize, mouseScreenPos, mouseWorldPos, worldToScreen, mouseWorldGrid, mouseWorldMap, mouseEntity,
                UserInterfaceManager.CurrentlyHovered);

            stringBuilder.AppendLine("\nAttached Entity:");
            if (playerManager.LocalPlayer?.ControlledEntity == null)
            {
                stringBuilder.AppendLine("No attached entity.");

            }
            else
            {
                var entityTransform = playerManager.LocalPlayer.ControlledEntity.Transform;
                var playerWorldOffset = entityTransform.WorldPosition;
                var playerScreen = eyeManager.WorldToScreen(playerWorldOffset);

                stringBuilder.AppendFormat(@"    World: {0}
    Screen: {1}
    Grid: {2}
    Map: {3}", playerWorldOffset, playerScreen, entityTransform.GridID, entityTransform.MapID);
            }

            contents.Text = stringBuilder.ToString();
            MinimumSizeChanged();
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new Vector2(175, contents.CombinedMinimumSize.Y + 10);
        }
    }
}
