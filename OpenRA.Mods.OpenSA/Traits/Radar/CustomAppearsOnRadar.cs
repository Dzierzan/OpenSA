#region Copyright & License Information
/*
 * Copyright 2019-2020 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Radar
{
    public class CustomAppearsOnRadarInfo : TraitInfo
    {
        public readonly CVec Offset = CVec.Zero;

        public override object Create(ActorInitializer init)
        {
            return new CustomAppearsOnRadar(this);
        }
    }

    public class CustomAppearsOnRadar : IRadarSignature
    {
        private readonly CustomAppearsOnRadarInfo info;

        public CustomAppearsOnRadar(CustomAppearsOnRadarInfo info)
        {
            this.info = info;
        }

        void IRadarSignature.PopulateRadarSignatureCells(Actor self, List<(CPos, Color)> destinationBuffer)
        {
            var color = self.Owner.Color;

            if (self.Owner.InternalName == "Neutral" || self.Owner.InternalName == "Creeps")
                color = Color.FromArgb(227, 217, 205);

            if (self.TraitOrDefault<Colony>() != null || self.TraitOrDefault<DefeatedColony>() != null)
            {
                for (var y = 0; y < 6; y++)
                    for (var x = 0; x < 6; x++)
                        destinationBuffer.Add((self.Location + new CVec(x, y) + info.Offset, color));
            }
            else
                destinationBuffer.Add((self.Location, color));
        }
    }
}
