using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCalv3Test.Models
{
    public class GoogleCalendarAppointmentModel
    {
        public DateTime EventStartTime;
        public DateTime EventEndTime;
        public bool DeleteAppointment;
        public string EventID;
        public string EventLocation;
        public string EventTitle;
        public string EventDetails;
    }
}
