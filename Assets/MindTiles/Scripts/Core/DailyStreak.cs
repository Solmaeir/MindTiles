using System;
using System.Globalization;

namespace MindTiles.Core
{
    // The weekly streak shown on the menu: one box per day, filled for every
    // consecutive day on which the player finished the Daily Streak run
    // (all 10 levels). Missing a whole day breaks the chain. After 7 days the
    // bar starts over.
    //
    // Stored in the local save file, dated with the device's local calendar day.
    // (That also means changing the phone's clock changes the streak; fine for
    // an offline game.)
    public static class DailyStreak
    {
        public const int WeekLength = 7;
        private const string DateFormat = "yyyy-MM-dd";

        private static DailyStreakSave Save => SaveSystem.Data.dailyStreak;

        private static DateTime Today => DateTime.Now.Date;

        private static bool TryGetLastDay(out DateTime day)
        {
            return DateTime.TryParseExact(Save.lastDay, DateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out day);
        }

        // The run was already finished today (a clock set back counts as today too).
        public static bool DoneToday => TryGetLastDay(out DateTime last) && last >= Today;

        // Days in a row, counting today if it is done; 0 once a day was missed.
        public static int Current
        {
            get
            {
                if (Save.count <= 0 || !TryGetLastDay(out DateTime last)) return 0;
                return (Today - last).Days <= 1 ? Save.count : 0;
            }
        }

        // Boxes to show as filled, 0..7. A finished week stays full for the rest
        // of that day, then the next day starts a fresh bar.
        public static int FilledBoxes
        {
            get
            {
                int streak = Current;
                if (streak == 0) return 0;

                int inWeek = streak % WeekLength;
                if (inWeek == 0) return DoneToday ? WeekLength : 0;
                return inWeek;
            }
        }

        // Called when a Daily run is finished. Returns false if today already counted.
        public static bool RecordCompletion()
        {
            if (DoneToday) return false;

            DailyStreakSave s = Save;
            s.count = Current + 1;      // chain alive: +1; chain broken (Current = 0): starts again at 1
            s.lastDay = Today.ToString(DateFormat, CultureInfo.InvariantCulture);
            SaveSystem.Save();
            return true;
        }
    }
}
