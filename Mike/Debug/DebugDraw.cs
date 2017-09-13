using System;
using Color = OpenTK.Graphics.Color4;
using Point = OpenTK.Vector3;
using Vector = OpenTK.Vector3;
using Transform = OpenTK.Matrix4;

namespace Mike.Debug
{
    /// <summary>
    ///     Contains functions to draw debug primitives in the scene.
    /// </summary>
    public interface IDebugManager
    {
        /// <summary>
        ///     Adds a line segment to the debug drawing queue.
        /// </summary>
        void AddLine(Point from, Point to, Color color, float lineWidth = 1.0f, float duration = 0.0f, bool depthEnabled = true);

        /// <summary>
        ///     Adds an axis-aligned cross (3 lines converging at a point) to the debug drawing queue.
        /// </summary>
        void AddCross(Point center, Color color, float size, float duration = 0.0f, bool depthEnabled = true);

        /// <summary>
        ///     Adds a wireframe sphere to the debug drawing queue.
        /// </summary>
        void AddSphere(Point center, float radius, Color color, float duration = 0.0f, bool depthEnabled = true);

        /// <summary>
        ///     Adds a circle to the debug drawing queue.
        /// </summary>
        void AddCircle(Point center, Vector normal, float radius, Color color, float duration = 0.0f, bool depthEnabled = true);

        /// <summary>
        ///     Adds a set of coordinate axes depicting the position and orientation of the given transformation to the debug
        ///     drawing queue.
        /// </summary>
        void AddAxes(Transform xfm, Color color, float size, float duration = 0.0f, bool depthEnabled = true);

        /// <summary>
        ///     Adds a wireframe triangle to the debug drawing queue.
        /// </summary>
        void AddTriangle(Point vertex0, Point vertex1, Point vertex2, Color color, float lineWidth = 1.0f, float duration = 0.0f, bool depthEnabled = true);

        /// <summary>
        ///     Adds an axis-aligned bounding box to the debug queue.
        /// </summary>
        void AddAABB(Point minCoords, Point maxCoords, Color color, float lineWidth = 1.0f, float duration = 0.0f, bool depthEnabled = true);

        /// <summary>
        ///     Adds an oriented bounding box to the debug queue.
        /// </summary>
        void AddOBB(Transform xfm, Vector scaleXYZ, Color color, float lineWidth = 1.0f, float duration = 0.0f, bool depthEnabled = true);

        /// <summary>
        ///     Adds a text string to the debug drawing queue.
        /// </summary>
        void AddString(Point pos, string text, Color color, float duration = 0.0f, bool depthEnabled = true);
    }

    public class DebugManager : IDebugManager
    {
        public void AddLine(Point from, Point to, Color color, float lineWidth = 1, float duration = 0, bool depthEnabled = true)
        {
            throw new NotImplementedException();
        }

        public void AddCross(Point center, Color color, float size, float duration = 0, bool depthEnabled = true)
        {
            throw new NotImplementedException();
        }

        public void AddSphere(Point center, float radius, Color color, float duration = 0, bool depthEnabled = true)
        {
            throw new NotImplementedException();
        }

        public void AddCircle(Point center, Point normal, float radius, Color color, float duration = 0, bool depthEnabled = true)
        {
            throw new NotImplementedException();
        }

        public void AddAxes(Transform xfm, Color color, float size, float duration = 0, bool depthEnabled = true)
        {
            throw new NotImplementedException();
        }

        public void AddTriangle(Point vertex0, Point vertex1, Point vertex2, Color color, float lineWidth = 1, float duration = 0, bool depthEnabled = true)
        {
            throw new NotImplementedException();
        }

        public void AddAABB(Point minCoords, Point maxCoords, Color color, float lineWidth = 1, float duration = 0, bool depthEnabled = true)
        {
            throw new NotImplementedException();
        }

        public void AddOBB(Transform centerTransform, Point scaleXYZ, Color color, float lineWidth = 1, float duration = 0, bool depthEnabled = true)
        {
            throw new NotImplementedException();
        }

        public void AddString(Point pos, string text, Color color, float duration = 0, bool depthEnabled = true)
        {
            throw new NotImplementedException();
        }
    }
}
