using System.ComponentModel.DataAnnotations;

namespace HairSalonApp.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Display(Name = "Nazwa usługi")]
        public string Name { get; set; }

        [Display(Name = "Czas trwania")]
        public int Duration { get; set; }

        [Display(Name = "Koszt")]
        public decimal Price { get; set; }
    }
}
