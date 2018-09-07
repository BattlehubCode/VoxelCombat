
namespace Battlehub.VoxelCombat
{
    public class VoxelBomb : VoxelActor
    {
        public override int Type
        {
            get
            {
                return m_isPreview ?
                  (int)KnownVoxelTypes.BombPreview :
                  (int)KnownVoxelTypes.Bomb;
            }
        }

        public override void Explode(float delay, int health)
        {
            InstantiateParticleEffect(ParticleEffectType.BombExplosion, delay, health);
            InstantiateParticleEffect(ParticleEffectType.BombCollapse, delay, health);
            base.Explode(delay, health);
        }

        public override void Smash(float delay, int health)
        {
            InstantiateParticleEffect(ParticleEffectType.BombExplosion, delay, health);
            InstantiateParticleEffect(ParticleEffectType.BombCollapse, delay, health);
            base.Smash(delay, health);
        }
    }

}
