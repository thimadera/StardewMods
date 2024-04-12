using StardewValley.Objects;

namespace Thimadera.StardewMods.StackEverythingRedux.ObjectCopiers
{
    internal class FurnitureCopier : ICopier<Furniture>
    {
        public Furniture Copy(Furniture obj)
        {
            Furniture furniture = obj.getOne() as Furniture;

            int attempts = 0;
            while (!furniture.boundingBox.Value.Equals(obj.boundingBox.Value) && attempts < 8)
            {
                furniture.rotate();
                attempts++;
            }

            return furniture;
        }
    }
}
