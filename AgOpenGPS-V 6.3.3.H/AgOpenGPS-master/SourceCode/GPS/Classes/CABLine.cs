﻿using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace AgOpenGPS
{
    public class CABLine
    {
        public double abFixHeadingDelta;
        public double abHeading, abLength;
        public double angVel;

        public bool isABValid, isLateralTriggered;

        //the current AB guidance line
        public vec3 currentLinePtA = new vec3(0.0, 0.0, 0.0);
        public vec3 currentLinePtB = new vec3(0.0, 1.0, 0.0);

        public double distanceFromCurrentLinePivot;
        public double distanceFromRefLine;

        //pure pursuit values
        public vec2 goalPointAB = new vec2(0, 0);

        //List of all available ABLines
        public CTrk refLine = new CTrk();
        public CTrk track = new CTrk();

        public double howManyPathsAway;

        public bool isMakingABLine;
        public bool isHeadingSameWay = true;

        //public bool isOnTramLine;
        //public int tramBasedOn;
        public double ppRadiusAB;

        public vec2 radiusPointAB = new vec2(0, 0);
        public double rEastAB, rNorthAB;

        public double snapDistance, lastSecond = 0;
        public double steerAngleAB;
        public int lineWidth;

        //design
        public vec2 desPtA = new vec2(0.2, 0.15);
        public vec2 desPtB = new vec2(0.3, 0.3);

        public vec2 desLineEndA = new vec2(0.0, 0.0);
        public vec2 desLineEndB = new vec2(999997, 1.0);

        public vec2 refNudgePtA = new vec2(1, 1), refNudgePtB = new vec2(2, 2);


        public double desHeading = 0;

        public string desName = "";

        //autosteer errors
        public double pivotDistanceError, pivotDistanceErrorLast, pivotDerivative, pivotDerivativeSmoothed;

        //derivative counters
        private int counter2;

        public double inty;
        public double steerAngleSmoothed, pivotErrorTotal;
        public double distSteerError, lastDistSteerError, derivativeDistError;

        //Color tramColor = Color.YellowGreen;
        public int tramPassEvery;

        //pointers to mainform controls
        private readonly FormGPS mf;

        public CABLine(FormGPS _f)
        {
            //constructor
            mf = _f;
            //isOnTramLine = true;
            lineWidth = Properties.Settings.Default.setDisplay_lineWidth;
            abLength = 2000;
        }

        private double shadowOffset = 0;
        private double widthMinusOverlap = 0;

        public void BuildCurrentABLineList(vec3 pivot)
        {
            double dx, dy;
            int idx = mf.trk.idx;
            double distAway;

            if (mf.trk.gArr.Count < mf.trk.idx || mf.trk.idx < 0) return;

            track = mf.trk.gArr[mf.trk.idx];

            abHeading = track.heading;

            track.endPtA.easting = track.ptA.easting - (Math.Sin(abHeading) * abLength);
            track.endPtA.northing = track.ptA.northing - (Math.Cos(abHeading) * abLength);

            track.endPtB.easting = track.ptB.easting + (Math.Sin(abHeading) * abLength);
            track.endPtB.northing = track.ptB.northing + (Math.Cos(abHeading) * abLength);

            refNudgePtA = track.endPtA; refNudgePtB = track.endPtB;

            //#######################################################################################
            string nam = track.name;
            if (nam.Length > 4 && nam.Substring(0, 5) == "&FIX ")
            {
                track.nudgeDistance = 0;
            }

            //move the origin ABLine 
            refNudgePtA.easting += (Math.Sin(abHeading + glm.PIBy2) * track.nudgeDistance);
            refNudgePtA.northing += (Math.Cos(abHeading + glm.PIBy2) * track.nudgeDistance);

            refNudgePtB.easting += (Math.Sin(abHeading + glm.PIBy2) * track.nudgeDistance);
            refNudgePtB.northing += (Math.Cos(abHeading + glm.PIBy2) * track.nudgeDistance);

            lastSecond = mf.secondsSinceStart;

            //move the ABLine over based on the overlap amount set in
            //x2-x1
            dx = refNudgePtB.easting - refNudgePtA.easting;
            //y2-y1
            dy = refNudgePtB.northing - refNudgePtA.northing;

            //move the ABLine over based on the overlap amount set in
            widthMinusOverlap = mf.tool.width - mf.tool.overlap;

            distanceFromRefLine = ((dy * mf.guidanceLookPos.easting) - (dx * mf.guidanceLookPos.northing) + (refNudgePtB.easting
                                        * refNudgePtA.northing) - (refNudgePtB.northing * refNudgePtA.easting))
                                            / Math.Sqrt((dy * dy) + (dx * dx));

            //#######################################################################################

            if (nam.Length > 4 && nam.Substring(0, 5) == "&FIX ")
            {
                //distanceFromRefLine = 0;
                mf.isSideGuideLines = false;
            }
            else
            {
                mf.isSideGuideLines = true;
                distanceFromRefLine -= (0.5 * widthMinusOverlap);
            }

            isLateralTriggered = false;

            isHeadingSameWay = Math.PI - Math.Abs(Math.Abs(pivot.heading - abHeading) - Math.PI) < glm.PIBy2;

            if (mf.yt.isYouTurnTriggered && !mf.yt.isGoingStraightThrough) isHeadingSameWay = !isHeadingSameWay;

            //Which ABLine is the vehicle on, negative is left and positive is right side
            double RefDist = (distanceFromRefLine + (isHeadingSameWay ? mf.tool.offset : -mf.tool.offset)) / widthMinusOverlap;
            if (RefDist < 0) howManyPathsAway = (int)(RefDist - 0.5);
            else howManyPathsAway = (int)(RefDist + 0.5);

            distAway = widthMinusOverlap * howManyPathsAway;
            distAway += (0.5 * widthMinusOverlap);

            shadowOffset = isHeadingSameWay ? mf.tool.offset : -mf.tool.offset;

            //move the curline as well. 
            vec2 nudgePtA = new vec2(track.ptA);
            vec2 nudgePtB = new vec2(track.ptB);


            //#######################################################################################
            if (nam.Length > 4 && nam.Substring(0, 5) == "&FIX ")
                distAway = 0;

            nudgePtA.easting += (Math.Sin(abHeading + glm.PIBy2) * track.nudgeDistance);
            nudgePtA.northing += (Math.Cos(abHeading + glm.PIBy2) * track.nudgeDistance);

            nudgePtB.easting += (Math.Sin(abHeading + glm.PIBy2) * track.nudgeDistance);
            nudgePtB.northing += (Math.Cos(abHeading + glm.PIBy2) * track.nudgeDistance);

            //depending which way you are going, the offset can be either side
            vec2 point1 = new vec2((Math.Cos(-abHeading) * (distAway + (isHeadingSameWay ? -mf.tool.offset : mf.tool.offset))) + nudgePtA.easting,
            (Math.Sin(-abHeading) * (distAway + (isHeadingSameWay ? -mf.tool.offset : mf.tool.offset))) + nudgePtA.northing);

            vec2 point2 = new vec2((Math.Cos(-abHeading) * (distAway + (isHeadingSameWay ? -mf.tool.offset : mf.tool.offset))) + nudgePtB.easting,
            (Math.Sin(-abHeading) * (distAway + (isHeadingSameWay ? -mf.tool.offset : mf.tool.offset))) + nudgePtB.northing);

            //create the new line extent points for current ABLine based on original heading of AB line
            currentLinePtA.easting = point1.easting - (Math.Sin(abHeading) * abLength);
            currentLinePtA.northing = point1.northing - (Math.Cos(abHeading) * abLength);

            currentLinePtB.easting = point2.easting + (Math.Sin(abHeading) * abLength);
            currentLinePtB.northing = point2.northing + (Math.Cos(abHeading) * abLength);

            currentLinePtA.heading = abHeading;
            currentLinePtB.heading = abHeading;
            isABValid = true;
        }

        public void GetCurrentABLine(vec3 pivot, vec3 steer)
        {
            double dx, dy;

            //Check uturn first
            if (mf.yt.isYouTurnTriggered && mf.yt.DistanceFromYouTurnLine())//do the pure pursuit from youTurn
            {
                //now substitute what it thinks are AB line values with auto turn values
                steerAngleAB = mf.yt.steerAngleYT;
                distanceFromCurrentLinePivot = mf.yt.distanceFromCurrentLine;

                goalPointAB = mf.yt.goalPointYT;
                radiusPointAB.easting = mf.yt.radiusPointYT.easting;
                radiusPointAB.northing = mf.yt.radiusPointYT.northing;
                ppRadiusAB = mf.yt.ppRadiusYT;

                mf.vehicle.modeTimeCounter = 0;
                mf.vehicle.modeActualXTE = (distanceFromCurrentLinePivot);
            }

            //Stanley
            else if (mf.isStanleyUsed)
                mf.gyd.StanleyGuidanceABLine(currentLinePtA, currentLinePtB, pivot, steer);

            //Pure Pursuit
            else
            {
                //get the distance from currently active AB line
                //x2-x1
                dx = currentLinePtB.easting - currentLinePtA.easting;
                //z2-z1
                dy = currentLinePtB.northing - currentLinePtA.northing;

                //save a copy of dx,dy in youTurn
                mf.yt.dxAB = dx; mf.yt.dyAB = dy;

                //how far from current AB Line is fix
                if (mf.vehicle.purePursuitIntegralGain > 16)
                    distanceFromCurrentLinePivot = ((dy * pivot.easting) - (dx * pivot.northing) + (currentLinePtB.easting
                                * currentLinePtA.northing) - (currentLinePtB.northing * currentLinePtA.easting))
                                / Math.Sqrt((dy * dy) + (dx * dx));
                else
                    distanceFromCurrentLinePivot = ((dy * steer.easting) - (dx * steer.northing) + (currentLinePtB.easting
                                 * currentLinePtA.northing) - (currentLinePtB.northing * currentLinePtA.easting))
                                 / Math.Sqrt((dy * dy) + (dx * dx));

                //integral slider is set to 0
                if (mf.vehicle.purePursuitIntegralGain != 0 && !mf.isReverse)
                {
                    pivotDistanceError = distanceFromCurrentLinePivot * 0.2 + pivotDistanceError * 0.8;

                    if (counter2++ > 4)
                    {
                        pivotDerivative = pivotDistanceError - pivotDistanceErrorLast;
                        pivotDistanceErrorLast = pivotDistanceError;
                        counter2 = 0;
                        pivotDerivative *= 2;

                        //limit the derivative
                        //if (pivotDerivative > 0.03) pivotDerivative = 0.03;
                        //if (pivotDerivative < -0.03) pivotDerivative = -0.03;
                        //if (Math.Abs(pivotDerivative) < 0.01) pivotDerivative = 0;
                    }

                    //pivotErrorTotal = pivotDistanceError + pivotDerivative;

                    if (mf.isBtnAutoSteerOn
                        && Math.Abs(pivotDerivative) < (0.1)
                        && mf.avgSpeed > 2.5
                        && !mf.yt.isYouTurnTriggered)
                    //&& Math.Abs(pivotDistanceError) < 0.2)

                    {
                        //if over the line heading wrong way, rapidly decrease integral
                        if ((inty < 0 && distanceFromCurrentLinePivot < 0) || (inty > 0 && distanceFromCurrentLinePivot > 0))
                        {
                            inty += pivotDistanceError * mf.vehicle.purePursuitIntegralGain * -0.04;
                        }
                        else
                        {
                            if (Math.Abs(distanceFromCurrentLinePivot) > 0.02)
                            {
                                inty += pivotDistanceError * mf.vehicle.purePursuitIntegralGain * -0.02;
                                if (inty > 0.2) inty = 0.2;
                                else if (inty < -0.2) inty = -0.2;
                            }
                        }
                    }
                    else inty *= 0.95;
                }
                else inty = 0;

                //Subtract the two headings, if > 1.57 its going the opposite heading as refAB
                abFixHeadingDelta = (Math.Abs(mf.fixHeading - abHeading));
                if (abFixHeadingDelta >= Math.PI) abFixHeadingDelta = Math.Abs(abFixHeadingDelta - glm.twoPI);

                // ** Pure pursuit ** - calc point on ABLine closest to current position
                double U = (((pivot.easting - currentLinePtA.easting) * dx)
                            + ((pivot.northing - currentLinePtA.northing) * dy))
                            / ((dx * dx) + (dy * dy));

                //point on AB line closest to pivot axle point
                rEastAB = currentLinePtA.easting + (U * dx);
                rNorthAB = currentLinePtA.northing + (U * dy);

                //update base on autosteer settings and distance from line
                double goalPointDistance = mf.vehicle.UpdateGoalPointDistance();

                if (mf.isReverse ? isHeadingSameWay : !isHeadingSameWay)
                {
                    goalPointAB.easting = rEastAB - (Math.Sin(abHeading) * goalPointDistance);
                    goalPointAB.northing = rNorthAB - (Math.Cos(abHeading) * goalPointDistance);
                }
                else
                {
                    goalPointAB.easting = rEastAB + (Math.Sin(abHeading) * goalPointDistance);
                    goalPointAB.northing = rNorthAB + (Math.Cos(abHeading) * goalPointDistance);
                }

                //calc "D" the distance from pivot axle to lookahead point
                double goalPointDistanceDSquared
                    = glm.DistanceSquared(goalPointAB.northing, goalPointAB.easting, pivot.northing, pivot.easting);

                //calculate the the new x in local coordinates and steering angle degrees based on wheelbase
                double localHeading;

                if (isHeadingSameWay) localHeading = glm.twoPI - mf.fixHeading + inty;
                else localHeading = glm.twoPI - mf.fixHeading - inty;

                ppRadiusAB = goalPointDistanceDSquared / (2 * (((goalPointAB.easting - pivot.easting) * Math.Cos(localHeading))
                    + ((goalPointAB.northing - pivot.northing) * Math.Sin(localHeading))));

                steerAngleAB = glm.toDegrees(Math.Atan(2 * (((goalPointAB.easting - pivot.easting) * Math.Cos(localHeading))
                    + ((goalPointAB.northing - pivot.northing) * Math.Sin(localHeading))) * mf.vehicle.wheelbase
                    / goalPointDistanceDSquared));

                if (mf.ahrs.imuRoll != 88888)
                    steerAngleAB += mf.ahrs.imuRoll * -mf.gyd.sideHillCompFactor;

                //steerAngleAB *= 1.4;

                if (steerAngleAB < -mf.vehicle.maxSteerAngle) steerAngleAB = -mf.vehicle.maxSteerAngle;
                if (steerAngleAB > mf.vehicle.maxSteerAngle) steerAngleAB = mf.vehicle.maxSteerAngle;

                //limit circle size for display purpose
                if (ppRadiusAB < -500) ppRadiusAB = -500;
                if (ppRadiusAB > 500) ppRadiusAB = 500;

                radiusPointAB.easting = pivot.easting + (ppRadiusAB * Math.Cos(localHeading));
                radiusPointAB.northing = pivot.northing + (ppRadiusAB * Math.Sin(localHeading));

                //if (mf.isConstantContourOn)
                //{
                //    //angular velocity in rads/sec  = 2PI * m/sec * radians/meters

                //    //clamp the steering angle to not exceed safe angular velocity
                //    if (Math.Abs(mf.setAngVel) > 1000)
                //    {
                //        //mf.setAngVel = mf.setAngVel < 0 ? -mf.vehicle.maxAngularVelocity : mf.vehicle.maxAngularVelocity;
                //        mf.setAngVel = mf.setAngVel < 0 ? -1000 : 1000;
                //    }
                //}

                //distance is negative if on left, positive if on right
                if (!isHeadingSameWay)
                    distanceFromCurrentLinePivot *= -1.0;

                //used for acquire/hold mode
                mf.vehicle.modeActualXTE = (distanceFromCurrentLinePivot);

                double steerHeadingError = (pivot.heading - abHeading);
                //Fix the circular error
                if (steerHeadingError > Math.PI)
                    steerHeadingError -= Math.PI;
                else if (steerHeadingError < -Math.PI)
                    steerHeadingError += Math.PI;

                if (steerHeadingError > glm.PIBy2)
                    steerHeadingError -= Math.PI;
                else if (steerHeadingError < -glm.PIBy2)
                    steerHeadingError += Math.PI;

                mf.vehicle.modeActualHeadingError = glm.toDegrees(steerHeadingError);

                //Convert to millimeters
                mf.guidanceLineDistanceOff = (short)Math.Round(distanceFromCurrentLinePivot * 1000.0, MidpointRounding.AwayFromZero);
                mf.guidanceLineSteerAngle = (short)(steerAngleAB * 100);
            }

            //mf.setAngVel = 0.277777 * mf.avgSpeed * (Math.Tan(glm.toRadians(steerAngleAB))) / mf.vehicle.wheelbase;
            //mf.setAngVel = glm.toDegrees(mf.setAngVel);
        }

        public void DrawABLineNew()
        {
            //ABLine currently being designed
            GL.LineWidth(lineWidth);
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(0.95f, 0.70f, 0.50f);
            GL.Vertex3(desLineEndA.easting, desLineEndA.northing, 0.0);
            GL.Vertex3(desLineEndB.easting, desLineEndB.northing, 0.0);
            GL.End();

            GL.Color3(0.2f, 0.950f, 0.20f);
            mf.font.DrawText3D(desPtA.easting, desPtA.northing, "&A");
            mf.font.DrawText3D(desPtB.easting, desPtB.northing, "&B");
        }

        public void DrawABLines()
        {

            double toolWidthn = mf.tool.width - mf.tool.overlap;
            double toolOffsetn = mf.tool.offset * 2 + toolWidthn * 0.4;
            int ToolNumber;

            if (!mf.isNewLineCurve)
            {
                //Draw AB Points  //draw original AB Line A and B 
                GL.PointSize(8.0f);
                GL.Begin(PrimitiveType.Points);

                GL.Color3(0.0f, 0.90f, 0.95f);
                GL.Vertex3(mf.trk.gArr[mf.trk.idx].ptB.easting, mf.trk.gArr[mf.trk.idx].ptB.northing, 0.0);
                GL.Color3(0.95f, 0.0f, 0.0f);
                GL.Vertex3(mf.trk.gArr[mf.trk.idx].ptA.easting, mf.trk.gArr[mf.trk.idx].ptA.northing, 0.0);
                //GL.Color3(0.00990f, 0.990f, 0.095f);
                //GL.Vertex3(mf.bnd.iE, mf.bnd.iN, 0.0);
                GL.End();

                if (mf.font.isFontOn && !isMakingABLine)
                {
                    mf.font.DrawText3D(mf.trk.gArr[mf.trk.idx].ptA.easting, mf.trk.gArr[mf.trk.idx].ptA.northing, "&A");
                    mf.font.DrawText3D(mf.trk.gArr[mf.trk.idx].ptB.easting, mf.trk.gArr[mf.trk.idx].ptB.northing, "&B");
                    double toolWidthline;
                    toolWidthline = toolWidthn;
                    //int HalfPointCount = (int)(curList.Count - 1) / 2;
                    if ((mf.isLineNumberon) && (mf.camera.camSetDistance > mf.tool.width * -120))
                    {
                        for (int toolsn = -mf.GuidanceLine9 + 1; toolsn < mf.GuidanceLine9; toolsn++) //curList[h].easting
                        {
                            double DistAB = glm.Distance(mf.trk.gArr[mf.trk.idx].ptA, mf.trk.gArr[mf.trk.idx].ptB) * 0.5;
                            toolWidthn = toolWidthline * toolsn;
                            ToolNumber = toolsn + (int)howManyPathsAway + 1;
                            if (ToolNumber <= 0) ToolNumber--;
                            string LiNu = ToolNumber.ToString();
                            mf.font.DrawText3D((Math.Cos(-mf.trk.gArr[mf.trk.idx].heading) * (toolWidthn + toolOffsetn) + Math.Cos(-mf.trk.gArr[mf.trk.idx].heading) * toolWidthline * howManyPathsAway) + mf.trk.gArr[mf.trk.idx].ptA.easting, (Math.Sin(-mf.trk.gArr[mf.trk.idx].heading) * (toolWidthn + toolOffsetn) + Math.Sin(-mf.trk.gArr[mf.trk.idx].heading) * toolWidthline * howManyPathsAway) + mf.trk.gArr[mf.trk.idx].ptA.northing, LiNu);
                            mf.font.DrawText3D((Math.Cos(-mf.trk.gArr[mf.trk.idx].heading) * (toolWidthn + toolOffsetn) + Math.Cos(-mf.trk.gArr[mf.trk.idx].heading) * toolWidthline * howManyPathsAway) + mf.trk.gArr[mf.trk.idx].ptA.easting - Math.Sin(-mf.trk.gArr[mf.trk.idx].heading) * DistAB, (Math.Sin(-mf.trk.gArr[mf.trk.idx].heading) * (toolWidthn + toolOffsetn) + Math.Sin(-mf.trk.gArr[mf.trk.idx].heading) * toolWidthline * howManyPathsAway) + mf.trk.gArr[mf.trk.idx].ptA.northing + Math.Cos(-mf.trk.gArr[mf.trk.idx].heading) * DistAB, LiNu);
                            mf.font.DrawText3D((Math.Cos(-mf.trk.gArr[mf.trk.idx].heading) * (toolWidthn + toolOffsetn) + Math.Cos(-mf.trk.gArr[mf.trk.idx].heading) * toolWidthline * howManyPathsAway) + mf.trk.gArr[mf.trk.idx].ptB.easting, (Math.Sin(-mf.trk.gArr[mf.trk.idx].heading) * (toolWidthn + toolOffsetn) + Math.Sin(-mf.trk.gArr[mf.trk.idx].heading) * toolWidthline * howManyPathsAway) + mf.trk.gArr[mf.trk.idx].ptB.northing, LiNu);
                        }
                    }
                }

                GL.PointSize(1.0f);

                //Draw original red AB line
                GL.LineWidth(4);
                GL.Enable(EnableCap.LineStipple);
                GL.LineStipple(1, 0x0F00);
                GL.Begin(PrimitiveType.Lines);
                GL.Color3(0.930f, 0.2f, 0.2f);
                GL.Vertex3(mf.trk.gArr[mf.trk.idx].endPtA.easting, mf.trk.gArr[mf.trk.idx].endPtA.northing, 0);
                GL.Vertex3(mf.trk.gArr[mf.trk.idx].endPtB.easting, mf.trk.gArr[mf.trk.idx].endPtB.northing, 0);
                GL.End();
                GL.Disable(EnableCap.LineStipple);

                double widthMinusOverlap = mf.tool.width - mf.tool.overlap;
                double shadowOffset = isHeadingSameWay ? mf.tool.offset : -mf.tool.offset;
                double sinHR = Math.Sin(abHeading + glm.PIBy2) * (widthMinusOverlap * 0.5 + shadowOffset);
                double cosHR = Math.Cos(abHeading + glm.PIBy2) * (widthMinusOverlap * 0.5 + shadowOffset);
                double sinHL = Math.Sin(abHeading + glm.PIBy2) * (widthMinusOverlap * 0.5 - shadowOffset);
                double cosHL = Math.Cos(abHeading + glm.PIBy2) * (widthMinusOverlap * 0.5 - shadowOffset);

                //shadow
                GL.Color4(0.5, 0.5, 0.5, 0.3);
                GL.Begin(PrimitiveType.TriangleFan);
                {
                    GL.Vertex3(currentLinePtA.easting - sinHL, currentLinePtA.northing - cosHL, 0);
                    GL.Vertex3(currentLinePtA.easting + sinHR, currentLinePtA.northing + cosHR, 0);
                    GL.Vertex3(currentLinePtB.easting + sinHR, currentLinePtB.northing + cosHR, 0);
                    GL.Vertex3(currentLinePtB.easting - sinHL, currentLinePtB.northing - cosHR, 0);
                }
                GL.End();

                //shadow lines
                GL.Color4(0.55, 0.55, 0.55, 0.3);
                GL.LineWidth(1);
                GL.Begin(PrimitiveType.LineLoop);
                {
                    GL.Vertex3(currentLinePtA.easting - sinHL, currentLinePtA.northing - cosHL, 0);
                    GL.Vertex3(currentLinePtA.easting + sinHR, currentLinePtA.northing + cosHR, 0);
                    GL.Vertex3(currentLinePtB.easting + sinHR, currentLinePtB.northing + cosHR, 0);
                    GL.Vertex3(currentLinePtB.easting - sinHL, currentLinePtB.northing - cosHR, 0);
                }
                GL.End();

                //draw current AB Line in use
                GL.LineWidth(lineWidth * 2);
                GL.Begin(PrimitiveType.Lines);
                GL.Color3(0.95f, 0.20f, 0.950f);
                GL.Vertex3(currentLinePtA.easting, currentLinePtA.northing, 0.0);
                GL.Vertex3(currentLinePtB.easting, currentLinePtB.northing, 0.0);

                GL.End();
                //GL.Color3(0, 1f, 0.2f);

                if ((mf.isSideGuideLines && mf.camera.camSetDistance > mf.tool.width * -120) || (mf.yt.isYouTurnBtnOn))
                {
                    //get the tool offset and width
                    double toolOffset = mf.tool.offset * 2;
                    double toolWidth = mf.tool.width - mf.tool.overlap;
                    double cosHeading = Math.Cos(-abHeading);
                    double sinHeading = Math.Sin(-abHeading);
                    int tools = 1;

                    GL.LineWidth(2);
                    GL.Enable(EnableCap.LineStipple);
                    GL.LineStipple(1, 0x0F00);
                    GL.Begin(PrimitiveType.Lines);
                    GL.Color3(1f, 2.7650f, 0.0050f);
                    if (mf.isSideGuideLines)
                    {
                        double toolWidthline = toolWidth;

                        if (isHeadingSameWay)
                        {
                            GL.Vertex3((cosHeading * (toolWidth + toolOffset)) + currentLinePtA.easting, (sinHeading * (toolWidth + toolOffset)) + currentLinePtA.northing, 0);
                            GL.Vertex3((cosHeading * (toolWidth + toolOffset)) + currentLinePtB.easting, (sinHeading * (toolWidth + toolOffset)) + currentLinePtB.northing, 0);
                            GL.Vertex3((cosHeading * (-toolWidth + toolOffset)) + currentLinePtA.easting, (sinHeading * (-toolWidth + toolOffset)) + currentLinePtA.northing, 0);
                            GL.Vertex3((cosHeading * (-toolWidth + toolOffset)) + currentLinePtB.easting, (sinHeading * (-toolWidth + toolOffset)) + currentLinePtB.northing, 0);

                            for (tools = 2; tools < mf.GuidanceLine9; tools++)
                            {
                                toolWidth = toolWidthline * tools;
                                GL.Vertex3((cosHeading * toolWidth) + currentLinePtA.easting, (sinHeading * toolWidth) + currentLinePtA.northing, 0);
                                GL.Vertex3((cosHeading * toolWidth) + currentLinePtB.easting, (sinHeading * toolWidth) + currentLinePtB.northing, 0);
                                GL.Vertex3((cosHeading * (-toolWidth)) + currentLinePtA.easting, (sinHeading * (-toolWidth)) + currentLinePtA.northing, 0);
                                GL.Vertex3((cosHeading * (-toolWidth)) + currentLinePtB.easting, (sinHeading * (-toolWidth)) + currentLinePtB.northing, 0);
                                if ((toolWidthline < 6510) && (toolWidth > 30000)) return;
                                if ((toolWidthline >= 6510) && (toolWidth > 60000)) return;
                                if (tools > 1) GL.Color3(0.5f, 0.7650f, 0.0050f);
                                if (tools > 3) GL.Color3(1.0f, 0.000f, 0.50f);
                                if (tools > 5) GL.Color3(0.0f, 2.7650f, 0.0050f);
                                if (tools > 7) GL.Color3(1.0f, 1.000f, 1.50f);
                            }
                        }
                        else
                        {
                            GL.Vertex3((cosHeading * (toolWidth - toolOffset)) + currentLinePtA.easting, (sinHeading * (toolWidth - toolOffset)) + currentLinePtA.northing, 0);
                            GL.Vertex3((cosHeading * (toolWidth - toolOffset)) + currentLinePtB.easting, (sinHeading * (toolWidth - toolOffset)) + currentLinePtB.northing, 0);
                            GL.Vertex3((cosHeading * (-toolWidth - toolOffset)) + currentLinePtA.easting, (sinHeading * (-toolWidth - toolOffset)) + currentLinePtA.northing, 0);
                            GL.Vertex3((cosHeading * (-toolWidth - toolOffset)) + currentLinePtB.easting, (sinHeading * (-toolWidth - toolOffset)) + currentLinePtB.northing, 0);

                            for (tools = 2; tools < mf.GuidanceLine9; tools++)
                            {
                                toolWidth = toolWidthline * tools;
                                GL.Vertex3((cosHeading * toolWidth) + currentLinePtA.easting, (sinHeading * toolWidth) + currentLinePtA.northing, 0);
                                GL.Vertex3((cosHeading * toolWidth) + currentLinePtB.easting, (sinHeading * toolWidth) + currentLinePtB.northing, 0);
                                GL.Vertex3((cosHeading * (-toolWidth)) + currentLinePtA.easting, (sinHeading * (-toolWidth)) + currentLinePtA.northing, 0);
                                GL.Vertex3((cosHeading * (-toolWidth)) + currentLinePtB.easting, (sinHeading * (-toolWidth)) + currentLinePtB.northing, 0);
                                if ((toolWidthline < 6510) && (toolWidth > 30000)) return;
                                if ((toolWidthline >= 6510) && (toolWidth > 60000)) return;
                                if (tools > 1) GL.Color3(0.5f, 0.7650f, 0.0050f);
                                if (tools > 3) GL.Color3(1.0f, 0.000f, 0.50f);
                                if (tools > 5) GL.Color3(0.0f, 2.7650f, 0.0050f);
                                if (tools > 7) GL.Color3(1.0f, 1.000f, 1.50f);
                            }
                        }

                    }
                    else
                    {
                        string nam = mf.trk.gArr[mf.trk.idx].name;

                        if (nam.Length > 0 && nam.Substring(0, 1) != "&")
                        {
                            if (isHeadingSameWay)
                            {
                                Uturnline_draw();
                                toolWidth *= mf.yt.rowSkipsWidth2;
                                if (!mf.yt.isYouTurnRightB)
                                {
                                    GL.Vertex3((cosHeading * toolWidth) + currentLinePtA.easting, (sinHeading * toolWidth) + currentLinePtA.northing, 0);
                                    GL.Vertex3((cosHeading * toolWidth) + currentLinePtB.easting, (sinHeading * toolWidth) + currentLinePtB.northing, 0);
                                }
                                else
                                {
                                    GL.Vertex3((cosHeading * (-toolWidth)) + currentLinePtA.easting, (sinHeading * (-toolWidth)) + currentLinePtA.northing, 0);
                                    GL.Vertex3((cosHeading * (-toolWidth)) + currentLinePtB.easting, (sinHeading * (-toolWidth)) + currentLinePtB.northing, 0);
                                }
                                Uturnline_nodraw();
                            }
                            else
                            {
                                Uturnline_draw();
                                toolWidth *= mf.yt.rowSkipsWidth1;
                                if (mf.yt.isYouTurnRightA)
                                {
                                    GL.Vertex3((cosHeading * toolWidth) + currentLinePtA.easting, (sinHeading * toolWidth) + currentLinePtA.northing, 0);
                                    GL.Vertex3((cosHeading * toolWidth) + currentLinePtB.easting, (sinHeading * toolWidth) + currentLinePtB.northing, 0);
                                }
                                else
                                {
                                    GL.Vertex3((cosHeading * (-toolWidth)) + currentLinePtA.easting, (sinHeading * (-toolWidth)) + currentLinePtA.northing, 0);
                                    GL.Vertex3((cosHeading * (-toolWidth)) + currentLinePtB.easting, (sinHeading * (-toolWidth)) + currentLinePtB.northing, 0);
                                }
                                Uturnline_nodraw();

                            }
                        }
                    }
                    GL.End();
                    GL.Disable(EnableCap.LineStipple);
                    GL.End();
                }

                if (!mf.isStanleyUsed && mf.camera.camSetDistance > -200)
                {
                    //Draw lookahead Point
                    GL.PointSize(8.0f);
                    GL.Begin(PrimitiveType.Points);
                    GL.Color3(1.0f, 1.0f, 0.0f);
                    GL.Vertex3(goalPointAB.easting, goalPointAB.northing, 0.0);
                    //GL.Vertex3(mf.gyd.rEastSteer, mf.gyd.rNorthSteer, 0.0);
                    //GL.Vertex3(mf.gyd.rEastPivot, mf.gyd.rNorthPivot, 0.0);
                    GL.End();
                    GL.PointSize(1.0f);

                    if (ppRadiusAB < 50 && ppRadiusAB > -50)
                    {
                        const int numSegments = 200;
                        double theta = glm.twoPI / numSegments;
                        double c = Math.Cos(theta);//precalculate the sine and cosine
                        double s = Math.Sin(theta);
                        //double x = ppRadiusAB;//we start at angle = 0
                        double x = 0;//we start at angle = 0
                        double y = 0;

                        GL.LineWidth(2);
                        GL.Color3(0.53f, 0.530f, 0.950f);
                        GL.Begin(PrimitiveType.Lines);
                        for (int ii = 0; ii < numSegments - 15; ii++)
                        {
                            //glVertex2f(x + cx, y + cy);//output vertex
                            GL.Vertex3(x + radiusPointAB.easting, y + radiusPointAB.northing, 0);//output vertex
                            double t = x;//apply the rotation matrix
                            x = (c * x) - (s * y);
                            y = (s * t) + (c * y);
                        }
                        GL.End();
                    }
                }
            }
            mf.yt.DrawYouTurn();

            GL.PointSize(1.0f);
            GL.LineWidth(1);
        }

        private void Uturnline_draw()
        {
            GL.End();
            GL.LineWidth(4);
            GL.Enable(EnableCap.LineStipple);
            GL.LineStipple(5, 0x9F20);
            GL.Begin(PrimitiveType.Lines);
        }
        private void Uturnline_nodraw()
        {
            GL.End();
            GL.LineWidth(4);
            GL.Enable(EnableCap.LineStipple);
            GL.LineStipple(1, 0x0F00);
            GL.Begin(PrimitiveType.Lines);
        }


        public void BuildTram()
        {
            if (mf.tram.generateMode != 1)
            {
                mf.tram.BuildTramBnd();
            }
            else
            {
                mf.tram.tramBndOuterArr?.Clear();
                mf.tram.tramBndInnerArr?.Clear();
            }

            mf.tram.tramList?.Clear();
            mf.tram.tramArr?.Clear();

            if (mf.tram.generateMode == 2) return;

            List<vec2> tramRef = new List<vec2>();

            bool isBndExist = mf.bnd.bndList.Count != 0;

            abHeading = mf.trk.gArr[mf.trk.idx].heading;

            double hsin = Math.Sin(abHeading);
            double hcos = Math.Cos(abHeading);

            double len = glm.Distance(mf.trk.gArr[mf.trk.idx].endPtA, mf.trk.gArr[mf.trk.idx].endPtB);
            //divide up the AB line into segments
            vec2 P1 = new vec2();
            for (int i = 0; i < (int)len; i += 4)
            {
                P1.easting = (hsin * i) + mf.trk.gArr[mf.trk.idx].endPtA.easting;
                P1.northing = (hcos * i) + mf.trk.gArr[mf.trk.idx].endPtA.northing;
                tramRef.Add(P1);
            }

            //create list of list of points of triangle strip of AB Highlight
            double headingCalc = abHeading + glm.PIBy2;

            hsin = Math.Sin(headingCalc);
            hcos = Math.Cos(headingCalc);

            mf.tram.tramList?.Clear();
            mf.tram.tramArr?.Clear();

            //no boundary starts on first pass
            int cntr = 0;
            if (isBndExist)
            {
                if (mf.tram.generateMode == 1)
                    cntr = 0;
                else
                    cntr = 1;
            }

            double widd;
            for (int i = cntr; i < mf.tram.passes; i++)
            {
                mf.tram.tramArr = new List<vec2>
                {
                    Capacity = 128
                };

                mf.tram.tramList.Add(mf.tram.tramArr);

                widd = (mf.tram.tramWidth * 0.5) - mf.tram.halfWheelTrack;
                widd += (mf.tram.tramWidth * i);

                for (int j = 0; j < tramRef.Count; j++)
                {
                    P1.easting = hsin * widd + tramRef[j].easting;
                    P1.northing = (hcos * widd) + tramRef[j].northing;

                    if (!isBndExist || mf.bnd.bndList[0].fenceLineEar.IsPointInPolygon(P1))
                    {
                        mf.tram.tramArr.Add(P1);
                    }
                }
            }

            for (int i = cntr; i < mf.tram.passes; i++)
            {
                mf.tram.tramArr = new List<vec2>
                {
                    Capacity = 128
                };

                mf.tram.tramList.Add(mf.tram.tramArr);

                widd = (mf.tram.tramWidth * 0.5) + mf.tram.halfWheelTrack;
                widd += (mf.tram.tramWidth * i);

                for (int j = 0; j < tramRef.Count; j++)
                {
                    P1.easting = (hsin * widd) + tramRef[j].easting;
                    P1.northing = (hcos * widd) + tramRef[j].northing;

                    if (!isBndExist || mf.bnd.bndList[0].fenceLineEar.IsPointInPolygon(P1))
                    {
                        mf.tram.tramArr.Add(P1);
                    }
                }
            }

            tramRef?.Clear();
            //outside tram

            if (mf.bnd.bndList.Count == 0 || mf.tram.passes != 0)
            {
                //return;
            }
        }

        public class CABLines
        {
            public vec2 origin = new vec2();
            public double heading = 0;
            public string Name = "aa";
        }
    }
}