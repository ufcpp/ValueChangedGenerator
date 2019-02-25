using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConsoleApplication1
{
    internal struct NotifyRecord
    {
        public int X;
        public int Y;
    }

    partial class Sample<T> : INotifyPropertyChanged
    {
        private struct NotifyRecord
        {
            public int X;

            public int Y;

            public int Z => X * Y;

            /// <summary>
            /// test
            /// </summary>
            public int W;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        protected void SetProperty<T>(ref T storage, T value, PropertyChangedEventArgs args)
        {
            if (!EqualityComparer<T>.Default.Equals(storage, value))
            {
                storage = value;
                OnPropertyChanged(args);
            }
        }

        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null) => SetProperty(ref storage, value, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}
