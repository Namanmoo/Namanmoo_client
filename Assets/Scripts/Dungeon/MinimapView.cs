using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 미니맵을 화면 우상단에 그린다. 무엇을 그릴지는 <see cref="MinimapModel"/>이 정하고
    /// 여기서는 칸을 실제 Image로 옮기는 일만 한다.
    ///
    /// 칸은 다시 쓴다 — 방을 옮길 때마다 만들고 지우면 이동이 잦은 게임에서 GC가 튄다.
    /// </summary>
    public sealed class MinimapView : MonoBehaviour
    {
        /// <summary>칸 하나의 크기(픽셀).</summary>
        public const float CellSize = 14f;

        /// <summary>칸 간격. 크기보다 커야 칸 사이에 틈이 보인다.</summary>
        public const float Step = 18f;

        private static readonly Color CurrentColor = new Color(1f, 0.95f, 0.35f, 1f);
        private static readonly Color VisitedColor = new Color(0.85f, 0.85f, 0.88f, 0.95f);
        private static readonly Color UnknownColor = new Color(0.42f, 0.42f, 0.48f, 0.75f);
        private static readonly Color BossColor = new Color(0.90f, 0.30f, 0.28f, 1f);
        private static readonly Color TreasureColor = new Color(0.95f, 0.80f, 0.35f, 1f);
        private static readonly Color ShopColor = new Color(0.45f, 0.75f, 0.95f, 1f);

        private readonly List<Image> pool = new List<Image>();

        // 씬 빌더가 넣고 씬에 저장된다. [SerializeField]가 없으면 저장 뒤 실행할 때
        // 전부 null이 되어 미니맵이 조용히 사라진다.
        [SerializeField] private DungeonRunner runner;
        [SerializeField] private RectTransform holder;

        /// <summary>
        /// 붙일 런너와 칸을 담을 RectTransform. 씬 빌더가 부른다.
        /// </summary>
        public void Configure(DungeonRunner dungeonRunner, RectTransform cellHolder)
        {
            runner = dungeonRunner;
            holder = cellHolder;
        }

        private void OnEnable()
        {
            if (runner == null)
            {
                return;
            }

            runner.RoomChanged += OnRoomChanged;

            // Start()가 먼저 돌아 첫 RoomChanged를 놓쳤을 수 있다. 지금 상태를 물어본다.
            if (runner.Layout != null)
            {
                Redraw();
            }
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.RoomChanged -= OnRoomChanged;
            }
        }

        private void OnRoomChanged(Vector2Int cell)
        {
            Redraw();
        }

        private void Redraw()
        {
            if (runner == null || holder == null || runner.Layout == null)
            {
                return;
            }

            List<MinimapCell> cells = MinimapModel.Build(
                runner.Layout, runner.CurrentCell, runner.ClearedRooms);

            EnsurePool(cells.Count);

            for (int i = 0; i < cells.Count; i++)
            {
                MinimapCell cell = cells[i];
                Image image = pool[i];
                image.enabled = true;
                image.color = ColorFor(cell);

                var rect = (RectTransform)image.transform;
                rect.anchoredPosition = MinimapModel.PositionOf(
                    cell.Cell, runner.CurrentCell, Step);
                rect.sizeDelta = new Vector2(CellSize, CellSize);
            }

            for (int i = cells.Count; i < pool.Count; i++)
            {
                pool[i].enabled = false;
            }
        }

        private static Color ColorFor(MinimapCell cell)
        {
            if (cell.Current)
            {
                return CurrentColor;
            }

            if (!cell.Visited)
            {
                return UnknownColor;
            }

            return cell.Kind switch
            {
                RoomKind.Boss => BossColor,
                RoomKind.Treasure => TreasureColor,
                RoomKind.Shop => ShopColor,
                _ => VisitedColor
            };
        }

        private void EnsurePool(int wanted)
        {
            while (pool.Count < wanted)
            {
                var cellObject = new GameObject($"Minimap Cell {pool.Count}");
                cellObject.transform.SetParent(holder, false);

                var rect = cellObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                Image image = cellObject.AddComponent<Image>();
                image.raycastTarget = false;
                pool.Add(image);
            }
        }
    }
}
