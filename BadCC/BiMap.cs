using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadCC
{
    class BiMap<T1, T2> : IEnumerable<KeyValuePair<T1, T2>> 
    {
        private Dictionary<T1, T2> oneToTwo = new Dictionary<T1, T2>();
        private Dictionary<T2, T1> twoToOne = new Dictionary<T2, T1>();

        public void Add(T1 first, T2 second)
        {
            if(oneToTwo.ContainsKey(first) || twoToOne.ContainsKey(second))
            {
                throw new Exception("Duplicate first or second value");
            }

            oneToTwo.Add(first, second);
            twoToOne.Add(second, first);
        }

        public void Add(KeyValuePair<T1, T2> value)
        {
            Add(value.Key, value.Value);
        }

        public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator()
        {
            return oneToTwo.GetEnumerator();
        }

        public bool TryGetValueFirst(T1 key, out T2 value)
        {
            return oneToTwo.TryGetValue(key, out value);
        }

        public bool TryGetValueSecond(T2 key, out T1 value)
        {
            return twoToOne.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
