using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentScheduler
{
    public class Rule
    {
        private DateTime _effectiveDate;
        private DateTime? _expirationDate;
        private int _recurranceType;
        private int _daysOfWeek;
        private int _ordinality;
        private int? _month;
        private int? _dayOrdinal;
        private int? _interval;
        private int? _constant;
        private bool _useFirstFullWorkWeek;
        private string _description;

        public Rule()
        {

        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public DateTime EffectiveDate
        {
            get { return _effectiveDate; }
            set { _effectiveDate = value; }
        }

        public DateTime? ExpirationDate
        {
            get { return _expirationDate; }
            set { _expirationDate = value; }
        }

        public int RecurranceType
        {
            get { return _recurranceType; }
            set { _recurranceType = value; }
        }

        public int? Constant
        {
            get { return _constant; }
            set { _constant = value; }
        }
        public AppointmentRecurranceType RecurranceTypeEnum
        {
            get { return (AppointmentRecurranceType)_recurranceType; }
            set { _recurranceType = (int)value; }
        }

        public int? Interval
        {
            get { return _interval; }
            set { _interval = value; }
        }

        public int? Month
        {
            get { return _month; }
            set { _month = value; }
        }

        public bool UseFirstFullWorkWeek
        {
            get { return _useFirstFullWorkWeek; }
            set { _useFirstFullWorkWeek = value; }

        }

        public int DaysOfWeek
        {
            get { return _daysOfWeek; }
            set { _daysOfWeek = value; }
        }

        public int Ordinality
        {
            get { return _ordinality; }
            set { _ordinality = value; }
        }

        public CommonOrdinality OrdinalityEnum
        {
            get { return (CommonOrdinality)_ordinality; }
            set { _ordinality = (int)value; }
        }

        internal int? DayOrdinal
        {
            get { return _dayOrdinal; }
            set { _dayOrdinal = value; }
        }

        public void AppendWeekdayFlag(WeekdayFlags flag)
        {
            var days = (WeekdayFlags)_daysOfWeek;
            days |= flag;
            _daysOfWeek = (int)days;
        }

        public void SetWeekdayFlag(WeekdayFlags flag)
        {
            _daysOfWeek = (int)flag;
        }

        public bool HasWeekDayFlag(WeekdayFlags flags)
        {
            var days = (WeekdayFlags)_daysOfWeek;

            return days.HasFlag(flags);
        }

        private void ResetRecurranceValues()
        {
            Ordinality = 0;
            DaysOfWeek = 0;
            DayOrdinal = null;
            Constant = null;
            Interval = null;
            Month = null;
        }

    }
}
