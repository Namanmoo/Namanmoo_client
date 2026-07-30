using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 단색 사각형을 스프라이트로 그린다.
    ///
    /// 방을 메시(MeshRenderer)와 폴리라인(LineRenderer)으로 그렸을 때, 에디터에서
    /// 만든 머티리얼 에셋·셰이더·경계·카메라가 전부 정상인데도 WebGL 빌드에서
    /// 화면에 아무것도 나오지 않았다. 씬에 저장된 오브젝트는 보이고 실행 중에 만든
    /// 것만 안 보였다. 2D 프로젝트에서 단색 사각형은 스프라이트로 그리는 편이
    /// 자연스럽기도 해서, 방 전체를 이쪽으로 옮겼다.
    ///
    /// 스프라이트 하나(1x1 흰 점)를 모두가 나눠 쓰고 색은 <see cref="SpriteRenderer.color"/>로
    /// 준다 — 방마다 텍스처를 만들면 방을 옮길 때마다 메모리가 늘어난다.
    /// </summary>
    public static class SolidQuad
    {
        private static Sprite shared;

        /// <summary>1x1 흰 스프라이트. 픽셀당 1유닛이라 스케일이 곧 크기가 된다.</summary>
        public static Sprite Shared
        {
            get
            {
                if (shared == null)
                {
                    var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                    {
                        name = "Solid Quad",
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp
                    };
                    texture.SetPixel(0, 0, Color.white);
                    texture.Apply();

                    shared = Sprite.Create(
                        texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                    shared.name = "Solid Quad";
                }

                return shared;
            }
        }

        /// <summary>가운데가 <paramref name="center"/>인 사각형.</summary>
        public static SpriteRenderer Create(
            Transform parent,
            string name,
            Vector2 center,
            Vector2 size,
            Color color,
            int sortingOrder,
            float z = 0f)
        {
            var quad = new GameObject(name);
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = new Vector3(center.x, center.y, z);
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = quad.AddComponent<SpriteRenderer>();
            renderer.sprite = Shared;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        /// <summary>
        /// 두 점을 잇는 굵기 <paramref name="thickness"/>의 막대. 벽의 흔들린 구간을
        /// 하나씩 이렇게 그려 손그림 느낌을 유지한다.
        /// </summary>
        public static SpriteRenderer CreateSegment(
            Transform parent,
            string name,
            Vector2 from,
            Vector2 to,
            float thickness,
            Color color,
            int sortingOrder,
            float z = 0f)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;

            // 길이가 0이면 스케일 0이라 아무것도 안 보인다. 최소한 굵기만큼은 준다.
            if (length < 0.0001f)
            {
                length = thickness;
            }

            SpriteRenderer renderer = Create(
                parent, name, (from + to) * 0.5f,
                new Vector2(length, thickness), color, sortingOrder, z);

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            return renderer;
        }
    }
}
