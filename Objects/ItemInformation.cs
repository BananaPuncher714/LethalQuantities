using System;
using System.Collections.Generic;
using System.Text;

namespace LethalQuantities.Objects
{
    public class ItemInformation
    {

        public ItemInformation(int min, int max) {
            this.maxValue = max;
            this.minValue = min;
        }
        public int maxValue {  get; set; }
        public int minValue {  get; set; }
    }
}
