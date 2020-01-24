using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
 
namespace OpenRA.Mods.Swarm_Assault.Traits.Colony
{
    public enum ExplosionType { Footprint, CenterPosition }
    
    public class ColonyInfo : ITraitInfo, Requires<HealthInfo>
    {
        public readonly int BitFireDelay = 50;
        public readonly int NumberOfBits = 8;
        public readonly int MinRange = 5;
        public readonly int MaxRange = 10;
        public readonly int ResurrectHealth = 10;
        public readonly string Weapon = "colony_bit";
        public readonly string Explode = "dieBuilding";
        public readonly string CaptureSound = "sounds|POWERUP.SDF";
        public readonly string LostSound = "sounds|POWERDOWN.SDF";
        public readonly string ColonyExplosionSound = "sounds|COLONYEXPLODE.SDF";
 
        public object Create(ActorInitializer init)
        {
            return new Colony(init, this);
        }
    }

    public class Colony : INotifyKilled, ITick
    {
        private ColonyInfo info;
        private WeaponInfo weaponInfo;
        private WeaponInfo explodesInfo;
        private Health health;
        private Dictionary<OpenRA.Player, int> bitPickers = new Dictionary<OpenRA.Player, int>();
        private int fireBitTimer;
 
        public Colony(ActorInitializer init, ColonyInfo info)
        {
            this.info = info;
            Game.ModData.DefaultRules.Weapons.TryGetValue(info.Weapon, out weaponInfo);
            Game.ModData.DefaultRules.Weapons.TryGetValue(info.Explode, out explodesInfo);
            health = init.Self.Trait<Health>();
            health.RemoveOnDeath = false;
        }
 
        void INotifyKilled.Killed(Actor self, AttackInfo e)
        {
            Game.Sound.Play(SoundType.World, info.LostSound, self.CenterPosition);
            self.ChangeOwner(self.World.Players.First(player => player.InternalName == "Neutral"));
            CancelProductions(self);
            fireBitTimer = info.BitFireDelay;
        }
 
        private List<CPos> GetBitTargetTiles(Actor self)
        {
            var cell = self.World.Map.CellContaining(self.CenterPosition);
            var tiles = new List<CPos>();
            for (var i = 0; tiles.Count < info.NumberOfBits; i++)
            {
                tiles = self.World.Map
                    .FindTilesInAnnulus(cell, info.MinRange, info.MaxRange + i)
                    .Where(tilePosition =>
                    {
                        if (self.World.Map.GetTerrainInfo(tilePosition).Type == "Water")
                            return false;
 
                        if (self.World.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(tilePosition) != null)
                            return false;
 
                        return !self.World.ActorMap.AnyActorsAt(tilePosition);
                    }).ToList();
            }
 
            return tiles;
        }
 
        private void LaunchBits(Actor self, CPos tile)
        {
            var projectile = new ProjectileArgs
            {
                Weapon = weaponInfo,
                Source = self.CenterPosition,
                SourceActor = self,
                PassiveTarget = self.World.Map.CenterOfCell(tile),
                RangeModifiers = new int[0],
                DamageModifiers = new int[0],
                InaccuracyModifiers = new int[0]
            };
            if (weaponInfo == null) throw new YamlException("welp nope");
            self.World.AddFrameEndTask(world => world.Add(projectile.Weapon.Projectile.Create(projectile)));
        }

        private void Explodes(Actor self)
        { 
            //var explosion = info.weaponInfo;
            if (Colony.explodesInfo == null) throw new YamlException("welp nope");

            Colony.explodesInfo.Impact(Target.FromActor(self), self, Enumerable.Empty<int>());
            Game.Sound.Play(SoundType.World, info.ColonyExplosionSound, self.CenterPosition);
        }

        private void CancelProductions(Actor self)
        {
            foreach (var productionQueue in self.TraitsImplementing<ProductionQueue>())
            {
                while (true)
                {
                    var producing = productionQueue.AllQueued();
 
                    if (!producing.Any())
                        break;
 
                    foreach (var productionItem in producing)
                        productionQueue.EndProduction(productionItem);
                }
            }
        }
 
        public void PickBit(OpenRA.Player player)
        {
            if (!bitPickers.ContainsKey(player))
                bitPickers.Add(player, 0);
 
            bitPickers[player]++;
        }
 
        public void Tick(Actor self)
        {
            if (fireBitTimer > 0)
            {
                if (--fireBitTimer == 0)
                {
                    var tiles = GetBitTargetTiles(self);

                    for (var i = 0; i < info.NumberOfBits; i++)
                    {
                        var tile = tiles[self.World.SharedRandom.Next(0, tiles.Count)];
                        tiles.Remove(tile);
                        LaunchBits(self, tile);
                        Explodes(self);
                    }
                }
            }
 
            if (!self.IsDead)
                return;
 
            if (bitPickers.Values.Sum() < info.NumberOfBits)
                return;
 
            var bits = bitPickers.Aggregate((a, b) => a.Value > b.Value ? a : b).Value;
            var newOwner = bitPickers.First(entry => entry.Value == bits).Key;
 
            if (bitPickers.Values.Count(value => value == bits) > 1)
                newOwner = self.World.Players.First(player => player.InternalName == "Creeps");
 
            bitPickers.Clear();
            health.Resurrect(self, self);
            health.InflictDamage(self, self, new Damage(health.MaxHP - info.ResurrectHealth), true);
            self.ChangeOwner(newOwner);
            Game.Sound.Play(SoundType.World, info.CaptureSound, self.CenterPosition);
        }
    }
}