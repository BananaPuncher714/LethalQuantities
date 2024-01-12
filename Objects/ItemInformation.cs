namespace LethalQuantities.Objects
{
    public class ItemInformation
    {
        public int maxValue {  get; set; }
        public int minValue {  get; set; }
        public bool conductive { get; set; }

        public ItemInformation(int min, int max, bool conductive)
        {
            maxValue = max;
            minValue = min;
            this.conductive = conductive;
        }
    }
}
