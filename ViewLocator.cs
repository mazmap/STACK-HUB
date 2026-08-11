using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using STACK_HUB.Models;
using STACK_HUB.ViewModels;

namespace STACK_HUB;

public class ViewLocator : IDataTemplate
{
    private readonly ConditionalWeakTable<object, Control> _viewCache = new();

    public Control? Build(object? param)
    {
        if (param is null) return null;

        if (param is ICacheablePane)
        {
            if (_viewCache.TryGetValue(param, out var cachedView))
            {
                // 🛑 FIX: Detach cachedView from its previous visual/logical parent 
                // before giving it to the new split pane ContentPresenter.
                if (cachedView.Parent is ContentPresenter oldContentPresenter)
                {
                    oldContentPresenter.Content = null;
                }
                else if (cachedView.Parent is ContentControl oldContentControl)
                {
                    oldContentControl.Content = null;
                }
                else if (cachedView.Parent is Panel oldPanel)
                {
                    oldPanel.Children.Remove(cachedView);
                }

                return cachedView;
            }
        }

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        // Fallback: If "CASTextEditorView" isn't found, check for "CASTextEditor"
        if (type == null && name.EndsWith("View"))
        {
            var withoutViewSuffix = name.Substring(0, name.Length - 4);
            type = Type.GetType(withoutViewSuffix);
        }

        if (type != null)
        {
            var view = (Control)Activator.CreateInstance(type)!;

            if (param is ICacheablePane)
            {
                _viewCache.AddOrUpdate(param, view);
            }

            return view;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
