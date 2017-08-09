﻿using System;
using OpenTK;

namespace SS14.Shared.Maths
{
    public enum Direction
    {
        East = 0,
        NorthEast = 1,
        North = 2,
        NorthWest = 3,
        West = 4,
        SouthWest = 5,
        South = 6,
        SouthEast = 7
    }

    /// <summary>
    /// Extension methods for Direction enum.
    /// </summary>
    public static class DirectionExtensions
    {
        private const double Segment = 2 * Math.PI / 8.0; // Cut the circle into 8 pieces
        private const double Offset = Segment / 2.0; // offset the pieces by 1/2 their size

        /// <summary>
        /// Converts a direction vector to the closest Direction enum.
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static Direction GetDir(this Vector2 vec)
        {
            var ang = ToAngle(vec);

            if (ang < 0.0f) // convert -PI > PI to 0 > 2PI
                ang += 2 * (float)Math.PI;

            return (Direction)Math.Floor((ang + Offset) / Segment);
        }

        /// <summary>
        /// Converts a direction to an angle, where angle is -PI to +PI.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static double ToAngle(this Direction dir)
        {
            var ang = Segment * (int)dir;

            if (ang > Math.PI) // convert 0 > 2PI to -PI > +PI
                ang -= 2 * Math.PI;

            return ang;
        }

        /// <summary>
        /// Converts a Direction to a normalized Direction vector.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Vector2 ToVec(this Direction dir)
        {
            return ToVec(ToAngle(dir));
        }

        /// <summary>
        /// Converts a direction vector to an angle, where angle is -PI to +PI.
        /// </summary>
        /// <param name="vec">Vector to get the angle from.</param>
        /// <returns>Angle of the vector.</returns>
        public static double ToAngle(this Vector2 vec)
        {
            return Math.Atan2(vec.Y, vec.X);
        }

        private static Vector2 ToVec(double rads)
        {
            var x = Math.Cos(rads);
            var y = Math.Sin(rads);
            return new Vector2((float)x, (float)y);
        }
    }
}
