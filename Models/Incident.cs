using Microsoft.AspNetCore.Mvc;

namespace WebformularFuerMit.Models
{
    public class Incident
    {
        public int Id { get; set; }
        public string IncidentNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string UpdateText { get; set; }
    }
}
