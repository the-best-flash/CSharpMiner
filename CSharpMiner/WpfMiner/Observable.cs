using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMiner
{
    public class Observable<T> : IObservable<T>, INotifyPropertyChanged
    {
        private List<IObserver<T>> observers;
        private T _value;

        public event EventHandler ValueChanged;

        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                foreach (var observer in observers)
                {
                    observer.OnNext(value);
                }

                if (this.ValueChanged != null)
                    ValueChanged(this, EventArgs.Empty);

                OnPropertyChanged("Value");
            }
        }

        public Observable(T value)
        {
            observers = new List<IObserver<T>>();
            _value = value;
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<T>> _observers;
            private IObserver<T> _observer;

            public Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            // Check whether observer is already registered. If not, add it 
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
                // Provide observer with existing data. 
                observer.OnNext(_value);
            }
            return new Unsubscriber(observers, observer);
        }

        public override int GetHashCode()
        {
            if (this._value != null)
            {
                return this._value.GetHashCode();
            }
            else
            {
                return 0;
            }
        }

        public override bool Equals(object obj)
        {
            if (_value != null && obj != null)
            {
                if (obj is Observable<T>)
                {
                    return _value.Equals(((Observable<T>)obj).Value);
                }
                else
                {
                    return _value.Equals(obj);
                }
            }
            else if(obj == null && _value == null)
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public void Done()
        {
            foreach (var observer in observers)
                observer.OnCompleted();

            observers.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
