using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace macro.Control
{
    public abstract class ItemsSourceControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(ItemsSourceControl), new PropertyMetadata(new PropertyChangedCallback(OnItemsSourcePropertyChanged)));

        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };

        public Mutex ContainerLock { get; private set; } = new Mutex();

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public ItemsSourceControl()
        {
            DataContextChanged += ItemsSourceControl_DataContextChanged;
        }

        private void ItemsSourceControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        public void OnPropertyChanged(string name)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private static void OnItemsSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as ItemsSourceControl;
            if (control == null)
                return;

            control.OnItemsSourceChanged((IEnumerable)e.OldValue, (IEnumerable)e.NewValue);
        }

        private void OnItemsSourceChanged(IEnumerable OldItems, IEnumerable NewItems)
        {
            // Remove handler for oldValue.CollectionChanged
            var oldValueINotifyCollectionChanged = OldItems as INotifyCollectionChanged;

            if (null != oldValueINotifyCollectionChanged)
            {
                oldValueINotifyCollectionChanged.CollectionChanged -= new NotifyCollectionChangedEventHandler(NewValueINotifyCollectionChanged_CollectionChanged);
                NewValueINotifyCollectionChanged_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<object>(OldItems as IEnumerable<object>)));
            }
            // Add handler for newValue.CollectionChanged (if possible)
            var newValueINotifyCollectionChanged = NewItems as INotifyCollectionChanged;
            if (null != newValueINotifyCollectionChanged)
            {
                newValueINotifyCollectionChanged.CollectionChanged += new NotifyCollectionChangedEventHandler(NewValueINotifyCollectionChanged_CollectionChanged);
                NewValueINotifyCollectionChanged_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<object>(NewItems as IEnumerable<object>)));
            }
        }

        public abstract Panel GetContainer();

        public virtual Panel GetRemoveContainer(object context) { return GetContainer(); }

        public abstract FrameworkElement OnCreate(object context);

        public virtual void OnFinedDestroyedItem(FrameworkElement control, object context)
        {
            GetRemoveContainer(context).Children.Remove(control);
        }

        public virtual void OnFindingdDestroyedItem(object context) { }

        public virtual void OnFinish() { }

        public void NewValueINotifyCollectionChanged_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ContainerLock.WaitOne();

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Replace:
                        {
                            for (var i = 0; i < e.NewItems.Count; i++)
                            {
                                var container = GetRemoveContainer(e.OldItems[i]);
                                var control = container.Children.OfType<FrameworkElement>().First(x => x.DataContext == e.OldItems[i]);
                                var index = container.Children.IndexOf(control);
                                control.DataContext = e.NewItems[i];
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Add:
                        {
                            ContainerLock.WaitOne();
                            foreach (var context in e.NewItems)
                            {
                                var created = OnCreate(context);
                                if (created != null)
                                {
                                    created.DataContext = context;
                                    GetContainer().Children.Add(created);
                                }
                            }
                        }
                        OnFinish();
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        {
                            var removed = new List<FrameworkElement>();
                            foreach (var context in e.OldItems)
                            {
                                OnFindingdDestroyedItem(context);

                                var container = GetRemoveContainer(context);
                                var control = container.Children.OfType<FrameworkElement>().First(x => x.DataContext == context);
                                removed.Add(control);
                            }

                            foreach (var x in removed)
                                OnFinedDestroyedItem(x, x.DataContext);

                            removed.Clear();
                        }
                        OnFinish();
                        break;

                    default:
                        Console.WriteLine(e.Action);
                        break;
                }

                ContainerLock.ReleaseMutex();
            }));
        }
    }
}
