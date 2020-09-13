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

            if (self.TraitOrDefault<Colony>() != null)
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
