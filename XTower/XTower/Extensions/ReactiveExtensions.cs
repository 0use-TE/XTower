using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace XTower.Extensions
{
    internal static class ReactiveExtensions
    {
        public static IObservable<EventPattern<PropertyChangedEventArgs>> ObserveProperty<TObj>(
            this TObj source,
            params string[] propertyNames)
            where TObj : INotifyPropertyChanged =>
            Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    h => source.PropertyChanged += h,
                    h => source.PropertyChanged -= h)
                .Where(x => propertyNames.Contains(x.EventArgs.PropertyName));
    }
}
