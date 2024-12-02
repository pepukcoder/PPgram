using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using PPgram.MVVM.ViewModels;

namespace PPgram;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null) return null;
        string name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            if (name.Contains("Chat"))
                name = data.GetType().FullName!.Replace("ViewModel", "MobileView", StringComparison.Ordinal);
        }
        Type? type = Type.GetType(name);
        if (type != null) return (Control)Activator.CreateInstance(type)!;
        return new TextBlock { Text = "Not Found: " + name };
    }
    public bool Match(object? data) => data is ViewModelBase;
}