namespace PadScope.Core.Input;

public sealed class MacroProcessor
{
    private readonly IReadOnlyList<MacroDefinition> _definitions;
    private readonly IReadOnlyList<MacroSequence> _sequences;

    private Ds4Buttons _previousInput;
    private MacroDefinition? _toggleMacro;
    private double _rapidTime;
    private MacroSequence? _activeSequence;
    private int _sequenceStep;
    private double _sequenceTime;

    public MacroProcessor(
        IReadOnlyList<MacroDefinition> definitions,
        IReadOnlyList<MacroSequence> sequences)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _sequences = sequences ?? throw new ArgumentNullException(nameof(sequences));
    }

    public Ds4Buttons Process(Ds4Buttons input, TimeSpan elapsed)
    {
        double dt = Math.Max(elapsed.TotalSeconds, 0);

        Ds4Buttons owned = Ds4Buttons.None;
        Ds4Buttons output = Ds4Buttons.None;

        if (_activeSequence is not null)
        {
            AdvanceSequence(dt, ref output);
            owned |= _activeSequence?.Trigger ?? Ds4Buttons.None;
        }
        else
        {
            MacroSequence? start = _sequences.FirstOrDefault(sequence =>
                sequence.Trigger != Ds4Buttons.None &&
                sequence.Steps.Count > 0 &&
                (input & sequence.Trigger) == sequence.Trigger &&
                (_previousInput & sequence.Trigger) != sequence.Trigger);

            if (start is not null)
            {
                _activeSequence = start;
                _sequenceStep = 0;
                _sequenceTime = 0;
                owned |= start.Trigger;
                output |= start.Steps[0].Buttons;
            }
        }

        if (_activeSequence is null)
        {
            MacroDefinition? held = _definitions
                .Where(definition =>
                    definition.Trigger != Ds4Buttons.None &&
                    (input & definition.Trigger) == definition.Trigger)
                .OrderByDescending(definition => CountBits(definition.Trigger))
                .FirstOrDefault();

            bool pressedNow = held is not null &&
                              (_previousInput & held.Trigger) != held.Trigger;

            if (pressedNow && held!.IsToggle)
            {
                _toggleMacro = ReferenceEquals(_toggleMacro, held) ? null : held;
                _rapidTime = 0;
            }

            MacroDefinition? active = _toggleMacro ?? (held is { IsToggle: false } ? held : null);

            if (active is not null)
            {
                owned |= active.Trigger;

                if (active.ShotsPerSecond > 0)
                {
                    _rapidTime += dt;
                    double period = 1.0 / active.ShotsPerSecond;

                    if (_rapidTime % period < period * 0.5)
                    {
                        output |= active.Output;
                    }
                }
                else
                {
                    output |= active.Output;
                }
            }
            else
            {
                _rapidTime = 0;
            }
        }

        _previousInput = input;
        return (Ds4Buttons)(((int)input & ~(int)owned) | (int)output);
    }

    private void AdvanceSequence(double dt, ref Ds4Buttons output)
    {
        if (_activeSequence is null || _activeSequence.Steps.Count == 0)
        {
            _activeSequence = null;
            return;
        }

        _sequenceTime += dt;

        while (_sequenceTime >= _activeSequence.Steps[_sequenceStep].DurationSeconds)
        {
            double duration = Math.Max(_activeSequence.Steps[_sequenceStep].DurationSeconds, 0.001);
            _sequenceTime -= duration;
            _sequenceStep++;

            if (_sequenceStep >= _activeSequence.Steps.Count)
            {
                _activeSequence = null;
                _sequenceStep = 0;
                _sequenceTime = 0;
                return;
            }
        }

        output |= _activeSequence.Steps[_sequenceStep].Buttons;
    }

    private static int CountBits(Ds4Buttons buttons)
    {
        int value = (int)buttons;
        int count = 0;

        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }

        return count;
    }
}