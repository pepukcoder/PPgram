using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using PPgram.MVVM.ViewModels;

namespace PPgram;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        
        if(OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            if (name.EndsWith("ChatView", StringComparison.Ordinal))
            {
                name = name.Replace("ChatView", "Mobile_ChatView", StringComparison.Ordinal);
            }
        }

        var type = Type.GetType(name);
        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}