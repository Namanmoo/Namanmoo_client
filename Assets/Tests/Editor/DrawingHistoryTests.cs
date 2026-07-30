using System;
using NUnit.Framework;

public sealed class DrawingHistoryTests
{
    private static byte[] Frame(byte value)
    {
        return new byte[] { value, value, value, 255 };
    }

    [Test]
    public void NewHistory_HasNothingToUndoOrRedo()
    {
        var history = new DrawingHistory();

        Assert.That(history.CanUndo, Is.False);
        Assert.That(history.CanRedo, Is.False);
        Assert.That(history.Undo(Frame(1)), Is.Null);
        Assert.That(history.Redo(Frame(1)), Is.Null);
    }

    [Test]
    public void Undo_ReturnsThePreviousFrameAndRedoReturnsItBack()
    {
        var history = new DrawingHistory();
        history.Push(Frame(1));

        byte[] undone = history.Undo(Frame(2));
        Assert.That(undone, Is.EqualTo(Frame(1)));
        Assert.That(history.CanUndo, Is.False);
        Assert.That(history.CanRedo, Is.True);

        byte[] redone = history.Redo(Frame(1));
        Assert.That(redone, Is.EqualTo(Frame(2)));
        Assert.That(history.CanUndo, Is.True);
        Assert.That(history.CanRedo, Is.False);
    }

    [Test]
    public void MultipleUndos_WalkBackInOrder()
    {
        var history = new DrawingHistory();
        history.Push(Frame(1));
        history.Push(Frame(2));
        history.Push(Frame(3));

        Assert.That(history.Undo(Frame(4)), Is.EqualTo(Frame(3)));
        Assert.That(history.Undo(Frame(3)), Is.EqualTo(Frame(2)));
        Assert.That(history.Undo(Frame(2)), Is.EqualTo(Frame(1)));
        Assert.That(history.Undo(Frame(1)), Is.Null);
    }

    [Test]
    public void PushingAfterUndo_DropsTheRedoBranch()
    {
        var history = new DrawingHistory();
        history.Push(Frame(1));
        history.Undo(Frame(2));
        Assert.That(history.CanRedo, Is.True);

        history.Push(Frame(5));

        Assert.That(history.CanRedo, Is.False);
        Assert.That(history.Redo(Frame(5)), Is.Null);
    }

    [Test]
    public void OldestEntriesAreDroppedBeyondCapacity()
    {
        var history = new DrawingHistory(capacity: 2);
        history.Push(Frame(1));
        history.Push(Frame(2));
        history.Push(Frame(3));

        Assert.That(history.UndoCount, Is.EqualTo(2));
        Assert.That(history.Undo(Frame(4)), Is.EqualTo(Frame(3)));
        Assert.That(history.Undo(Frame(3)), Is.EqualTo(Frame(2)));
        Assert.That(history.Undo(Frame(2)), Is.Null);
    }

    [Test]
    public void StoredFramesAreCopied_SoLaterMutationDoesNotCorruptHistory()
    {
        var history = new DrawingHistory();
        byte[] live = Frame(1);
        history.Push(live);

        // 캔버스는 같은 배열을 계속 덮어쓰며 그린다 — 히스토리가 그걸 붙들면 안 된다
        live[0] = 99;

        Assert.That(history.Undo(Frame(2))[0], Is.EqualTo(1));
    }

    [Test]
    public void Clear_EmptiesBothStacks()
    {
        var history = new DrawingHistory();
        history.Push(Frame(1));
        history.Undo(Frame(2));

        history.Clear();

        Assert.That(history.CanUndo, Is.False);
        Assert.That(history.CanRedo, Is.False);
    }

    [Test]
    public void InvalidArgumentsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingHistory(0));

        var history = new DrawingHistory();
        Assert.Throws<ArgumentNullException>(() => history.Push(null));
        Assert.Throws<ArgumentNullException>(() => history.Undo(null));
        Assert.Throws<ArgumentNullException>(() => history.Redo(null));
    }
}
