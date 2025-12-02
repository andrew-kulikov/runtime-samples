
using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GeneratedClasses;

public interface IHasValue
{
    int Value { get; set; }
}

public static class HelperCalculator
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<IEnumerable<T>> Add<T>(T input, int value) where T : struct, IHasValue
    {
        IHasValue input1 = input as IHasValue;
        input1.Value += value;
        await Task.Yield();
        return new List<T> { (T)(object)input1.Value };
    }
}
public record DummyClass1: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass2: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass3: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass4: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass5: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass6: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass7: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass8: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass9: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass10: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass11: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass12: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass13: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass14: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass15: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass16: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass17: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass18: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass19: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass20: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass21: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass22: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass23: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass24: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass25: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass26: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass27: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass28: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass29: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass30: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass31: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass32: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass33: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass34: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass35: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass36: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass37: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass38: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass39: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass40: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass41: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass42: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass43: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass44: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass45: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass46: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass47: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass48: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass49: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass50: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass51: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass52: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass53: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass54: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass55: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass56: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass57: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass58: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass59: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass60: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass61: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass62: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass63: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass64: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass65: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass66: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass67: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass68: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass69: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass70: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass71: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass72: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass73: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass74: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass75: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass76: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass77: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass78: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass79: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass80: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass81: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass82: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass83: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass84: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass85: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass86: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass87: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass88: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass89: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass90: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass91: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass92: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass93: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass94: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass95: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass96: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass97: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass98: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass99: IHasValue
{
    public int Value { get; set; }
}

public record DummyClass100: IHasValue
{
    public int Value { get; set; }
}
