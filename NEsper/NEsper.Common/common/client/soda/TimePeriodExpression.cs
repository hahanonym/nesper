///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;

namespace com.espertech.esper.common.client.soda
{
    /// <summary>
    /// Represent an expression
    /// </summary>
    public class TimePeriodExpression : ExpressionBase
    {
        private bool hasYears;
        private bool hasMonths;
        private bool hasWeeks;
        private bool hasDays;
        private bool hasHours;
        private bool hasMinutes;
        private bool hasSeconds;
        private bool hasMilliseconds;
        private bool hasMicroseconds;

        /// <summary>
        /// Ctor.
        /// </summary>
        public TimePeriodExpression()
        {
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="hasYears">flag to indicate that a year-part expression exists</param>
        /// <param name="hasMonths">flag to indicate that a month-part expression exists</param>
        /// <param name="hasWeeks">flag to indicate that a week-part expression exists</param>
        /// <param name="hasDays">flag to indicate that a day-part expression exists</param>
        /// <param name="hasHours">flag to indicate that a hour-part expression exists</param>
        /// <param name="hasMinutes">flag to indicate that a minute-part expression exists</param>
        /// <param name="hasSeconds">flag to indicate that a seconds-part expression exists</param>
        /// <param name="hasMilliseconds">flag to indicate that a millisec-part expression exists</param>
        /// <param name="hasMicroseconds">flag to indicate that a microsecond-part expression exists</param>
        public TimePeriodExpression(
            bool hasYears,
            bool hasMonths,
            bool hasWeeks,
            bool hasDays,
            bool hasHours,
            bool hasMinutes,
            bool hasSeconds,
            bool hasMilliseconds,
            bool hasMicroseconds)
        {
            this.hasYears = hasYears;
            this.hasMonths = hasMonths;
            this.hasWeeks = hasWeeks;
            this.hasDays = hasDays;
            this.hasHours = hasHours;
            this.hasMinutes = hasMinutes;
            this.hasSeconds = hasSeconds;
            this.hasMilliseconds = hasMilliseconds;
            this.hasMicroseconds = hasMicroseconds;
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="yearsExpr">expression returning years value, or null if no such part</param>
        /// <param name="monthsExpr">expression returning months value, or null if no such part</param>
        /// <param name="weeksExpr">expression returning weeks value, or null if no such part</param>
        /// <param name="daysExpr">expression returning days value, or null if no such part</param>
        /// <param name="hoursExpr">expression returning hours value, or null if no such part</param>
        /// <param name="minutesExpr">expression returning minutes value, or null if no such part</param>
        /// <param name="secondsExpr">expression returning seconds value, or null if no such part</param>
        /// <param name="millisecondsExpr">expression returning millisec value, or null if no such part</param>
        /// <param name="microsecondsExpr">expression returning microsecond value, or null if no such part</param>
        public TimePeriodExpression(
            Expression yearsExpr,
            Expression monthsExpr,
            Expression weeksExpr,
            Expression daysExpr,
            Expression hoursExpr,
            Expression minutesExpr,
            Expression secondsExpr,
            Expression millisecondsExpr,
            Expression microsecondsExpr)
        {
            AddExpr(yearsExpr, monthsExpr, weeksExpr, daysExpr, hoursExpr, minutesExpr, secondsExpr, millisecondsExpr, microsecondsExpr);
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="hasYears">flag to indicate that a year-part expression exists</param>
        /// <param name="hasMonths">flag to indicate that a month-part expression exists</param>
        /// <param name="hasWeeks">flag to indicate that a week-part expression exists</param>
        /// <param name="hasDays">flag to indicate that a day-part expression exists</param>
        /// <param name="hasHours">flag to indicate that a hour-part expression exists</param>
        /// <param name="hasMinutes">flag to indicate that a minute-part expression exists</param>
        /// <param name="hasSeconds">flag to indicate that a seconds-part expression exists</param>
        /// <param name="hasMilliseconds">flag to indicate that a millisec-part expression exists</param>
        public TimePeriodExpression(
            bool hasYears,
            bool hasMonths,
            bool hasWeeks,
            bool hasDays,
            bool hasHours,
            bool hasMinutes,
            bool hasSeconds,
            bool hasMilliseconds)
            : this(hasYears, hasMonths, hasWeeks, hasDays, hasHours, hasMinutes, hasSeconds, hasMilliseconds, false)
        {
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="hasDays">flag to indicate that a day-part expression exists</param>
        /// <param name="hasHours">flag to indicate that a hour-part expression exists</param>
        /// <param name="hasMinutes">flag to indicate that a minute-part expression exists</param>
        /// <param name="hasSeconds">flag to indicate that a seconds-part expression exists</param>
        /// <param name="hasMilliseconds">flag to indicate that a millisec-part expression exists</param>
        public TimePeriodExpression(
            bool hasDays,
            bool hasHours,
            bool hasMinutes,
            bool hasSeconds,
            bool hasMilliseconds)
            : this(false, false, false, hasDays, hasHours, hasMinutes, hasSeconds, hasMilliseconds, false)
        {
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="yearsExpr">expression returning years value, or null if no such part</param>
        /// <param name="monthsExpr">expression returning months value, or null if no such part</param>
        /// <param name="weeksExpr">expression returning weeks value, or null if no such part</param>
        /// <param name="daysExpr">expression returning days value, or null if no such part</param>
        /// <param name="hoursExpr">expression returning hours value, or null if no such part</param>
        /// <param name="minutesExpr">expression returning minutes value, or null if no such part</param>
        /// <param name="secondsExpr">expression returning seconds value, or null if no such part</param>
        /// <param name="millisecondsExpr">expression returning millisec value, or null if no such part</param>
        public TimePeriodExpression(
            Expression yearsExpr,
            Expression monthsExpr,
            Expression weeksExpr,
            Expression daysExpr,
            Expression hoursExpr,
            Expression minutesExpr,
            Expression secondsExpr,
            Expression millisecondsExpr)
            : this(yearsExpr, monthsExpr, weeksExpr, daysExpr, hoursExpr, minutesExpr, secondsExpr, millisecondsExpr, null)
        {
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="daysExpr">expression returning days value, or null if no such part</param>
        /// <param name="hoursExpr">expression returning hours value, or null if no such part</param>
        /// <param name="minutesExpr">expression returning minutes value, or null if no such part</param>
        /// <param name="secondsExpr">expression returning seconds value, or null if no such part</param>
        /// <param name="millisecondsExpr">expression returning millisec value, or null if no such part</param>
        public TimePeriodExpression(
            Expression daysExpr,
            Expression hoursExpr,
            Expression minutesExpr,
            Expression secondsExpr,
            Expression millisecondsExpr)
            : this(null, null, null, daysExpr, hoursExpr, minutesExpr, secondsExpr, millisecondsExpr, null)
        {
        }

        private void AddExpr(
            Expression yearsExpr,
            Expression monthExpr,
            Expression weeksExpr,
            Expression daysExpr,
            Expression hoursExpr,
            Expression minutesExpr,
            Expression secondsExpr,
            Expression millisecondsExpr,
            Expression microsecondsExpr)
        {
            if (yearsExpr != null) {
                hasYears = true;
                this.AddChild(yearsExpr);
            }

            if (monthExpr != null) {
                hasMonths = true;
                this.AddChild(monthExpr);
            }

            if (weeksExpr != null) {
                hasWeeks = true;
                this.AddChild(weeksExpr);
            }

            if (daysExpr != null) {
                hasDays = true;
                this.AddChild(daysExpr);
            }

            if (hoursExpr != null) {
                hasHours = true;
                this.AddChild(hoursExpr);
            }

            if (minutesExpr != null) {
                hasMinutes = true;
                this.AddChild(minutesExpr);
            }

            if (secondsExpr != null) {
                hasSeconds = true;
                this.AddChild(secondsExpr);
            }

            if (millisecondsExpr != null) {
                hasMilliseconds = true;
                this.AddChild(millisecondsExpr);
            }

            if (microsecondsExpr != null) {
                hasMicroseconds = true;
                this.AddChild(microsecondsExpr);
            }
        }

        /// <summary>
        /// Returns true if a subexpression exists that is a day-part.
        /// </summary>
        /// <returns>indicator for presence of part</returns>
        public bool IsDays {
            get => hasDays;
        }

        /// <summary>
        /// Set to true if a subexpression exists that is a day-part.
        /// </summary>
        /// <param name="hasDays">for presence of part</param>
        public void SetHasDays(bool hasDays)
        {
            this.hasDays = hasDays;
        }

        /// <summary>
        /// Returns true if a subexpression exists that is a hour-part.
        /// </summary>
        /// <returns>indicator for presence of part</returns>
        public bool IsHours {
            get => hasHours;
        }

        /// <summary>
        /// Set to true if a subexpression exists that is a hour-part.
        /// </summary>
        /// <param name="hasHours">for presence of part</param>
        public void SetHasHours(bool hasHours)
        {
            this.hasHours = hasHours;
        }

        /// <summary>
        /// Returns true if a subexpression exists that is a minutes-part.
        /// </summary>
        /// <returns>indicator for presence of part</returns>
        public bool IsMinutes {
            get => hasMinutes;
        }

        /// <summary>
        /// Set to true if a subexpression exists that is a minutes-part.
        /// </summary>
        /// <param name="hasMinutes">for presence of part</param>
        public void SetHasMinutes(bool hasMinutes)
        {
            this.hasMinutes = hasMinutes;
        }

        /// <summary>
        /// Returns true if a subexpression exists that is a seconds-part.
        /// </summary>
        /// <returns>indicator for presence of part</returns>
        public bool IsSeconds {
            get => hasSeconds;
        }

        /// <summary>
        /// Set to true if a subexpression exists that is a seconds-part.
        /// </summary>
        /// <param name="hasSeconds">for presence of part</param>
        public void SetHasSeconds(bool hasSeconds)
        {
            this.hasSeconds = hasSeconds;
        }

        /// <summary>
        /// Returns true if a subexpression exists that is a milliseconds-part.
        /// </summary>
        /// <returns>indicator for presence of part</returns>
        public bool IsMilliseconds {
            get => hasMilliseconds;
        }

        /// <summary>
        /// Set to true if a subexpression exists that is a msec-part.
        /// </summary>
        /// <param name="hasMilliseconds">for presence of part</param>
        public void SetHasMilliseconds(bool hasMilliseconds)
        {
            this.hasMilliseconds = hasMilliseconds;
        }

        /// <summary>
        /// Returns true if a subexpression exists that is a year-part.
        /// </summary>
        /// <returns>indicator for presence of part</returns>
        public bool IsYears {
            get => hasYears;
        }

        /// <summary>
        /// Set to true if a subexpression exists that is a year-part.
        /// </summary>
        /// <param name="hasYears">for presence of part</param>
        public void SetHasYears(bool hasYears)
        {
            this.hasYears = hasYears;
        }

        /// <summary>
        /// Returns true if a subexpression exists that is a month-part.
        /// </summary>
        /// <returns>indicator for presence of part</returns>
        public bool IsMonths {
            get => hasMonths;
        }

        /// <summary>
        /// Set to true if a subexpression exists that is a month-part.
        /// </summary>
        /// <param name="hasMonths">for presence of part</param>
        public void SetHasMonths(bool hasMonths)
        {
            this.hasMonths = hasMonths;
        }

        /// <summary>
        /// Returns true if a subexpression exists that is a weeks-part.
        /// </summary>
        /// <returns>indicator for presence of part</returns>
        public bool IsWeeks {
            get => hasWeeks;
        }

        /// <summary>
        /// Set to true if a subexpression exists that is a weeks-part.
        /// </summary>
        /// <param name="hasWeeks">for presence of part</param>
        public void SetHasWeeks(bool hasWeeks)
        {
            this.hasWeeks = hasWeeks;
        }

        /// <summary>
        /// Returns true if a subexpression exists that is a microsecond-part.
        /// </summary>
        /// <returns>indicator for presence of part</returns>
        public bool IsMicroseconds {
            get => hasMicroseconds;
        }

        /// <summary>
        /// Set to true if a subexpression exists that is a microsecond-part.
        /// </summary>
        /// <param name="hasMicroseconds">indicator for presence of part</param>
        public void SetHasMicroseconds(bool hasMicroseconds)
        {
            this.hasMicroseconds = hasMicroseconds;
        }

        public override ExpressionPrecedenceEnum Precedence {
            get => ExpressionPrecedenceEnum.UNARY;
        }

        public override void ToPrecedenceFreeEPL(TextWriter writer)
        {
            string delimiter = "";
            int countExpr = 0;
            if (hasYears) {
                this.Children[countExpr].ToEPL(writer, ExpressionPrecedenceEnum.MINIMUM);
                writer.Write(" years");
                delimiter = " ";
                countExpr++;
            }

            if (hasMonths) {
                writer.Write(delimiter);
                this.Children[countExpr].ToEPL(writer, ExpressionPrecedenceEnum.MINIMUM);
                writer.Write(" months");
                delimiter = " ";
                countExpr++;
            }

            if (hasWeeks) {
                writer.Write(delimiter);
                this.Children[countExpr].ToEPL(writer, ExpressionPrecedenceEnum.MINIMUM);
                writer.Write(" weeks");
                delimiter = " ";
                countExpr++;
            }

            if (hasDays) {
                writer.Write(delimiter);
                this.Children[countExpr].ToEPL(writer, ExpressionPrecedenceEnum.MINIMUM);
                writer.Write(" days");
                delimiter = " ";
                countExpr++;
            }

            if (hasHours) {
                writer.Write(delimiter);
                this.Children[countExpr].ToEPL(writer, ExpressionPrecedenceEnum.MINIMUM);
                writer.Write(" hours");
                delimiter = " ";
                countExpr++;
            }

            if (hasMinutes) {
                writer.Write(delimiter);
                this.Children[countExpr].ToEPL(writer, ExpressionPrecedenceEnum.MINIMUM);
                writer.Write(" minutes");
                delimiter = " ";
                countExpr++;
            }

            if (hasSeconds) {
                writer.Write(delimiter);
                this.Children[countExpr].ToEPL(writer, ExpressionPrecedenceEnum.MINIMUM);
                writer.Write(" seconds");
                delimiter = " ";
                countExpr++;
            }

            if (hasMilliseconds) {
                writer.Write(delimiter);
                this.Children[countExpr].ToEPL(writer, ExpressionPrecedenceEnum.MINIMUM);
                writer.Write(" milliseconds");
                delimiter = " ";
                countExpr++;
            }

            if (hasMicroseconds) {
                writer.Write(delimiter);
                this.Children[countExpr].ToEPL(writer, ExpressionPrecedenceEnum.MINIMUM);
                writer.Write(" microseconds");
            }
        }
    }
} // end of namespace