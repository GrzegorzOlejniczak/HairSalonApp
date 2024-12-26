using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HairSalonApp.Data;
using HairSalonApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Runtime.InteropServices;
using System.Globalization;


namespace HairSalonApp.Controllers
{
    [Authorize]
    [Route("Appointments")]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AppointmentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //GET: Reserved hours
        [HttpGet("GetReservedHours")]
        public IActionResult GetReservedHours(string hairdresserId, int year, int month, int day, string hour )
        {
            DateTime selectedTime;
            if (!DateTime.TryParseExact(hour, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out selectedTime))
            {
                return BadRequest("Invalid hour format.");
            }

            // Tworzymy pełną datę na podstawie przekazanych danych: rok, miesiąc, dzień, oraz godzinę z parametru
            selectedTime = new DateTime(year, month, day, selectedTime.Hour, selectedTime.Minute, 0);

            // Pobieramy wszystkie zarezerwowane wizyty na dany dzień dla wybranego fryzjera
            var reservedAppointments = _context.Appointment
                .Where(a => a.HairdresserId == hairdresserId &&
                            a.AppointmentDate.Year == year &&
                            a.AppointmentDate.Month == month &&
                            a.AppointmentDate.Day == day)
                .Include(a => a.Service)  // Pobieramy usługę, aby wiedzieć, ile trwa
                .ToList();

            // Lista godzin, które są już zarezerwowane
            List<DateTime> blockedHours = new List<DateTime>();

            // Dla każdej zarezerwowanej wizyty:
            foreach (var appointment in reservedAppointments)
            {
                // Czas rozpoczęcia wizyty
                var startTime = appointment.AppointmentDate;
                // Czas trwania usługi w godzinach (jeśli usługa trwa np. 90 minut, to zamieniamy to na godziny)
                var serviceDurationInHours = appointment.Service.Duration / 60;

                // Dodajemy godziny, które są zarezerwowane na podstawie tej wizyty
                for (int i = 0; i < serviceDurationInHours; i++)
                {
                    blockedHours.Add(startTime.AddHours(i));
                }
            }


            var availableServices = _context.Service
                 .ToList() // Wczytujemy usługi do pamięci
                 .Where(service =>
                 {
                     // Obliczamy zakres czasowy wybranej usługi
                     var serviceEndTime = selectedTime.AddMinutes(service.Duration);

                     // Sprawdzamy kolizje dla każdej godziny w tym zakresie
                     for (var time = selectedTime; time < serviceEndTime; time = time.AddMinutes(60))
                     {
                         if (blockedHours.Contains(time))
                         {
                             return false; // Jeśli którakolwiek godzina jest zablokowana, usługa nie jest dostępna
                         }
                     }

                     return true; // Usługa dostępna, jeśli nie ma kolizji
                 })
                 .Select(service => new { service.Id, service.Name, service.Duration, service.Price })
                 .ToList();

            ViewBag.ServiceId = availableServices;

            return Json(new
            {
                availableServices,
                blockedHours,

            });
        }

        // GET: Appointments
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            //sprwadzenie czy zalogowany uzytkownik ma role Hairdresser
            var isHairdresser = await _userManager.IsInRoleAsync(user, "Hairdresser");

            IQueryable<Appointment> applicationDbContext;

            if (isHairdresser)
            {
                // Jeśli użytkownik jest fryzjerem, pokazujemy tylko wizyty przypisane do niego
                applicationDbContext = _context.Appointment
                    .Where(a => a.HairdresserId == user.Id) 
                    .Include(a => a.Hairdresser)            
                    .Include(a => a.Service)               
                    .Include(a => a.User)
                    .OrderByDescending(a => a.AppointmentDate);
            }
            else
            {
                // Jeśli użytkownik nie jest fryzjerem, pokazujemy tylko wizyty przypisane do niego
                applicationDbContext = _context.Appointment
                    .Where(a => a.UserId == user.Id) 
                    .Include(a => a.Hairdresser)     
                    .Include(a => a.Service)   
                    .Include(a => a.User)
                    .OrderByDescending(a => a.AppointmentDate);
            }

            // Zwrócenie widoku z wynikami
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Appointments/Details/5
        [HttpGet("Details")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointment
                .Include(a => a.Hairdresser)
                .Include(a => a.Service)
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // GET: Appointments/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {

            // przypisanie aktualnie zalogowanego uzytkownika
            var userId = _userManager.GetUserId(User);

            // pobranie użytkowników z rolą "Hairdresser" (już będzie typu ApplicationUser)
            var hairdressers = await _userManager.GetUsersInRoleAsync("Hairdresser");

            List<ApplicationUser> applicationUsers = hairdressers.Cast<ApplicationUser>().ToList();


            //var services = _context.Service
            //.Select(service => new SelectListItem
            //{
            //    Text = $"{service.Name} - {service.Duration/60} godz. Cena usługi:",  // Łączenie nazwy i czasu trwania
            //    Value = service.Id.ToString()
            //})
            //.ToList();

            // przekazanie danych do widoku
            ViewBag.Hairdressers = applicationUsers;
            ViewBag.UserId = new SelectList(new[] { userId }, userId);
            //ViewBag.ServiceId = new SelectList(_context.Service, "Id", "Name", "Duration");
            //ViewBag.ServiceId = services;

            return View();
        }

        // POST: Appointments/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,HairdresserId,AppointmentDate,ServiceId")] Appointment appointment)
        {
            var userId = _userManager.GetUserId(User);

            appointment.UserId = userId;



            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Validation Error: {error.ErrorMessage}");
                    }
                }
            }

            // Debugowanie wartości appointment
            System.Diagnostics.Debug.WriteLine($"AppointmentDate: {appointment.AppointmentDate}");
            System.Diagnostics.Debug.WriteLine($"HairdresserId: {appointment.HairdresserId}");
            System.Diagnostics.Debug.WriteLine($"ServiceId: {appointment.ServiceId}");
            System.Diagnostics.Debug.WriteLine($"UserId: {appointment.UserId}");


            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(appointment);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "Wystąpił błąd podczas zapisywania danych.");
                }
            }
            var hairdressers = await _userManager.GetUsersInRoleAsync("Hairdresser");
            var applicationUsers = hairdressers.OfType<ApplicationUser>().ToList();
            ViewBag.Hairdressers = applicationUsers.Any() ? applicationUsers : new List<ApplicationUser>();

            var services = _context.Service
                .Select(service => new SelectListItem
                {
                    Text = $"{service.Name} - {service.Duration / 60} godz. Cena usługi:",
                    Value = service.Id.ToString()
                })
                .ToList();

            ViewBag.ServiceId = services;
            ViewBag.UserId = new SelectList(new[] { userId }, userId);

            return View(appointment);
        }

        // GET: Appointments/Edit/5
        [HttpGet("Edit")]
        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointment
               .Include(a => a.Hairdresser)
               .Include(a => a.Service)
               .Include(a => a.User)
               .FirstOrDefaultAsync(a => a.Id == id);

           
            if (appointment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var isHairdresser = await _userManager.IsInRoleAsync(user, "Hairdresser");

            // Sprawdzenie, czy użytkownik jest przypisany do wizyty
            if (appointment.UserId != user.Id && (!isHairdresser || appointment.HairdresserId != user.Id))
            {
                return Forbid(); // Jeśli użytkownik nie jest właścicielem wizyty ani fryzjerem, blokujemy dostęp
            }

            // Pobranie fryzjerów i usług
            var hairdressers = await _userManager.GetUsersInRoleAsync("Hairdresser");
            List<ApplicationUser> applicationUsers = hairdressers.Cast<ApplicationUser>().ToList();

            var services = _context.Service
                .Select(service => new SelectListItem
                {
                    Text = $"{service.Name} - {service.Duration / 60} godz. Cena usługi:",
                    Value = service.Id.ToString()
                })
                .ToList();

            // Przekazanie danych do widoku
            ViewBag.Hairdressers = applicationUsers;
            ViewBag.ServiceId = services;
            ViewBag.UserId = new SelectList(new[] { appointment.UserId }, appointment.UserId);

            return View(appointment);
        }

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,HairdresserId,AppointmentDate,ServiceId")] Appointment appointment)
        {
            if (id != appointment.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var isHairdresser = await _userManager.IsInRoleAsync(user, "Hairdresser");

            if (appointment.UserId != user.Id && (!isHairdresser || appointment.HairdresserId != user.Id))
            {
                return Forbid(); // Blokowanie dostępu, jeśli użytkownik nie ma uprawnień
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            var hairdressers = await _userManager.GetUsersInRoleAsync("Hairdresser");
            var applicationUsers = hairdressers.OfType<ApplicationUser>().ToList();

            var services = _context.Service
                .Select(service => new SelectListItem
                {
                    Text = $"{service.Name} - {service.Duration / 60} godz. Cena usługi:",
                    Value = service.Id.ToString()
                })
                .ToList();

            ViewBag.Hairdressers = applicationUsers;
            ViewBag.ServiceId = services;
            ViewBag.UserId = new SelectList(new[] { appointment.UserId }, appointment.UserId);

            return View(appointment);
        }

        // GET: Appointments/Delete/5
        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointment
                .Include(a => a.Hairdresser)
                .Include(a => a.Service)
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var isHairdresser = await _userManager.IsInRoleAsync(user, "Hairdresser");

            // Sprawdzenie, czy użytkownik jest właścicielem wizyty lub przypisanym fryzjerem
            if (appointment.UserId != user.Id && (!isHairdresser || appointment.HairdresserId != user.Id))
            {
                return Forbid(); // Blokowanie dostępu, jeśli użytkownik nie ma uprawnień
            }

            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost("Delete/{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointment
        .Include(a => a.Hairdresser)
        .Include(a => a.User)
        .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var isHairdresser = await _userManager.IsInRoleAsync(user, "Hairdresser");

            // Sprawdzenie, czy użytkownik jest właścicielem wizyty lub przypisanym fryzjerem
            if (appointment.UserId != user.Id && (!isHairdresser || appointment.HairdresserId != user.Id))
            {
                return Forbid(); // Blokowanie dostępu, jeśli użytkownik nie ma uprawnień
            }

            // Usuwanie wizyty
            _context.Appointment.Remove(appointment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointment.Any(e => e.Id == id);
        }
    }
}
