using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConsoleApplication1
{
    partial class Sample<T>
    {
        private NotifyRecord _value;

        public int X { get { return _value.X; } set { SetProperty(ref _value.X, value, XProperty); OnPropertyChanged(ZProperty); } }
        private static readonly PropertyChangedEventArgs XProperty = new PropertyChangedEventArgs(nameof(X));

        public int Y { get { return _value.Y; } set { SetProperty(ref _value.Y, value, YProperty); OnPropertyChanged(ZProperty); } }
        private static readonly PropertyChangedEventArgs YProperty = new PropertyChangedEventArgs(nameof(Y));

        /// <summary>
        /// test
        /// </summary>
        public int W { get { return _value.W; } set { SetProperty(ref _value.W, value, WProperty); } }
        private static readonly PropertyChangedEventArgs WProperty = new PropertyChangedEventArgs(nameof(W));

        public int Z => _value.Z;
        private static readonly PropertyChangedEventArgs ZProperty = new PropertyChangedEventArgs(nameof(Z));
    }
}
