using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DeckTracker.Domain
{
    public class NotifyObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        private void Handle(object sender, PropertyChangedEventArgs args)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null) {
                foreach (var t in e.NewItems)
                    ((T)t).PropertyChanged += Handle;
            }
            if (e.OldItems != null) {
                foreach (var t in e.OldItems)
                    ((T)t).PropertyChanged -= Handle;
            }
            base.OnCollectionChanged(e);
        }
    }
}