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

    /// <summary>CLI: -executeMethod DungeonLayoutPreview.PrintFromCommandLine</summary>
    public static void PrintFromCommandLine()
    {
        Print(floor: 1);
        Print(floor: 4);
        EditorApplication.Exit(0);
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
