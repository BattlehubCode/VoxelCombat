using System.Collections.Generic;
namespace Battlehub.VoxelCombat
{
    public abstract class Pool<T>
    {
        private Queue<T> m_objects;

        private int m_poolSize;

        public void Initialize(int size)
        {
            m_poolSize = size;

            m_objects = new Queue<T>();
            for (int i = 0; i < m_poolSize; ++i)
            {
                T obj = Instantiate(i);
                m_objects.Enqueue(obj);
            }
        }

        protected abstract T Instantiate(int index);

        protected abstract void Destroy(T obj);

        public void SetPoolSize(int size)
        {
            if (size < 0)
            {
                throw new System.ArgumentOutOfRangeException("size");
            }

            if (size < m_poolSize)
            {
                for (int i = size; i < m_objects.Count; ++i)
                {
                    T obj = m_objects.Dequeue();
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
            }
            else if (size > m_poolSize)
            {
                for (int i = m_poolSize; i < size; ++i)
                {
                    T obj = Instantiate(i);
                    m_objects.Enqueue(obj);
                }
            }

            m_poolSize = size;
        }

        public virtual T Acquire()
        {
            if (m_objects.Count == 0)
            {
                SetPoolSize(m_poolSize * 2);
            }

            T obj = m_objects.Dequeue();
            return obj;
        }

        public virtual void Release(T obj)
        {
            m_objects.Enqueue(obj);

            if (m_objects.Count > m_poolSize)
            {
                m_poolSize = m_objects.Count;
            }
        }
    }

}

