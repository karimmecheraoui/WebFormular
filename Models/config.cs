using Microsoft.AspNetCore.Mvc;

namespace WebformularFuerMit.Models
{


    public class Config
    {
        public List<Priority> Priorities { get; set; } = new();
        public List<DistributionList> DistributionLists { get; set; } = new();
        public List<HeaderConfig> headers { get; set; }
    }
   

}
