using System.Text;
using NaManMoo.Dungeon;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 생성된 층을 글자 그림으로 찍어 본다. 확률 기반이라 숫자만 보고는 모양을 알 수 없어,
/// 방 개수·확률을 조정할 때 이걸로 실제 결과를 확인한다.
///
/// S=시작 B=보스 T=보물 P=상점 ·=일반
/// </summary>
public static class DungeonLayoutPreview
{
    private const int Samples = 6;

    [MenuItem("Tools/NaManMoo/Print Dungeon Layouts")]
    public static void PrintDefault()
    {
        Print(floor: 1);
    }

    [MenuItem("Tools/NaManMoo/Print Dungeon Layouts (Floor 4)")]
    public static void PrintDeeper()
    {
        Print(floor: 4);
    }

    [MenuItem("Tools/NaManMoo/Print Room Shapes")]
    public static void PrintRooms()
    {
        PrintRoom(Doors.North | Doors.South | Doors.East | Doors.West, seed: 1);
        PrintRoom(Doors.North | Doors.West, seed: 2);
        PrintRoom(Doors.East, seed: 3);
    }

    /// <summary>CLI: -executeMethod DungeonLayoutPreview.PrintFromCommandLine</summary>
    public static void PrintFromCommandLine()
    {
        Print(floor: 1);
        Print(floor: 4);
        PrintRooms();
        EditorApplication.Exit(0);
    }

    /// <summary>
    /// 방 하나를 글자 그림으로. #=벽 D=문 ·=바닥
    /// 문 옆이 평평한지, 흔들린 벽이 문을 막지 않는지 눈으로 본다.
    /// </summary>
    private static void PrintRoom(Doors doors, int seed)
    {
        RoomShape shape = RoomShape.Build(seed, doors);
        Rect b = shape.Bounds;
        int w = Mathf.RoundToInt(b.width);
        int h = Mathf.RoundToInt(b.height);
        var grid = new char[w + 1, h + 1];

        for (int y = 0; y <= h; y++)
        {
            for (int x = 0; x <= w; x++)
            {
                grid[x, y] = ' ';
            }
        }

        // 벽 폴리라인을 점으로 찍는다 (선분 사이를 잘게 나눠 끊기지 않게)
        foreach (var wall in shape.Walls)
        {
            for (int i = 0; i < wall.Count - 1; i++)
            {
                Vector2 from = wall[i];
                Vector2 to = wall[i + 1];
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) * 2f));
                for (int s = 0; s <= steps; s++)
                {
                    Vector2 p = Vector2.Lerp(from, to, s / (float)steps);
                    Mark(grid, b, w, h, p, '#');
                }
            }
        }

        foreach (DoorOpening door in shape.DoorOpenings)
        {
            int steps = Mathf.CeilToInt(RoomShape.DoorWidth * 2f);
            for (int s = 0; s <= steps; s++)
            {
                Mark(grid, b, w, h, Vector2.Lerp(door.From, door.To, s / (float)steps), 'D');
            }
        }

        var art = new StringBuilder();
        art.AppendLine($"── 방: {doors} (시드 {seed}) — 벽 구간 {shape.Walls.Count}개 ──");
        for (int y = h; y >= 0; y--)
        {
            for (int x = 0; x <= w; x++)
            {
                art.Append(grid[x, y]);
            }

            art.AppendLine();
        }

        Debug.Log(art.ToString());
    }

    private static void Mark(char[,] grid, Rect b, int w, int h, Vector2 p, char glyph)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt((p.x - b.xMin) / b.width * w), 0, w);
        int y = Mathf.Clamp(Mathf.RoundToInt((p.y - b.yMin) / b.height * h), 0, h);
        grid[x, y] = glyph;
    }

    private static void Print(int floor)
    {
        var report = new StringBuilder();
        report.AppendLine($"── {floor}층 표본 {Samples}개 ──");

        for (int seed = 1; seed <= Samples; seed++)
        {
            DungeonLayout layout = DungeonLayout.Generate(seed, floor);
            report.AppendLine();
            report.AppendLine($"시드 {seed} — 방 {layout.Rooms.Count}개, "
                + $"보스까지 {layout.RoomOfKind(RoomKind.Boss)?.DistanceFromStart ?? 0}칸");
            report.AppendLine(Render(layout));
        }

        Debug.Log(report.ToString());
    }

    private static string Render(DungeonLayout layout)
    {
        var art = new StringBuilder();

        // 위쪽이 y가 큰 쪽 — 화면에서 보이는 방향과 맞춘다
        for (int y = layout.Grid.y - 1; y >= 0; y--)
        {
            for (int x = 0; x < layout.Grid.x; x++)
            {
                DungeonRoom room = layout.RoomAt(new Vector2Int(x, y));
                art.Append(room == null ? "  " : Glyph(room.Kind) + " ");
            }

            art.AppendLine();
        }

        return art.ToString();
    }

    private static string Glyph(RoomKind kind)
    {
        return kind switch
        {
            RoomKind.Start => "S",
            RoomKind.Boss => "B",
            RoomKind.Treasure => "T",
            RoomKind.Shop => "P",
            _ => "·"
        };
    }
}
