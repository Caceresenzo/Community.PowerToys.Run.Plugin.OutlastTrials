#nullable enable

using System;
using System.Collections.Generic;

namespace Community.PowerToys.Run.Plugin.OutlastTrials;

public class PumpsPuzzleSolver
{
    private static readonly PumpState[] PossibleStates = Enum.GetValues<PumpState>();

    public static Actions? Solve(int beginning, int first, int second, int third, int final)
    {
        List<Actions> solutions = [];

        foreach (var firstAction in PossibleStates)
        {
            var valueWithFirst = beginning + (int)firstAction * first;
            foreach (var secondAction in PossibleStates)
            {
                var valueWithSecond = valueWithFirst + (int)secondAction * second;
                foreach (var thirdAction in PossibleStates)
                {
                    var valueWithThird = valueWithSecond + (int)thirdAction * third;
                    if (valueWithThird == final)
                        solutions.Add(new Actions(firstAction, secondAction, thirdAction));
                }
            }
        }

        if (solutions.Count == 0)
            return null;

        solutions.Sort((a, b) => a.ActionCount.CompareTo(b.ActionCount));
        return solutions[0];
    }

    public record Actions(PumpState First, PumpState Second, PumpState Third)
    {
        public int ActionCount =>
            (First != PumpState.Ignore ? 1 : 0)
            + (Second != PumpState.Ignore ? 1 : 0)
            + (Third != PumpState.Ignore ? 1 : 0);
    }

    public enum PumpState
    {
        Minus = -1,
        Ignore = 0,
        Plus = 1,
    }

    public static string ToText(PumpState state) =>
        state switch
        {
            PumpState.Minus => "-",
            PumpState.Ignore => "(skip)",
            PumpState.Plus => "+",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };
}
