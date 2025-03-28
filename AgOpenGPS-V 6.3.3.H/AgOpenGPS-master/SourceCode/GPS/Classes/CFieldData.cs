﻿using System;
using System.Text;

namespace AgOpenGPS
{
    public class CFieldData
    {
        private readonly FormGPS mf;

        //all the section area added up;
        public double workedAreaTotal;

        //just a cumulative tally based on distance and eq width.
        public double workedAreaTotalUser;

        //accumulated user distance
        public double distanceUser;

        public double barPercent = 0;

        public double overlapPercent = 0;

        //Outside area minus inner boundaries areas (m)
        public double areaBoundaryOuterLessInner;

        //used for overlap calcs - total done minus overlap
        public double actualAreaCovered;

        //Inner area of outer boundary(m)
        public double areaOuterBoundary;

        //not really used - but if needed
        public double userSquareMetersAlarm;

        //Area inside Boundary less inside boundary areas
        public string AreaBoundaryLessInnersHectares => (areaBoundaryOuterLessInner * glm.m2ha).ToString("N2");

        public string AreaBoundaryLessInnersAcres => (areaBoundaryOuterLessInner * glm.m2ac).ToString("N2");

        //USer tally string
        public string WorkedUserHectares => (workedAreaTotalUser * glm.m2ha).ToString("N2");

        //user tally string
        public string WorkedUserAcres => (workedAreaTotalUser * glm.m2ac).ToString("N2");

        //String of Area worked
        public string WorkedAcres => (workedAreaTotal * 0.000247105).ToString("N2");

        public string WorkedHectares => (workedAreaTotal * 0.0001).ToString("N2");

        //User Distance strings
        public string DistanceUserMeters => Convert.ToString(Math.Round(distanceUser, 2));

        public string DistanceUserFeet => Convert.ToString(Math.Round((distanceUser * glm.m2ft), 1));

        //remaining area to be worked
        public string WorkedAreaRemainHectares => ((areaBoundaryOuterLessInner - workedAreaTotal) * glm.m2ha).ToString("N2");

        public string WorkedAreaRemainAcres => ((areaBoundaryOuterLessInner - workedAreaTotal) * glm.m2ac).ToString("N2");

        public string WorkedAreaRemainPercentage
        {
            get
            {
                if (areaBoundaryOuterLessInner > 10)
                {
                    barPercent = ((areaBoundaryOuterLessInner - workedAreaTotal) * 100 / areaBoundaryOuterLessInner);
                    return barPercent.ToString("N1") + "%";
                }
                else
                {
                    barPercent = 0;
                    return "0%";
                }
            }
        }

        //overlap strings
        public string ActualAreaWorkedHectares => (actualAreaCovered * glm.m2ha).ToString("N2");
        public string ActualAreaWorkedAcres => (actualAreaCovered * glm.m2ac).ToString("N2");

        public string ActualRemainHectares => ((areaBoundaryOuterLessInner - actualAreaCovered) * glm.m2ha).ToString("N2");
        public string ActualRemainAcres => ((areaBoundaryOuterLessInner - actualAreaCovered) * glm.m2ac).ToString("N2");

        public string ActualOverlapPercent => overlapPercent.ToString("N1") + "% ";

        public string TimeTillFinished
        {
            get
            {
                if (mf.avgSpeed > 2)
                {
                    TimeSpan timeSpan = TimeSpan.FromHours(((areaBoundaryOuterLessInner - workedAreaTotal) * glm.m2ha
                        / (mf.tool.width * mf.avgSpeed * 0.1)));
                    return timeSpan.Hours.ToString("00:") + timeSpan.Minutes.ToString("00") + '"';
                }
                else return "\u221E Hrs";
            }
        }

        public string WorkRateHectares => (mf.tool.width * mf.avgSpeed * 0.1).ToString("N1") + " ha/hr";
        public string WorkRateAcres => (mf.tool.width * mf.avgSpeed * 0.2471).ToString("N1") + " ac/hr";

        //constructor
        public CFieldData(FormGPS _f)
        {
            mf = _f;
            workedAreaTotal = 0;
            workedAreaTotalUser = 0;
            userSquareMetersAlarm = 0;
        }

        public void UpdateFieldBoundaryGUIAreas()
        {
            if (mf.bnd.bndList.Count > 0)
            {
                areaOuterBoundary = mf.bnd.bndList[0].area;
                areaBoundaryOuterLessInner = areaOuterBoundary;

                for (int i = 1; i < mf.bnd.bndList.Count; i++)
                {
                    areaBoundaryOuterLessInner -= mf.bnd.bndList[i].area;
                }
            }
            else
            {
                areaOuterBoundary = 0;
                areaBoundaryOuterLessInner = 0;
            }
            //if (mf.isMetric) mf.btnManualOffOn.Text = AreaBoundaryLessInnersHectares;
            //else mf.btnManualOffOn.Text = AreaBoundaryLessInnersAcres;
        }

        public String GetDescription()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Field: {0}", mf.displayFieldName);
            sb.AppendLine();
            sb.AppendFormat("Total Hectares: {0}", AreaBoundaryLessInnersHectares);
            sb.AppendLine();
            sb.AppendFormat("Worked Hectares: {0}", WorkedHectares);
            sb.AppendLine();
            sb.AppendFormat("Missing Hectares: {0}", WorkedAreaRemainHectares);
            sb.AppendLine();
            sb.AppendFormat("Total Acres: {0}", AreaBoundaryLessInnersAcres);
            sb.AppendLine();
            sb.AppendFormat("Worked Acres: {0}", WorkedAcres);
            sb.AppendLine();
            sb.AppendFormat("Missing Acres: {0}", WorkedAreaRemainAcres);
            sb.AppendLine();
            sb.AppendFormat("Tool Width: {0}", mf.tool.width);
            sb.AppendLine();
            sb.AppendFormat("Sections: {0}", mf.tool.numOfSections);
            sb.AppendLine();
            sb.AppendFormat("Section Overlap: {0}", mf.tool.overlap);
            sb.AppendLine();
            return sb.ToString();
        }
    }
}