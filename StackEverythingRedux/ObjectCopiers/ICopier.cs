namespace Thimadera.StardewMods.StackEverythingRedux.ObjectCopiers
{
    internal interface ICopier<T>
    {
        T Copy(T obj);
    }
}
