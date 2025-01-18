using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Fontisso.NET.ViewModels;

namespace Fontisso.NET;

public class ViewLocator : IDataTemplate
{
    private static Dictionary<Type, Func<Control>> Registration = new();

    public static void Register<TViewModel, TView>() where TView : Control, new()
    {
        Registration.Add(typeof(TViewModel), () => new TView());
    }

    public static void Register<TViewModel, TView>(Func<TView> factory) where TView : Control
    {
        Registration.Add(typeof(TViewModel), factory);
    }

    public Control Build(object? data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var type = data.GetType();

        if (Registration.TryGetValue(type, out var factory))
        {
            return factory();
        }

        return new TextBlock { Text = "Not Found: " + type };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}