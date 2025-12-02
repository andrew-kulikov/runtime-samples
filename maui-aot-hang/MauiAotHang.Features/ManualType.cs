using System.Runtime.CompilerServices;

namespace MauiAotHang.Features
{
    public class Disposable : IDisposable
    {
        private readonly Action _onDispose;

        public Disposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke();
        }
    }

    public class ManualType1
    {
        private int _num;

        public ManualType1(int intial)
        {
            if (intial < 0)
            {
                throw new ArgumentException("invalid");
            }
            using var _ = new Disposable(() => Console.WriteLine("Initialized"));
            _num = intial;
        }

        public int Num
        {
            get => _num;
            set
            {
                if (value > 0)
                {
                    _num = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Do()
        {
            using var _ = new Disposable(() =>
            {
                Num++;
                Console.WriteLine("Do");
            });
            Console.WriteLine(_num);
        }
    }

    public class ManualType2
    {
        private int _num;

        public ManualType2(int intial)
        {
            if (intial < 0)
            {
                throw new ArgumentException("invalid");
            }
            using var _ = new Disposable(() => Console.WriteLine("Initialized"));
            _num = intial;
        }

        public int Num
        {
            get => _num;
            set
            {
                if (value > 0)
                {
                    _num = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Do()
        {
            using var _ = new Disposable(() =>
            {
                Num++;
                Console.WriteLine("Do");
            });
            Console.WriteLine(_num);
        }
    }
}
