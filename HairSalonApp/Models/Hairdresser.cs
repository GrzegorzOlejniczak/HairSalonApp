using System.Globalization;

namespace HairSalonApp.Models
{
    public class Hairdresser
    {
        public int Id { get; set; }
        public String Firstname { get; set; }
        public string Lastname { get; set; }

        public string Email { get; set; }


        public List<Appointment> Appointments { get; set; }
    }
}
