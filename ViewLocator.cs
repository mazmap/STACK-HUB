using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using STACK_HUB.Models;
using STACK_HUB.Services;
using STACK_HUB.ViewModels;
using STACK_HUB.Views;

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

        if (type == null && name.EndsWith("View"))
        {
            var withoutViewSuffix = name.Substring(0, name.Length - 4);
            type = Type.GetType(withoutViewSuffix);
        }

        if (type != null)
        {
            Control view;
            if (type == typeof(CasTextEditor))
            {
                view = EditorPool<CasTextEditor>.GetOrCreate();
            }
            else if (type == typeof(MaximaEditor))
            {
                view = EditorPool<MaximaEditor>.GetOrCreate();
            }
            else if (type == typeof(PrtEditorView))
            {
                view = EditorPool<PrtEditorView>.GetOrCreate();
            }
            else
            {
                view = (Control)Activator.CreateInstance(type)!;
            }
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
