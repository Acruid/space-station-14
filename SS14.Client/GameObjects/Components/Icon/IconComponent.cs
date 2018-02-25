using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Serialization;
using SS14.Shared.IoC;

namespace SS14.Client.GameObjects
{
    public class IconComponent : Component
    {
        private string _iconName;
        public override string Name => "Icon";

        public Sprite Icon { get; private set; }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SetIcon(_iconName);
        }

        /// <inheritdoc />
        public override void ExposeData(EntitySerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _iconName, "icon", null);
        }

        private void SetIcon(string name)
        {
            Icon = IoCManager.Resolve<IResourceCache>().GetSprite(name);
        }
    }
}
