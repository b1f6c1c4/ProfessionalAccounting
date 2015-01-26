using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console.Chart
{
    public abstract class AccountingChart
    {
        private DateTime m_CurDate;
        private DateFilter m_DateFilter;

        protected readonly Accountant Accountant;

        protected AccountingChart(Accountant helper, DateTime startDate, DateTime endDate, DateTime curDate)
        {
            Accountant = helper;
            m_DateFilter = new DateFilter(startDate, endDate);
            m_CurDate = curDate;
        }

        public DateTime CurDate { get { return m_CurDate; } set { m_CurDate = value; } }

        public DateTime StartDate
        {
            // ReSharper disable once PossibleInvalidOperationException
            get { return m_DateFilter.StartDate.Value; }
            set { m_DateFilter.StartDate = value; }
        }

        public DateTime EndDate
        {
            // ReSharper disable once PossibleInvalidOperationException
            get { return m_DateFilter.EndDate.Value; }
            set { m_DateFilter.EndDate = value; }
        }

        protected DateFilter DateRange { get { return m_DateFilter; } }

        protected void SetupChartArea(ChartArea area)
        {
            area.CursorX.Position = m_CurDate.ToOADate();
            area.AxisX.IsMarginVisible = false;
            area.AxisX.Minimum = StartDate.ToOADate();
            area.AxisX.Maximum = EndDate.ToOADate();
            area.AxisX.MinorGrid.Enabled = true;
            area.AxisX.MinorGrid.Interval = 1;
            area.AxisX.MinorGrid.LineColor = Color.DarkGray;
            area.AxisX.MajorGrid.Interval = 7;
            area.AxisX.MajorGrid.LineWidth = 2;
            area.AxisX.MajorGrid.IntervalOffset = -(int)StartDate.DayOfWeek;
            area.AxisX.LabelStyle = new LabelStyle
                                        {
                                            Format = "MM-dd",
                                            Font =
                                                new Font(
                                                "Consolas",
                                                14,
                                                GraphicsUnit.Pixel)
                                        };
        }

        public abstract ChartArea Setup();

        public abstract IEnumerable<Series> Gather();
    }
}
