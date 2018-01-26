using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public interface IGlobalState
    {
        bool HasKey(string key);

        T GetValue<T>(string key);

        void SetValue(string key, object value);

        void Clear();
    }

    public class GlobalState :  IGlobalState
    {
        private static readonly Dictionary<string, object> m_values = new Dictionary<string, object>();

        public bool HasKey(string key)
        {
            return m_values.ContainsKey(key);
        }

        public T GetValue<T>(string key)
        {
            object value;

            m_values.TryGetValue(key, out value);

            return (T)value;
        }

        public void SetValue(string key, object value)
        {
            m_values[key] = value;
        }

        public void Clear()
        {
            m_values.Clear();
        }
    }

}

