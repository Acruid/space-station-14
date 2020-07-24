using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture, Parallelizable, TestOf(typeof(CollisionSolver))]
    class CollisionSolverTests
    {
        [Test]
        public void CircleCircle_NotCollide()
        {
            var a = new Circle(new Vector2(0,0), 0.5f);
            var b = new Circle(new Vector2(1, 1), 0.5f);

            CollisionSolver.CalculateCollisionFeatures(in a, in b, out var results);

            Assert.AreEqual(false, results.Collided);
        }
        [Test]
        public void CircleCircle_Collide()
        {
            var a = new Circle(new Vector2(0, 0), 1f);
            var b = new Circle(new Vector2(1.5f, 0), 1f);

            CollisionSolver.CalculateCollisionFeatures(in a, in b, out var results);

            Assert.AreEqual(true, results.Collided);
            Assert.AreEqual(Vector2.UnitX, results.Normal);
            Assert.AreEqual(0.5f / 2, results.Penetration);
            Assert.IsNotNull(results.Contacts);
            Assert.AreEqual(1, results.Contacts.Length);
            Assert.AreEqual(new Vector2(0.5f, 0), results.Contacts[0]);
        }

        [Test]
        public void CircleCircle_SamePos()
        {
            var circle = new Circle(new Vector2(0, 0), 0.5f);

            CollisionSolver.CalculateCollisionFeatures(in circle, in circle, out var results);

            Assert.AreEqual(false, results.Collided);
        }
    }
}
