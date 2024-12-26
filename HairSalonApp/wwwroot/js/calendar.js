const currentYear = new Date().getFullYear();
let selectedMonth = new Date().getMonth(); // Bieżący miesiąc (0 - styczeń, 11 - grudzień)
const currentDay = new Date().getDate(); // Bieżący dzień
const currentHour = new Date().getHours(); // Bieżąca godzina
let selectedYear = currentYear;

document.addEventListener("DOMContentLoaded", function () {
    const daysContainer = document.querySelector(".days");
    const hours = document.querySelectorAll(".hours li:not(.taken)");
    const serviceOptions = document.querySelector(".service-options");
    const submitButton = document.querySelector("button");
    const resultContainer = document.createElement("div");
    const selectedDay = document.querySelector(".days .active")?.textContent;
    submitButton.insertAdjacentElement("afterend", resultContainer);


    //let selectedYear = currentYear;

    

    const monthLabel = document.querySelector("#monthText");
    const dateString = `${currentYear}-${String(selectedMonth + 1).padStart(2, '0')}-${String(currentDay).padStart(2, '0')}T${String(currentHour).padStart(2, '0')}:00`;

    // Ustawienie aktualnego dnia w input datetime-local
    document.querySelector("input[name='AppointmentDate']").value = dateString;

    function updateMonthLabel() {
        const monthNames = [
            "Styczeń", "Luty", "Marzec", "Kwiecień", "Maj", "Czerwiec",
            "Lipiec", "Sierpień", "Wrzesień", "Październik", "Listopad", "Grudzień"
        ];
        monthLabel.textContent = `${monthNames[selectedMonth]} ${selectedYear}`;
    }

    const prevButton = document.querySelector(".prev");
    const nextButton = document.querySelector(".next");

    prevButton.addEventListener("click", function () {
        const today = new Date();
        if (selectedYear === today.getFullYear() && selectedMonth === today.getMonth()) {
            return; // Prevent going to past months
        }

        if (selectedMonth === 0) {
            selectedMonth = 11;
            selectedYear--;
        } else {
            selectedMonth--;
        }

        updateMonthLabel();
        renderDays();
    });

    nextButton.addEventListener("click", function () {
        const today = new Date();
        const maxYear = today.getFullYear() + 1;
        const maxMonth = today.getMonth();

        if (selectedYear > maxYear || (selectedYear === maxYear && selectedMonth === maxMonth)) {
            return; // Prevent going beyond one year from now
        }

        if (selectedMonth === 11) {
            selectedMonth = 0;
            selectedYear++;
        } else {
            selectedMonth++;
        }

        updateMonthLabel();
        renderDays();
    });

    // Renderowanie dni miesiąca
    function renderDays() {
        const daysInMonth = new Date(selectedYear, selectedMonth + 1, 0).getDate();
        const firstDayOfMonth = new Date(selectedYear, selectedMonth, 1).getDay();

        // Clear days container
        if (daysContainer) {
            daysContainer.innerHTML = "";
        }

        // Adjust for Sunday (JavaScript `getDay` returns 0 for Sunday)
        const adjustedFirstDay = firstDayOfMonth === 0 ? 6 : firstDayOfMonth - 1;

        // Add empty slots for days before the first day of the month
        for (let i = 0; i < adjustedFirstDay; i++) {
            const emptySlot = document.createElement("li");
            emptySlot.classList.add("empty");
            daysContainer.appendChild(emptySlot);
        }

        // Add days of the month
        for (let day = 1; day <= daysInMonth; day++) {
            const dayElement = document.createElement("li");
            const currentDayOfWeek = (adjustedFirstDay + day - 1) % 7; // Calculate the day of the week (0=Monday, 6=Sunday)

            dayElement.textContent = day;

            // zablokowanie z wyboru dni przed aktualnym dniem
            const dayDate = new Date(selectedYear, selectedMonth, day);

            // Tworzymy obiekt daty dla dzisiejszego dnia, ale ustawiamy godzinę, minutę i sekundę na 0,
            // aby porównać tylko dni (bez uwzględniania godzin).
            const today = new Date();
            today.setHours(0, 0, 0, 0); // Ustawiamy godzinę na 00:00:00, aby porównać tylko daty.

            if (dayDate < today) {
                dayElement.classList.add("taken"); // Dodanie klasy do przeszłych dni
            }

            // Mark weekends (Saturday=5, Sunday=6)
            if (currentDayOfWeek === 5 || currentDayOfWeek === 6 || dayDate < today) {
                dayElement.classList.add("taken");
            } else {
                dayElement.addEventListener("click", function () {
                    document.querySelector(".days .active")?.classList.remove("active");
                    this.classList.add("active");
                    const selectedDay = this.textContent;
                    //const hairdresserId = document.querySelector("#SelectedHairdresserId").value;

                    const hairdresserId = document.querySelector("input[name='HairdresserId']").value;

                    console.log("TEST" + hairdresserId);
                    //const selectedHour = this.textContent;
                    console.log("DZIEN");
                    fetchAvailableHours(selectedYear, selectedMonth, selectedDay, "00:00", hairdresserId);
                    updateAppointmentDate();
                });
            }

            daysContainer.appendChild(dayElement);
        }

        const today = new Date();
        const currentDayOfWeek = today.getDay(); // 0=Sunday, 6=Saturday
        if ((currentDayOfWeek === 6 || currentDayOfWeek === 0) && firstAvailableDay) {
            firstAvailableDay.classList.add("active");
            const selectedDay = firstAvailableDay.textContent;
            const hairdresserId = document.querySelector("input[name='HairdresserId']").value;

            fetchAvailableHours(selectedYear, selectedMonth, selectedDay, "00:00", hairdresserId);
            updateAppointmentDate();
        }



        // Set the active day to the current day, if it's available
        const todayElement = [...daysContainer.querySelectorAll("li")].find(
            li => parseInt(li.textContent) === currentDay
        );
        if (todayElement && !todayElement.classList.contains("taken")) {
            todayElement.classList.add("active");
            updateAppointmentDate();
        }

        
        // Zaktualizowanie daty w input po zmianie miesiąca
        updateAppointmentDate();
    }

    

    hours.forEach(hour => {
        hour.addEventListener("click", function () {
            document.querySelector(".hours .active")?.classList.remove("active");
            this.classList.add("active");
            updateAppointmentDate();
            const selectedHour = this.textContent;
            const selectedDay = document.querySelector(".days .active")?.textContent;
            var hairdresserId = document.querySelector("input[name='HairdresserId']").value;
            fetchAvailableHours(selectedYear, selectedMonth, selectedDay, selectedHour, hairdresserId);
        });
    });

    submitButton.addEventListener("click", function () {
        const selectedDay = document.querySelector(".days .active")?.textContent;
        const selectedHour = document.querySelector(".hours .active")?.textContent;
        const selectedService = serviceOptions.value;

        //if (selectedDay && selectedHour && selectedService) {
        //    resultContainer.textContent = `Wybrana data: ${selectedDay}.${selectedMonth + 1}.${selectedYear}, godzina: ${selectedHour}, usługa: ${selectedService}`;
        //} else {
        //    resultContainer.textContent = "Proszę wybrać dzień, godzinę i usługę.";
        //}
    });

    // Inicjalizacja widoku i renderowanie dni
    updateMonthLabel();
    renderDays();
});



async function selectHairdresser(hairdresserId, element) {

    // Ustaw ID wybranego fryzjera w ukrytym polu
    document.querySelector("input[name='HairdresserId']").value = hairdresserId;
    document.getElementById('SelectedHairdresserId').value = hairdresserId;

    var allImgs = document.querySelectorAll('.stylist img'); // Wybieramy wszystkie elementy <img> w klasie .stylist
    allImgs.forEach(function (img) {
        img.classList.remove('selected');
    });

    // Dodajemy klasę 'selected' tylko do klikniętego obrazka
    var clickedImg = element.querySelector('img');  // Wybieramy obrazek w klikniętym elemencie
    clickedImg.classList.add('selected');

    document.querySelector(".days").classList.remove("disabled");
    document.querySelector(".hours").classList.remove("disabled");

    const selectedDay = document.querySelector(".days .active")?.textContent;

    if (!selectedDay) {
        console.log("Proszę wybrać dzień!");
        return; // Jeśli dzień nie jest wybrany, zakończ działanie
    }

    console.log(selectedDay);
    //updateAppointmentDate();
    await fetchAvailableHours(selectedYear, selectedMonth, selectedDay, "00:00", hairdresserId);

    //fetch(`/Appointments/GetReservedHours?hairdresserId=${hairdresserId}&year=${currentYear}&month=${selectedMonth + 1}&day=${selectedDay}`)
    //    .then(response => response.json())
    //    .then(reservedHours => {
    //        updateCalendarWithReservedHours(reservedHours);
    //    });

    //wybiera pierwszą dostępną godzinę
    const firstAvailableHour = document.querySelector(".hours li:not(.taken)");

    console.log(firstAvailableHour);
    if (firstAvailableHour) {
        firstAvailableHour.classList.add("active");
        updateAppointmentDate(); // Wykonanie aktualizacji daty po wybraniu godziny
    } else {
        console.log("Brak dostępnych godzin");
    }


    console.log("kliknelo !!!!!!!!");
}


function updateCalendarWithReservedHours(reservedHours) {
    const hours = document.querySelectorAll(".hours li");
    hours.forEach(hour => hour.classList.remove("taken"));

    reservedHours.forEach(reserved => {
        const reservedDate = new Date(reserved);
        if (reservedDate.getMonth() === selectedMonth && reservedDate.getFullYear() === selectedYear) {
            const hourElement = [...hours].find(hour => parseInt(hour.textContent) === reservedDate.getHours());
            if (hourElement) {
                hourElement.classList.add("taken");

            }
        }

    })
}

async function fetchAvailableHours(year, month, day, hour, hairdresserId) {
    try {
        // Wykonaj zapytanie do serwera
        const response = await fetch(`/Appointments/GetReservedHours?hairdresserId=${hairdresserId}&year=${year}&month=${month + 1}&day=${day}&hour=${hour}`);
        const data = await response.json();

        const reservedHours = data.blockedHours;
        if (Array.isArray(reservedHours)) {
            updateServiceOptions(data.availableServices);
            updateCalendarWithReservedHours(reservedHours);
        } else {
            console.error("Blocked hours data is not an array", reservedHours);
        }
    } catch (error) {
        console.error("Error fetching hours:", error);
    }
}

function updateServiceOptions(availableServices) {
    const serviceSelect = document.querySelector("select[name='ServiceId']");
    const hiddenServiceInput = document.querySelector("#SelectedServiceId");

    if (!serviceSelect) {
        console.error("Element <select> dla usług nie został znaleziony.");
        return;
    }

    serviceSelect.innerHTML = '';

    const defaultOption = document.createElement("option");
    defaultOption.value = '';
    defaultOption.textContent = 'Wybierz usługę';
    serviceSelect.appendChild(defaultOption);

    availableServices.forEach(service => {
        const option = document.createElement("option");
        option.value = service.id;  // Zmienione z Id na id (małe litery)
        option.textContent = `${service.name} | Czas trwania: ${service.duration / 60} godz. | Cena: ${service.price}`;
        serviceSelect.appendChild(option);
    });

    // Dodaj event listener na zmianę wartości
    serviceSelect.addEventListener('change', function () {
        if (hiddenServiceInput) {
            hiddenServiceInput.value = this.value;
        }
        console.log("Selected ServiceId:", this.value);
    });
}
function updateAppointmentDate() {
    const selectedHour = document.querySelector(".hours .active")?.textContent;
    const selectedDay = document.querySelector(".days .active")?.textContent;

    if (selectedDay && selectedHour) {
        // Tworzymy datę w formacie yyyy-MM-ddThh:mm
        const dateString = `${selectedYear}-${String(selectedMonth + 1).padStart(2, '0')}-${String(selectedDay).padStart(2, '0')}T${String(selectedHour).padStart(2, '0')}:00`;
        document.querySelector("input[name='AppointmentDate']").value = dateString;
    }
}