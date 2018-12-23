using System;
using System.Collections.Generic;
using System.Text;

namespace RavenDBTesting
{
    public class TeaProfile
    {        
        public enum TeaColorEnum
        {
            Green = 1,
            Black,
            White,
            Red,
            Blend,
            Chai,
            Oolong
        }
        public string Id { get { return Name; } }
        public string Name { get; set; }
        public decimal CaffeineMilligrams { get; set; }
        public bool IsCaffeinated { get { return CaffeineMilligrams > 0; } }
        public TeaColorEnum TeaColor { get; set; }

        //public TeaProfile()
        //{

        //}

        public TeaProfile(TeaColorEnum color, string name)
        {
            TeaColor = color;
            Name = name;
            
        }

        public TeaProfile(TeaColorEnum color, string name, decimal caffeineMilligrams)
        {
            TeaColor = color;
            Name = name;
            CaffeineMilligrams = caffeineMilligrams;
        }
    }
}
