using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Prism.Ioc;
using UkiChat.Services;

namespace UkiChat.Extensions;

public class LocExtension : MarkupExtension
{
    private readonly ILocalizationService _localizationService;
    private WeakReference _targetObject;
    private DependencyProperty? _targetProperty;

    public LocExtension()
    {
        _localizationService = ContainerLocator.Container.Resolve<ILocalizationService>();
        _localizationService.LanguageChanged += (_, __) => UpdateTarget();
    }

    private void UpdateTarget()
    {
        if (_targetObject?.IsAlive != true || _targetProperty == null) return;

        if (_targetObject.Target is DependencyObject dobj)
        {
            dobj.Dispatcher.Invoke(() =>
            {
                dobj.SetValue(_targetProperty, _localizationService.GetString(Key));
            });
        }
    }

    public string Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var valueTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
        _targetObject = new WeakReference(valueTarget?.TargetObject);
        _targetProperty = valueTarget?.TargetProperty as DependencyProperty;
        return _localizationService.GetString(Key);
    }
}