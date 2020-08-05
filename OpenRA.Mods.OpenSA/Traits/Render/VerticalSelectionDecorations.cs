using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.OpenSA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Render
{
	public class VerticalSelectionDecorationsInfo : SelectionDecorationsBaseInfo, Requires<InteractableInfo>
	{
		public override object Create(ActorInitializer init) { return new VerticalSelectionDecorations(init.Self, this); }
	}

	public class VerticalSelectionDecorations : SelectionDecorationsBase, IRender
	{
		readonly Interactable interactable;

		public VerticalSelectionDecorations(Actor self, VerticalSelectionDecorationsInfo info)
			: base(info)
		{
			interactable = self.Trait<Interactable>();
		}

		protected override int2 GetDecorationPosition(Actor self, WorldRenderer wr, DecorationPosition pos)
		{
			var bounds = interactable.DecorationBounds(self, wr);
			switch (pos)
			{
				case DecorationPosition.TopLeft: return bounds.TopLeft;
				case DecorationPosition.TopRight: return bounds.TopRight;
				case DecorationPosition.BottomLeft: return bounds.BottomLeft;
				case DecorationPosition.BottomRight: return bounds.BottomRight;
				case DecorationPosition.Top: return new int2(bounds.Left + bounds.Size.Width / 2, bounds.Top);
				default: return bounds.TopLeft + new int2(bounds.Size.Width / 2, bounds.Size.Height / 2);
			}
		}

		protected override IEnumerable<IRenderable> RenderSelectionBox(Actor self, WorldRenderer wr, Color color)
		{
			var bounds = interactable.DecorationBounds(self, wr);
			yield return new SelectionBoxAnnotationRenderable(self, bounds, color);
		}

		protected override IEnumerable<IRenderable> RenderSelectionBars(Actor self, WorldRenderer wr, bool displayHealth, bool displayExtra)
		{
			// Don't render the selection bars for non-selectable actors
			if (!(interactable is Selectable) || (!displayHealth && !displayExtra))
				yield break;

			var bounds = interactable.DecorationBounds(self, wr);
			yield return new VerticalSelectionBarsAnnotationRenderable(self, bounds, displayHealth, displayExtra);
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			yield break;
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			yield return interactable.DecorationBounds(self, wr);
		}
	}
}
