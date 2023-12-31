namespace LethalQuantities.Objects
{
    public class ItemInformation
    {
        public int maxValue {  get; set; }
        public int minValue {  get; set; }

        public ItemInformation(int min, int max) {
            maxValue = max;
            minValue = min;
        }
    }
}
