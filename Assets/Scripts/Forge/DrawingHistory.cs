using System;
using System.Collections.Generic;

/// <summary>
/// 그리기 undo/redo 스냅샷 스택. 픽셀 배열을 통째로 보관하는 가장 단순한 방식이다.
/// (512x512 RGBA = 1MB. 20단계면 20MB — 한 화면짜리 도구엔 충분히 싸다.)
///
/// Unity에 의존하지 않는 순수 로직이라 EditMode 테스트로 전부 덮을 수 있다.
/// </summary>
public sealed class DrawingHistory
{
    public const int DefaultCapacity = 20;

    private readonly List<Color32Snapshot> undoStack = new();
    private readonly List<Color32Snapshot> redoStack = new();
    private readonly int capacity;

    public DrawingHistory(int capacity = DefaultCapacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                "히스토리 용량은 1 이상이어야 합니다.");
        }

        this.capacity = capacity;
    }

    public int UndoCount => undoStack.Count;
    public int RedoCount => redoStack.Count;
    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;

    /// <summary>획을 긋기 <em>직전</em> 상태를 밀어 넣는다.</summary>
    public void Push(byte[] pixels)
    {
        if (pixels == null)
        {
            throw new ArgumentNullException(nameof(pixels));
        }

        undoStack.Add(new Color32Snapshot(pixels));
        // 새 획이 그어지면 redo 가지는 버려진다 — 일반적인 편집기 동작
        redoStack.Clear();

        while (undoStack.Count > capacity)
        {
            undoStack.RemoveAt(0);
        }
    }

    /// <summary>
    /// 한 단계 되돌린다. <paramref name="current"/>는 되돌리기 직전의 현재 픽셀로,
    /// redo 스택에 쌓인다. 되돌릴 게 없으면 null을 반환한다.
    /// </summary>
    public byte[] Undo(byte[] current)
    {
        if (current == null)
        {
            throw new ArgumentNullException(nameof(current));
        }

        if (undoStack.Count == 0)
        {
            return null;
        }

        int last = undoStack.Count - 1;
        Color32Snapshot snapshot = undoStack[last];
        undoStack.RemoveAt(last);
        redoStack.Add(new Color32Snapshot(current));
        return snapshot.ToArray();
    }

    /// <summary>다시 실행. 되돌린 게 없으면 null을 반환한다.</summary>
    public byte[] Redo(byte[] current)
    {
        if (current == null)
        {
            throw new ArgumentNullException(nameof(current));
        }

        if (redoStack.Count == 0)
        {
            return null;
        }

        int last = redoStack.Count - 1;
        Color32Snapshot snapshot = redoStack[last];
        redoStack.RemoveAt(last);
        undoStack.Add(new Color32Snapshot(current));
        return snapshot.ToArray();
    }

    public void Clear()
    {
        undoStack.Clear();
        redoStack.Clear();
    }

    /// <summary>바깥에서 넘어온 배열을 그대로 붙들지 않도록 복사해서 보관한다.</summary>
    private readonly struct Color32Snapshot
    {
        private readonly byte[] pixels;

        public Color32Snapshot(byte[] source)
        {
            pixels = new byte[source.Length];
            Buffer.BlockCopy(source, 0, pixels, 0, source.Length);
        }

        public byte[] ToArray()
        {
            var copy = new byte[pixels.Length];
            Buffer.BlockCopy(pixels, 0, copy, 0, pixels.Length);
            return copy;
        }
    }
}
