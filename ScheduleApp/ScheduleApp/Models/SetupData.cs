using System.Collections.Generic;

namespace ScheduleApp.Models
{
    public class SetupData
    {
        public List<Teacher> Teachers { get; set; } = new List<Teacher>();
        public List<Support> Supports { get; set; } = new List<Support>();
        public List<RoomPreference> Preferences { get; set; } = new List<RoomPreference>();
    }
}