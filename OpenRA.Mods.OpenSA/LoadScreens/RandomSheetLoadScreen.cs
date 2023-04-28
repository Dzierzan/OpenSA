#region Copyright & License Information
/*
 * Copyright The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.LoadScreens;
using OpenRA.Primitives;

namespace OpenRA.Mods.OpenSA.LoadScreens
{
	public abstract class RandomSheetLoadScreen : BlankLoadScreen
	{
		int totalCount = -1;

		protected Dictionary<string, string> Info { get; private set; }
		float dpiScale = 1;

		Sheet sheet;
		int density;

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			base.Init(modData, info);
			Info = info;
		}

		public abstract void DisplayInner(Renderer r, Sheet s, int density);

		public override void Display()
		{
			if (Game.Renderer == null)
				return;

			// Check for window DPI changes
			// We can't trust notifications to be working during initialization, so must do this manually
			var scale = Game.Renderer.WindowScale;
			if (dpiScale != scale)
			{
				dpiScale = scale;

				// Force images to be reloaded on the next display
				sheet?.Dispose();

				sheet = null;
				totalCount = -1;
			}

			if (Info.ContainsKey("Image") && totalCount != Game.CosmeticRandom.TotalCount)
			{
				var key = "Image";
				density = 1;
				if (dpiScale > 2 && Info.ContainsKey("Image3x"))
				{
					key = "Image3x";
					density = 3;
				}
				else if (dpiScale > 1 && Info.ContainsKey("Image2x"))
				{
					key = "Image2x";
					density = 2;
				}

				var files = Info[key].Split(',');
				var file = files[0];
				if (Game.CosmeticRandom.TotalCount == 0)
					Game.CosmeticRandom.Next();
				else
					file = files.Skip(1).Random(Game.CosmeticRandom);

				totalCount = Game.CosmeticRandom.TotalCount;

				using (var stream = ModData.DefaultFileSystem.Open(file.Trim()))
				{
					sheet = new Sheet(SheetType.BGRA, stream);
					sheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;
				}
			}

			Game.Renderer.BeginUI();
			DisplayInner(Game.Renderer, sheet, density);
			Game.Renderer.EndFrame(new NullInputHandler());
		}

		protected static Sprite CreateSprite(Sheet s, int density, Rectangle rect)
		{
			return new Sprite(s, density * rect, TextureChannel.RGBA, 1f / density);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				sheet?.Dispose();

			base.Dispose(disposing);
		}
	}
}
