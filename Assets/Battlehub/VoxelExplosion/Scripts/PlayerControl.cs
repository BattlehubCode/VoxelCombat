using UnityEngine;

namespace Battlehub.VoxelExplosion
{
    public class PlayerControl : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_explosionPrefab;

        [SerializeField]
        private float Speed = 1.0f;

        [SerializeField]
        private string WallTag = "Wall";

        private ParticleSystem m_ps;

        private Color[] m_colors = new[]
        {
            Color.red,
            Color.blue,
            Color.yellow,
            Color.green
        };

        // Update is called once per frame
        private void Update()
        {
            transform.position += Vector3.forward * Time.deltaTime * Speed;
        }

        private void LateUpdate()
        {
            if (m_ps != null)
            {
                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[m_ps.main.maxParticles];
                int numParticlesAlive = m_ps.GetParticles(particles);
                for (int i = 0; i < numParticlesAlive; ++i)
                {
                    particles[i].startColor = m_colors[Random.Range(0, m_colors.Length)];
                }
                m_ps.SetParticles(particles, numParticlesAlive);
                m_ps = null;
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if(collision.gameObject.tag == WallTag)
            {
                Explode();
            }
        }

        private void Explode()
        {
            GameObject explosion = Instantiate(m_explosionPrefab, transform.position, transform.rotation, null);
            explosion.transform.localScale = Vector3.one;
            m_ps = explosion.GetComponent<ParticleSystem>();

        }
    }

}


