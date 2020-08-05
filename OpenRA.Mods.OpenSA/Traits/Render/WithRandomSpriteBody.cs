using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.OpenSA.Traits.Render
{
	[Desc("Picks sprites from a random actor.")]
	class WithRandomSpriteBodyInfo : WithSpriteBodyInfo
	{
		[FieldLoader.Require]
		[Desc("The sequence names that define the actor sprites.")]
		public readonly string[] Images = null;

		public override object Create(ActorInitializer init) { return new WithRandomSpriteBody(init, this); }
	}

	class WithRandomSpriteBody : WithSpriteBody
	{
		public WithRandomSpriteBody(ActorInitializer init, WithRandomSpriteBodyInfo info)
			: base(init, info)
		{
			var self = init.Self;
			var renderSprites = self.Trait<RenderSprites>();

			var image = info.Images.Random(self.World.SharedRandom);

			var withSpriteBody = self.Info.TraitInfoOrDefault<WithSpriteBodyInfo>();
			if (withSpriteBody != null)
			{
				DefaultAnimation.PlayRepeating(NormalizeSequence(self, withSpriteBody.Sequence));
				DefaultAnimation.ChangeImage(image, withSpriteBody.Sequence);
			}

			renderSprites.UpdatePalette();
		}
	}
}
