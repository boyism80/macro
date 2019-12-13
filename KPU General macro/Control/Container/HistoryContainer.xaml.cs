﻿using System;
using System.Windows;
using System.Windows.Controls;

namespace KPU_General_macro
{
    /// <summary>
    /// Interaction logic for HistoryContainer.xaml
    /// </summary>
    public partial class HistoryContainer : ItemsSourceControl
    {
        public HistoryContainer()
        {
            InitializeComponent();
        }

        public override Panel GetContainer()
        {
            return this.Container;
        }

        public override FrameworkElement OnCreate(object context)
        {
            var control = new HistoryControl()
            {
                DataContext = context,
            };

            return control;
        }

        public override void OnFinedDestroyedItem(FrameworkElement control, object context)
        {
            base.OnFinedDestroyedItem(control, context);

            var disposable = control as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }
    }
}
