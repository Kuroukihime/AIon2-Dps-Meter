using AionDpsMeter.Services.Services.Settings;
using System.Reflection;

namespace AionDpsMeter.Tests;

public static class TestAppSettingsService
{
    public static IAppSettingsService Create()
        => DispatchProxy.Create<IAppSettingsService, DefaultSettingsProxy>();

    public class DefaultSettingsProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null)
                throw new InvalidOperationException("Missing settings method.");

            if (targetMethod.Name == "get_HistoryRetantionPeriod")
                return 365;

            return targetMethod.ReturnType == typeof(void)
                ? null
                : targetMethod.ReturnType.IsValueType
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null;
        }
    }
}
