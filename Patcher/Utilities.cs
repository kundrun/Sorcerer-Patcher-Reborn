using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;

internal static class Utilities
{
    public static FormKey MakeFormKey(string modKey, uint formId)
    {
        return ModKey.FromNameAndExtension(modKey).MakeFormKey(formId);
    }

    public static string MakeUniqueModIdentifier(this ModKey modKey)
    {
        return new string(modKey.Name
                              .TakeWhile((c, i) => i < 4 && char.IsAsciiLetterOrDigit(c))
                              .ToArray()).ToUpper();
    }

    public static IEnumerable<TSource> WhereIf<TSource>(
        this IEnumerable<TSource> source, Func<TSource, bool> predicate, Func<bool> condition)
    {
        return condition() ? source.Where(predicate) : source;
    }
}

internal partial class Patcher
{
    private T? TryResolve<T>(FormKey formKey) where T : class, IMajorRecordGetter
    {
        return _state.LinkCache.TryResolve<T>(formKey, out var form) ? form : null;
    }

    private T? TryResolve<T>(IFormLinkGetter<T> formLink) where T : class, IMajorRecordGetter
    {
        return _state.LinkCache.TryResolve(formLink, out var form) ? form : null;
    }
}
