
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
 
  
    
    /// <summary>
    /// This class represent voxel and ecapsulates its animation and
    /// other functionality
    /// </summary>
    public class VoxelEater : VoxelActor
    {
        /// <summary>
        /// Number of collected (owned) voxels
        /// </summary>
        //private int VoxelsCount;
        private VoxelCell[] m_cells = new VoxelCell[CellsPerRow * CellsPerRow * CellsPerRow];
        private List<int> m_emptyIndices = new List<int>();
        private List<int> m_usedIndices = new List<int>();

        /// <summary>
        /// Texture which is used to modify inner material of VoxelActor
        /// </summary>
        private Texture2D m_tex;

        /// <summary>
        /// Beacause of voxel could be rotated by 180 degreess we need to have flipped texture
        /// </summary>
        private Texture2D m_flippedTex;


        /// <summary>
        /// Size of m_tex;
        /// </summary>
        private const int TEX_SIZE = 8;
 
        protected override void SetMaterials(Material primary, Material secondary)
        {
            primary = Instantiate(primary);
            secondary = Instantiate(secondary);

            base.SetMaterials(primary, secondary);
            m_primaryMaterial.mainTexture = m_tex;
        }

        public override int Health
        {
            get
            {
                return base.Health;
            }
            set
            {
                base.Health = value;
                UpdateCells();
                UpdateUI(true);
            }
        }

        private void UpdateCells()
        {
            EmptyAllCells();

            int fillCellsCount = Mathf.Min(Health, m_emptyIndices.Count);
            for (int i = 0; i < fillCellsCount; ++i)
            {
                FillCell(ToIntVec(GetNextIndex()), false);
            }

            m_tex.Apply(false);
            m_flippedTex.Apply(false);
        }

        public override int Type
        {
            get { return (int)KnownVoxelTypes.Eater;  }
        }

        protected override void AwakeOverride()
        {
            base.AwakeOverride();

            m_tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.ARGB32, false, true);
            m_tex.filterMode = FilterMode.Point;

            m_flippedTex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.ARGB32, false, true);
            m_flippedTex.filterMode = FilterMode.Point;
            for (int i = 0; i < TEX_SIZE; i++)
            {
                for (int j = 0; j < TEX_SIZE; j++)
                {
                    m_tex.SetPixel(i, j, new Color(1, 1, 1, 0));
                    m_flippedTex.SetPixel(i, j, new Color(1, 1, 1, 0));
                }
            }

            m_flippedTex.Apply(false);
            m_tex.Apply(false);
            //m_primaryMaterial.mainTexture = m_tex;
        }

        protected override void OnDestroyOveride()
        {
            base.OnDestroyOveride();
            if (m_tex != null)
            {
                Destroy(m_tex);
            }

            if (m_flippedTex != null)
            {
                Destroy(m_flippedTex);
            }
        }

        protected override void OnFlipped(bool isFlipped)
        {
            base.OnFlipped(isFlipped);
            if (isFlipped)
            {
                m_primaryMaterial.mainTexture = m_flippedTex;
            }
            else
            {
                m_primaryMaterial.mainTexture = m_tex;
            }
        }


        private void FillCell(IntVec coord, bool apply = true)
        {
            int cellIndex = ToInt(coord);
            if (!m_emptyIndices.Contains(cellIndex))
            {
                return;
            }

            IntVec test = ToIntVec(cellIndex);
            Debug.Assert(test.X == coord.X && test.Y == coord.Y && test.Z == coord.Z);

            m_emptyIndices.Remove(cellIndex);
            m_cells[cellIndex].Type = -1;
            InsertSorted(cellIndex, m_usedIndices);

            SetTexturePixel(coord, true, apply);
        }

        private void EmptyCell(IntVec coord, bool apply = true)
        {
            int cellIndex = ToInt(coord);
            if (!m_usedIndices.Contains(cellIndex))
            {
                return;
            }

            IntVec test = ToIntVec(cellIndex);
            Debug.Assert(test.X == coord.X && test.Y == coord.Y && test.Z == coord.Z);

            InsertSorted(cellIndex, m_emptyIndices);
            m_cells[cellIndex].Type = -1;
            m_usedIndices.Remove(cellIndex);

            SetTexturePixel(coord, false, apply);
        }


        public override void BeginEat(Voxel voxel, long tick)
        {
            if(m_emptyIndices.Count == 0)
            {
                Debug.LogError("UnitIndex " + VoxelData.UnitOrAssetIndex + " Has Empty Indices " + m_emptyIndices.Count + " Health " + Health);
                Debug.DebugBreak();
            }


            voxel.Freeze();
            voxel.BeginAssimilate(0);

            VoxelDigestingBehavior digestingBehaviour = voxel.GetComponent<VoxelDigestingBehavior>();

            int emptyIndex = GetNextIndex();

            //m_cells[emptyIndex].Type = -1; //write type of eaten voxel?

            m_emptyIndices.Remove(emptyIndex);
            InsertSorted(emptyIndex, m_usedIndices);

            IntVec position = ToIntVec(emptyIndex);

            digestingBehaviour.Completed += OnDigestingCompleted;
            digestingBehaviour.Digest(this, Stomic, GetPositionLocal(position));
        }

        private void OnDigestingCompleted(VoxelDigestingBehavior digesting)
        {
            digesting.Completed -= OnDigestingCompleted;

            if (IsAcquired)
            {
                IntVec position = GetIntPositionLocal(digesting.TargetPosition);
                SetTexturePixel(position, true, true);
                UpdateUI(true);
            }
        }

        public override void BeginAssimilate(float delay)
        {
            base.BeginAssimilate(delay);
        }

        public override void Assimlate(float delay)
        {
            base.Assimlate(delay);
        }

        public override void Smash(float delay, int health)
        {
            InstantiateParticleEffect(ParticleEffectType.EaterCollapse, delay, health);
            base.Smash(delay, health);
        }

        public override void Explode(float delay, int health)
        {
            InstantiateParticleEffect(ParticleEffectType.EaterExplosion, delay, health);
            base.Explode(delay, health);
        }

        private void InsertSorted(int value, List<int> list)
        {
            if (list.Count == 0)
            {
                list.Add(value);
            }
            else
            {
                if (list[list.Count - 1] < value)
                {
                    list.Add(value);
                }
                else
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        if (list[i] >= value)
                        {
                            list.Insert(i, value);
                            break;
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Get Cell Position in rotation bone local coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Vector3 GetPositionLocal(IntVec pos)
        {
            int x = pos.X;
            int y = pos.Y;
            int z = pos.Z;

            y = CellsPerRow - y - 1;
            z = CellsPerRow - z - 1;

            float cellSize = VoxelSize / CellsPerRow;
            float xF = -VoxelSize / 2.0f + cellSize / 2.0f;
            float yF = -VoxelSize / 2.0f + cellSize / 2.0f;
            float zF = -VoxelSize / 2.0f + cellSize / 2.0f;

            xF += x * cellSize;
            yF += y * cellSize;
            zF += z * cellSize;

            return new Vector3(xF, yF, zF);
        }

        /// <summary>
        /// Get position in local integer space
        /// </summary>
        /// <param name="posLocal">position in local space</param>
        /// <returns></returns>
        private static IntVec GetIntPositionLocal(Vector3 posLocal)
        {
            float cellSize = VoxelSize / CellsPerRow;
            Vector3 originOffset = new Vector3(VoxelSize / 2 - cellSize / 2, VoxelSize / 2 - cellSize / 2, VoxelSize / 2 - cellSize / 2);
            posLocal = posLocal + originOffset;
            posLocal = posLocal / cellSize;

            IntVec result = new IntVec(posLocal);

            result.Y = CellsPerRow - result.Y - 1;
            result.Z = CellsPerRow - result.Z - 1;

            return result;
        }

        private static int ToInt(IntVec vec)
        {
            return ((vec.X * CellsPerRow) + vec.Y) * CellsPerRow + vec.Z;
        }

        public static IntVec ToIntVec(int index)
        {
            int x = index / (CellsPerRow * CellsPerRow);
            int y = (index - x * CellsPerRow * CellsPerRow) / CellsPerRow;
            int z = index % CellsPerRow;

            return new IntVec(x, y, z);
        }

        public static readonly int[] FillOrder = new int[]
        {
            ToInt(new IntVec(1, 1, 1)), ToInt(new IntVec(1, 1, 2)), ToInt(new IntVec(1, 2, 1)), ToInt(new IntVec(1, 2, 2)),
            ToInt(new IntVec(2, 1, 1)), ToInt(new IntVec(2, 1, 2)), ToInt(new IntVec(2, 2, 1)), ToInt(new IntVec(2, 2, 2)),

            ToInt(new IntVec(0, 1, 1)), ToInt(new IntVec(0, 1, 2)), ToInt(new IntVec(0, 2, 1)), ToInt(new IntVec(0, 2, 2)),
            ToInt(new IntVec(3, 1, 1)), ToInt(new IntVec(3, 1, 2)), ToInt(new IntVec(3, 2, 1)), ToInt(new IntVec(3, 2, 2)),

            ToInt(new IntVec(1, 0, 1)), ToInt(new IntVec(1, 0, 2)), ToInt(new IntVec(2, 0, 1)), ToInt(new IntVec(2, 0, 2)),
            ToInt(new IntVec(1, 3, 1)), ToInt(new IntVec(1, 3, 2)), ToInt(new IntVec(2, 3, 1)), ToInt(new IntVec(2, 3, 2)),

            ToInt(new IntVec(1, 1, 0)), ToInt(new IntVec(1, 2, 0)), ToInt(new IntVec(2, 1, 0)), ToInt(new IntVec(2, 2, 0)),
            ToInt(new IntVec(1, 1, 3)), ToInt(new IntVec(1, 2, 3)), ToInt(new IntVec(2, 1, 3)), ToInt(new IntVec(2, 2, 3)),

            ToInt(new IntVec(0, 1, 3)), ToInt(new IntVec(0, 2, 3)), ToInt(new IntVec(3, 1, 3)), ToInt(new IntVec(3, 2, 3)),
            ToInt(new IntVec(1, 0, 3)), ToInt(new IntVec(2, 0, 3)), ToInt(new IntVec(1, 3, 3)), ToInt(new IntVec(2, 3, 3)),

            ToInt(new IntVec(0, 1, 0)), ToInt(new IntVec(0, 2, 0)), ToInt(new IntVec(3, 1, 0)), ToInt(new IntVec(3, 2, 0)),
            ToInt(new IntVec(1, 0, 0)), ToInt(new IntVec(2, 0, 0)), ToInt(new IntVec(1, 3, 0)), ToInt(new IntVec(2, 3, 0)),

            ToInt(new IntVec(0, 0, 1)), ToInt(new IntVec(0, 0, 2)), ToInt(new IntVec(0, 3, 1)), ToInt(new IntVec(0, 3, 2)),
            ToInt(new IntVec(3, 3, 1)), ToInt(new IntVec(3, 3, 2)), ToInt(new IntVec(3, 0, 1)), ToInt(new IntVec(3, 0, 2)),

            ToInt(new IntVec(0, 0, 0)), ToInt(new IntVec(0, 0, 3)), ToInt(new IntVec(0, 3, 0)), ToInt(new IntVec(0, 3, 3)),
            ToInt(new IntVec(3, 0, 0)), ToInt(new IntVec(3, 0, 3)), ToInt(new IntVec(3, 3, 0)), ToInt(new IntVec(3, 3, 3)),
        };

        static VoxelEater()
        {
            Debug.Assert(FillOrder.Distinct().Count() == 64);
        }
        private int GetNextIndex()
        {
            return FillOrder[m_usedIndices.Count];
        }

        private void UpdateUI(bool animate)
        {
            if(m_ui == null)
            {
                return;
            }
            for(int i = 0; i < m_ui.Count; ++i)
            {
                float healthPerentage = ((float)m_usedIndices.Count) / (m_usedIndices.Count + m_emptyIndices.Count);
                m_ui[i].UpdateProgress(animate, healthPerentage);
            }
        }

        protected override void OnSelect(int playerIndex)
        {
            base.OnSelect(playerIndex);
            UpdateUI(false);
        }

        protected override void OnUnselect(int playerIndex)
        {
            base.OnUnselect(playerIndex);
        }


        public override void ReadFrom(VoxelData data)
        {
            base.ReadFrom(data);

            EmptyAllCells();

            int fillCellsCount = Mathf.Min(Health, m_emptyIndices.Count);
            for (int i = 0; i < fillCellsCount; ++i)
            {
                FillCell(ToIntVec(GetNextIndex()), false);
            }
            m_tex.Apply(false);
            m_flippedTex.Apply(false);

            UpdateUI(false);
        }

        private void EmptyAllCells()
        {
            m_usedIndices = new List<int>();
            m_emptyIndices = new List<int>();

            for (int i = 0; i < m_cells.Length; ++i)
            {
                m_emptyIndices.Add(i);
                SetTexturePixel(ToIntVec(i), false, false);
            }

            m_tex.Apply(false);
            m_flippedTex.Apply(false);
        }

        private void FillAllCells()
        {
            m_usedIndices = new List<int>();
            m_emptyIndices = new List<int>();

            for (int i = 0; i < m_cells.Length; ++i)
            {
                m_usedIndices.Add(i);
                SetTexturePixel(ToIntVec(i), true, false);
            }

            m_tex.Apply(false);
            m_flippedTex.Apply(false);
        }

  
        /// <summary>
        /// Voxel cells per row
        /// </summary>
        private const int CellsPerRow = 4;

        /// <summary>
        /// Size of voxel in world units
        /// </summary>
        private const float VoxelSize = 2.0f;

        private void SetTexturePixel(IntVec pos, bool v, bool apply)
        {
            int x = pos.X;
            int y = pos.Y;
            int z = pos.Z;

            int texY = z + (x / 2) * CellsPerRow;
            int texX = y + (x % 2) * CellsPerRow;

            m_tex.SetPixel(TEX_SIZE - texX - 1, TEX_SIZE - texY - 1, new Color(1, 1, 1, v ? 1 : 0));

            if (apply)
            {
                m_tex.Apply(false);
            }

            z = CellsPerRow - z - 1;
            y = CellsPerRow - y - 1;

            texY = z + (x / 2) * CellsPerRow;
            texX = y + (x % 2) * CellsPerRow;

            m_flippedTex.SetPixel(TEX_SIZE - texX - 1, TEX_SIZE - texY - 1, new Color(1, 1, 1, v ? 1 : 0));

            if (apply)
            {
                m_flippedTex.Apply(false);
            }

        }

    }

}
