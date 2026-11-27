using Ambystech.Elaris.UI.Input;
using FluentAssertions;

namespace Ambystech.Elaris.UI.Tests.Input;

public class MouseSequenceDecoderTests
{
    /// <summary>
    /// Feeds characters the way Console.ReadKey on Unix returns an unrecognized escape
    /// sequence: one key per character.
    /// </summary>
    private static MouseSequenceDecoder Decoder(Queue<ConsoleKeyInfo> keys)
        => new(() => keys.Count > 0, keys.Dequeue);

    private static ConsoleKeyInfo K(char c) => c switch
    {
        '\x1b' => new ConsoleKeyInfo(c, ConsoleKey.Escape, false, false, false),
        >= 'a' and <= 'z' => new ConsoleKeyInfo(c, (ConsoleKey)(c - 'a' + (int)ConsoleKey.A), false, false, false),
        _ => new ConsoleKeyInfo(c, ConsoleKey.NoName, false, false, false),
    };

    private static Queue<ConsoleKeyInfo> Keys(string text) => new(text.Select(K));

    private static List<TerminalInput> ReadAll(MouseSequenceDecoder decoder)
    {
        var inputs = new List<TerminalInput>();
        while (decoder.TryRead(out var input))
            inputs.Add(input);
        return inputs;
    }

    [Theory]
    [InlineData("0;10;5", 'M', 9, 4, MouseButton.Left, MouseEventKind.Press)]
    [InlineData("0;10;5", 'm', 9, 4, MouseButton.Left, MouseEventKind.Release)]
    [InlineData("1;1;1", 'M', 0, 0, MouseButton.Middle, MouseEventKind.Press)]
    [InlineData("2;3;4", 'M', 2, 3, MouseButton.Right, MouseEventKind.Press)]
    [InlineData("32;7;8", 'M', 6, 7, MouseButton.Left, MouseEventKind.Drag)]
    [InlineData("64;3;4", 'M', 2, 3, MouseButton.None, MouseEventKind.WheelUp)]
    [InlineData("65;3;4", 'M', 2, 3, MouseButton.None, MouseEventKind.WheelDown)]
    public void TryParseSgr_DecodesButtonKindAndZeroBasedPosition(
        string body, char final, int x, int y, MouseButton button, MouseEventKind kind)
    {
        MouseSequenceDecoder.TryParseSgr(body, final, out var e).Should().BeTrue();

        e.Should().Be(new MouseEvent(x, y, button, kind));
    }

    [Fact]
    public void TryParseSgr_DecodesModifiers()
    {
        MouseSequenceDecoder.TryParseSgr("28;1;1", 'M', out var e).Should().BeTrue(); // 4 + 8 + 16

        e.Modifiers.Should().Be(ConsoleModifiers.Shift | ConsoleModifiers.Alt | ConsoleModifiers.Control);
        e.Button.Should().Be(MouseButton.Left);
    }

    [Theory]
    [InlineData("0;10", 'M')]
    [InlineData("0;0;5", 'M')]
    [InlineData("x;1;1", 'M')]
    [InlineData("35;1;1", 'M')]   // motion without a button
    [InlineData("66;1;1", 'M')]   // horizontal wheel
    public void TryParseSgr_RejectsUnsupportedOrMalformed(string body, char final)
    {
        MouseSequenceDecoder.TryParseSgr(body, final, out _).Should().BeFalse();
    }

    [Fact]
    public void TryRead_DecodesMouseSequenceBetweenKeys()
    {
        var decoder = Decoder(Keys("a\x1b[<0;10;5Mb"));

        var inputs = ReadAll(decoder);

        inputs.Should().HaveCount(3);
        inputs[0].Key.KeyChar.Should().Be('a');
        inputs[1].Mouse.Should().Be(new MouseEvent(9, 4, MouseButton.Left, MouseEventKind.Press));
        inputs[2].Key.KeyChar.Should().Be('b');
    }

    [Fact]
    public void TryRead_DecodesConsecutiveSequences()
    {
        var decoder = Decoder(Keys("\x1b[<0;2;2M\x1b[<0;2;2m\x1b[<65;1;1M"));

        ReadAll(decoder).Select(i => i.Mouse!.Value.Kind).Should().Equal(
            MouseEventKind.Press, MouseEventKind.Release, MouseEventKind.WheelDown);
    }

    [Fact]
    public void TryRead_LoneEscape_IsAKey()
    {
        var decoder = Decoder(Keys("\x1b"));

        var inputs = ReadAll(decoder);

        inputs.Should().ContainSingle().Which.Key.Key.Should().Be(ConsoleKey.Escape);
    }

    [Fact]
    public void TryRead_EscapeFollowedByOtherKeys_ReturnsThemUnchangedInOrder()
    {
        var decoder = Decoder(Keys("\x1b[x"));

        ReadAll(decoder).Select(i => i.Key.KeyChar).Should().Equal('\x1b', '[', 'x');
    }

    [Fact]
    public void TryRead_IncompleteSequence_ReturnsItsKeysAfterTimeout()
    {
        var decoder = Decoder(Keys("\x1b[<0;1"));

        var inputs = ReadAll(decoder);

        inputs.Should().OnlyContain(i => !i.IsMouse);
        new string(inputs.Select(i => i.Key.KeyChar).ToArray()).Should().Be("\x1b[<0;1");
    }

    [Fact]
    public void TryRead_MalformedSequence_ReturnsItsKeys()
    {
        var decoder = Decoder(Keys("\x1b[<0;x;1Mz"));

        var text = new string(ReadAll(decoder).Select(i => i.Key.KeyChar).ToArray());

        text.Should().Be("\x1b[<0;x;1Mz");
    }

    [Fact]
    public void TryRead_SequenceArrivingInPieces_IsStillDecoded()
    {
        var keys = new Queue<ConsoleKeyInfo>(Keys("\x1b[<"));

        var later = Task.Run(async () =>
        {
            await Task.Delay(10);
            lock (keys)
                foreach (var k in Keys("0;3;3M"))
                    keys.Enqueue(k);
        });

        var decoderWithLock = new MouseSequenceDecoder(
            () => { lock (keys) return keys.Count > 0; },
            () => { lock (keys) return keys.Dequeue(); });

        decoderWithLock.TryRead(out var input).Should().BeTrue();
        later.Wait();

        input.Mouse.Should().Be(new MouseEvent(2, 2, MouseButton.Left, MouseEventKind.Press));
    }
}
