using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;

namespace Mike.Graphics
{
    public class Camera
    {
        private Vector3 _mousePos;
        private const float SPEED_ROTATE = 0.005f;
        private const float SPEED_ZOOM = 0.25f;
        private const float SPEED_TRANSLATE = 0.0025f;

        public Camera(Size viewportSize, Vector3 camPosition, Vector3 targetPosition, Vector3 up)
        {
            ViewportSize = viewportSize;
            CamPosition = camPosition;
            TargetPosition = targetPosition;
            Up = up;

            _modelviewMatrix = Matrix4.LookAt(camPosition, targetPosition, up);
        }

        public bool Think(double deltaTime)
        {
            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();

            var newMouse = new Vector3(mouse.X, mouse.Y, mouse.WheelPrecise);
            var mouseDelta = _mousePos - newMouse;
            _mousePos = newMouse;

            var refresh = false;

            Matrix4 translation;
            Matrix4 rotation;
            if (Math.Abs(mouseDelta.Z) > 0.01f)
            {
                refresh = true;

                translation = Matrix4.CreateTranslation(-1 * _modelviewMatrix.Column2.Xyz * SPEED_ZOOM * mouseDelta.Z);
                Matrix4.Mult(ref translation, ref _modelviewMatrix, out _modelviewMatrix);
            }

            if (mouse[MouseButton.Left])
            {
                refresh = true;
                rotation = Matrix4.CreateRotationZ(-1 * SPEED_ROTATE * mouseDelta.X) * Matrix4.CreateFromAxisAngle(-1 * _modelviewMatrix.Column0.Xyz, SPEED_ROTATE * mouseDelta.Y);
                Matrix4.Mult(ref rotation, ref _modelviewMatrix, out _modelviewMatrix);
            }
            else if (mouse[MouseButton.Right])
            {
                refresh = true;

                var transformation = Matrix4.Invert(_modelviewMatrix);
                var position = transformation.Row3;
                transformation = transformation * Matrix4.CreateFromAxisAngle(_modelviewMatrix.Column0.Xyz, mouseDelta.Y * SPEED_ROTATE);
                transformation = transformation * Matrix4.CreateRotationZ(mouseDelta.X * SPEED_ROTATE);
                transformation.Row3 = position;

                _modelviewMatrix = Matrix4.Invert(transformation);
            }

            if (keyboard[Key.Left])
            {
                refresh = true;
                translation = Matrix4.CreateTranslation(1 * _modelviewMatrix.Column0.Xyz * SPEED_TRANSLATE * (float)deltaTime);
                Matrix4.Mult(ref translation, ref _modelviewMatrix, out _modelviewMatrix);
            }
            else if (keyboard[Key.Right])
            {
                refresh = true;
                translation = Matrix4.CreateTranslation(-1 * _modelviewMatrix.Column0.Xyz * SPEED_TRANSLATE * (float)deltaTime);
                Matrix4.Mult(ref translation, ref _modelviewMatrix, out _modelviewMatrix);
            }

            if (keyboard[Key.Up])
            {
                refresh = true;
                translation = Matrix4.CreateTranslation(-1 * _modelviewMatrix.Column1.Xyz * SPEED_TRANSLATE * (float)deltaTime);
                Matrix4.Mult(ref translation, ref _modelviewMatrix, out _modelviewMatrix);
            }
            else if (keyboard[Key.Down])
            {
                refresh = true;
                translation = Matrix4.CreateTranslation(1 * _modelviewMatrix.Column1.Xyz * SPEED_TRANSLATE * (float)deltaTime);
                Matrix4.Mult(ref translation, ref _modelviewMatrix, out _modelviewMatrix);
            }

            if (keyboard[Key.Space])
            {
                refresh = true;
                _modelviewMatrix = Matrix4.LookAt(CamPosition, TargetPosition, Up);
            }

            return refresh;
        }

        private Matrix4 _modelviewMatrix;

        private Size ViewportSize { get; }
        private Vector3 CamPosition { get; }
        private Vector3 TargetPosition { get; }
        private Vector3 Up { get; }
        public Matrix4 ProjectionMatrix
        {
            get
            {
                Matrix4 projectionMatrix;
                var aspectRatio = (float)ViewportSize.Width / ViewportSize.Height;
                Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), aspectRatio, 0.001f, 10000.0f, out projectionMatrix);
                return projectionMatrix;
            }
        }

        public Matrix4 ViewMatrix => _modelviewMatrix;
    }
}
