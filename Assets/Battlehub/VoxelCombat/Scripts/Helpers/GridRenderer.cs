using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class GridRenderer : MonoBehaviour, IGL
    {
        [SerializeField]
        private Material m_mat;

        [SerializeField]
        private float m_gridSize = 100;

        [SerializeField]
        private float m_cellSize = 2;

        [SerializeField]
        private Transform m_target;

        [SerializeField]
        public int CullingMask;

        [SerializeField]
        private float m_verticalOffset = 0.5f;

        public Transform Target
        {
            get { return m_target; }
            set { m_target = value; }
        }

        private void Start()
        {
            if (GLRenderer.Instance != null)
            {
                GLRenderer.Instance.Add(this);
            }
        }

        private void OnDestroy()
        {
            if(GLRenderer.Instance != null)
            {
                GLRenderer.Instance.Remove(this);
            }
        }

        public void Draw(int cullingMask)
        {
            if(CullingMask != cullingMask)
            {
                return;
            }

            if (!m_mat)
            {
                Debug.LogError("Please Assign a material on the inspector");
                return;
            }

            if (!m_target)
            {
                m_target = transform;
            }

            Vector3 offset = new Vector3(
                Mathf.Round(m_target.position.x / m_cellSize) * m_cellSize,
                m_verticalOffset,
                Mathf.Round(m_target.position.z / m_cellSize) * m_cellSize);

            Matrix4x4 m = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one);

            GL.PushMatrix();

            m_mat.SetPass(0);
            GL.Begin(GL.LINES);
            for (int i = 0; i <= m_gridSize; ++i)
            {
                Vector3 p1 = new Vector3(-m_gridSize * m_cellSize / 2, 0, i * m_cellSize - m_gridSize * m_cellSize / 2);
                Vector3 p2 = new Vector3(m_gridSize * m_cellSize / 2, 0, i * m_cellSize - m_gridSize * m_cellSize / 2);
                p1 = m.MultiplyPoint(p1);
                p2 = m.MultiplyPoint(p2);

                if (Mathf.Abs(p1.z) < 0.00001f)
                {
                    GL.Color(Color.red);
                }
                else
                {
                    GL.Color(Color.white);
                }

                GL.Vertex(p1);
                GL.Vertex(p2);

                Vector3 p3 = new Vector3(i * m_cellSize - m_gridSize * m_cellSize / 2, 0, -m_gridSize * m_cellSize / 2);
                Vector3 p4 = new Vector3(i * m_cellSize - m_gridSize * m_cellSize / 2, 0, m_gridSize * m_cellSize / 2);
                p3 = m.MultiplyPoint(p3);
                p4 = m.MultiplyPoint(p4);

                if (Mathf.Abs(p3.x) < 0.00001f)
                {
                    GL.Color(Color.blue);
                }
                else
                {
                    GL.Color(Color.white);
                }


                GL.Vertex(p3);
                GL.Vertex(p4);
            }
            GL.End();
            GL.PopMatrix();
        }
    }

}
