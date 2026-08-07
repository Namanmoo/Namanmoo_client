using System.Collections.Generic;

namespace NaManMoo.Audio
{
    /// <summary>
    /// 효과음 파일명 후보를 폴백 순서대로 만든다. 순서와 어휘의 근거는
    /// <c>Assets/Audio/Weapon/NAMING.md</c>·<c>Assets/Audio/Impact/NAMING.md</c>다 —
    /// 여기 로직을 바꾸면 그 문서도 같이 고친다.
    ///
    /// 이름만 만들 뿐 파일이 있는지는 모른다. 실제 재생은 <see cref="SfxPlayer"/>가
    /// 후보를 위에서부터 훑으며 처음 있는 것을 튼다.
    /// </summary>
    public static class SfxNames
    {
        /// <summary>
        /// 휘두르는 소리는 화면에서 벌어지는 동작을 따른다 — 그림이 쇠몽둥이여도
        /// 타입이 투척으로 뽑혔으면 소리도 던지는 소리여야 한다.
        /// </summary>
        public static string MotionOf(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Sword:
                case WeaponType.Axe:
                    return "swing";
                case WeaponType.Spear:
                    return "thrust";
                case WeaponType.Projectile:
                case WeaponType.Boomerang:
                    return "throw";
                case WeaponType.Missile:
                    return "shoot";
                default:
                    return "swing";
            }
        }

        /// <summary>
        /// 무게값 = damage × attackInterval. 묵직함은 초당 화력이 아니라 한 방의
        /// 크기에서 온다. 경계값은 NAMING.md의 표와 같다.
        /// </summary>
        public static string WeightOf(int damage, float attackInterval)
        {
            float heft = damage * attackInterval;
            if (heft < 3f)
            {
                return "light";
            }

            return heft < 10f ? "medium" : "heavy";
        }

        public static string MaterialNameOf(WeaponMaterial material)
        {
            return material.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// 휘두르는 소리 폴백 — 재질을 무게보다 오래 붙든다. 마지막은 default.
        /// </summary>
        public static List<string> AttackCandidates(
            string motion, string weight, string material)
        {
            return new List<string>
            {
                $"{motion}_{weight}_{material}",
                $"{motion}_any_{material}",
                $"any_{weight}_{material}",
                $"any_any_{material}",
                $"{motion}_{weight}_any",
                $"{motion}_any_any",
                "default"
            };
        }

        /// <summary>
        /// 타격음 폴백 — 대상 재질이 소리를 지배하므로 대상을 가장 오래 붙들고
        /// 마지막에 버린다. 마지막은 default.
        /// </summary>
        public static List<string> ImpactCandidates(
            string weaponMaterial, string weight, string targetMaterial)
        {
            return new List<string>
            {
                $"hit_{weaponMaterial}_{weight}_{targetMaterial}",
                $"hit_{weaponMaterial}_any_{targetMaterial}",
                $"hit_any_{weight}_{targetMaterial}",
                $"hit_any_any_{targetMaterial}",
                $"hit_{weaponMaterial}_{weight}_any",
                $"hit_{weaponMaterial}_any_any",
                $"hit_any_{weight}_any",
                "hit_any_any_any",
                "default"
            };
        }

        /// <summary>
        /// 상태이상 소리 폴백 — 출처와 효과만 폴백하고 단계는 폴백하지 않는다.
        /// default도 없다. 없는 단계는 무음이 맞는 동작이다(Effect/NAMING.md).
        /// </summary>
        public static List<string> EffectCandidates(
            string source, string effect, string stage)
        {
            var candidates = new List<string> { $"{source}_{effect}_{stage}" };
            if (source != "any")
            {
                candidates.Add($"any_{effect}_{stage}");
            }

            return candidates;
        }

        /// <summary>적이 스스로 내는 죽는 소리. 재질별 파일이 없으면 무음.</summary>
        public static List<string> DieCandidates(string targetMaterial)
        {
            return new List<string> { $"die_{targetMaterial}" };
        }
    }
}
