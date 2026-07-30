namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 시드로 완전히 재현되는 난수. xorshift32.
    ///
    /// System.Random을 쓰지 않는 이유가 있다 — 내부 알고리즘이 .NET 버전에 따라
    /// 바뀌었다. 같은 시드가 같은 맵을 만들어야 하는데 런타임에 따라 달라지면
    /// "시드 하나가 맵 전체"라는 전제가 무너진다.
    ///
    /// UnityEngine.Random도 쓰지 않는다 — 전역 상태라 다른 코드가 끼어들면
    /// 결과가 달라진다.
    /// </summary>
    public struct DeterministicRandom
    {
        private uint state;

        public DeterministicRandom(int seed)
        {
            // xorshift는 상태가 0이면 계속 0이다
            state = seed == 0 ? 0x9E3779B9u : unchecked((uint)seed);
        }

        public uint NextUInt()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        /// <summary>0 이상 <paramref name="maxExclusive"/> 미만.</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                return 0;
            }

            return (int)(NextUInt() % (uint)maxExclusive);
        }

        /// <summary><paramref name="minInclusive"/> 이상 <paramref name="maxExclusive"/> 미만.</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            return minInclusive + Next(maxExclusive - minInclusive);
        }

        /// <summary>확률 <paramref name="probability"/>(0~1)로 true.</summary>
        public bool Chance(float probability)
        {
            if (probability <= 0f)
            {
                return false;
            }

            if (probability >= 1f)
            {
                return true;
            }

            // 24비트만 써도 확률 판정에는 충분하고, float 정밀도 안에 들어온다
            return (NextUInt() & 0xFFFFFF) < probability * 0x1000000;
        }

        /// <summary>제자리에서 섞는다 (Fisher-Yates).</summary>
        public void Shuffle<T>(T[] items)
        {
            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }
}
