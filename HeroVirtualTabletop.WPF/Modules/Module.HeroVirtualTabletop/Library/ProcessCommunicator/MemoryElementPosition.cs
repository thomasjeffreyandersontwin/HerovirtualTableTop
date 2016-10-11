using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Library.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.ProcessCommunicator
{
    public class Position : MemoryInstance, IMemoryElementPosition
    {
        public Position(bool initFromCurrentTarget = true, uint targetPointer = 0) : base(initFromCurrentTarget, targetPointer) { }

        private float x, y, z = 0;

        public float X
        {
            get
            {
                if (this.IsReal)
                {
                    x = GetAttributeAsFloat(92);
                }
                return x;
            }
            set
            {
                x = value;
                if (this.IsReal)
                {
                    SetTargetAttribute(92, x);
                }
            }
        }

        public float Y
        {
            get
            {
                if (this.IsReal)
                {
                    y = GetAttributeAsFloat(96);
                }
                return y;
            }
            set
            {
                y = value;
                if (this.IsReal)
                {
                    SetTargetAttribute(96, y);
                }
            }
        }

        public float Z
        {
            get
            {
                if (this.IsReal)
                {
                    z = GetAttributeAsFloat(100);
                }
                return z;
            }
            set
            {
                z = value;
                if (this.IsReal)
                {
                    SetTargetAttribute(100, z);
                }
            }
        }

        private float[,] rotationMatrix = new float[3, 3];
        public float[,] RotationMatrix
        {
            get
            {
                if (this.IsReal)
                {
                    rotationMatrix = new float[3, 3] {
                        { GetAttributeAsFloat(56), GetAttributeAsFloat(60), GetAttributeAsFloat(64) },
                        { GetAttributeAsFloat(68), GetAttributeAsFloat(72), GetAttributeAsFloat(76) },
                        { GetAttributeAsFloat(80), GetAttributeAsFloat(84), GetAttributeAsFloat(88) }
                    };
                }
                return rotationMatrix;
            }
            set
            {
                rotationMatrix = value;
                if (this.IsReal)
                {
                    SetTargetAttribute(56, value[0, 0]);
                    SetTargetAttribute(60, value[0, 1]);
                    SetTargetAttribute(64, value[0, 2]);
                    SetTargetAttribute(68, value[1, 0]);
                    SetTargetAttribute(72, value[1, 1]);
                    SetTargetAttribute(76, value[1, 2]);
                    SetTargetAttribute(80, value[2, 0]);
                    SetTargetAttribute(84, value[2, 1]);
                    SetTargetAttribute(88, value[2, 2]);
                }
            }
        }

        public IMemoryElementPosition Clone(bool preserveTargetPointer = true, uint oldTargetPointer = 0)
        {
            Position clone = new Position(false);
            if (preserveTargetPointer)
            {
                clone.SetTargetPointerFromGameMemoryInstance(this);
            }
            else
            {
                clone.SetTargetPointer(oldTargetPointer);
            }
            clone.X = this.X;
            clone.Y = this.Y;
            clone.Z = this.Z;
            clone.RotationMatrix = this.RotationMatrix;

            return clone;
        }

        public bool IsWithin(float maxDistance, IMemoryElementPosition From, out float calculatedDistance)
        {
            bool isWithinDistance = false;
            Vector3 currentCameraPosition = new Vector3(X, Y, Z);
            Vector3 targetElementPosition = new Vector3(From.X, From.Y, From.Z);
            calculatedDistance = Vector3.Distance(currentCameraPosition, targetElementPosition);
            if ((calculatedDistance >= 0 && calculatedDistance <= maxDistance) || (calculatedDistance < 0 && calculatedDistance >= -maxDistance))
            {
                isWithinDistance = true;
            }
            if(calculatedDistance > 100)
            {

            }

            return isWithinDistance;
        }

        public Vector3 GetTargetInFacingDirection()
        {
            Vector3 facingDirection = GetFacingVector();
            Vector3 currentPos = GetPositionVector();
            // Calculate a point fairly distant along the facing direction
            var px = currentPos.X + facingDirection.X * 10000;
            var py = currentPos.Y + facingDirection.Y * 10000;
            var pz = currentPos.Z + facingDirection.Z * 10000;
            return new Vector3(px, py, pz);
        }

        public Vector3 GetFacingVector()
        {
            var facingX = GetAttributeAsFloat(80);
            var facingY = GetAttributeAsFloat(84);
            var facingZ = GetAttributeAsFloat(88);
            return new Vector3(facingX, facingY, facingZ);
        }

        public Vector3 GetPositionVector()
        {
            return new Vector3(X, Y, Z);
        }

        public void MoveTarget(Vector3 vTarget, float units)
        {
            Vector3 vCurrent = GetPositionVector();
            Vector3 directionVector = vTarget;
            directionVector.Normalize();
            var destX = vCurrent.X + directionVector.X * units;
            var destY = vCurrent.Y + directionVector.Y * units;
            var destZ = vCurrent.Z + directionVector.Z * units;
            //if (Math.Abs(vCurrent.X - destX) < 1)
            //    destX = vCurrent.X;
            //if (Math.Abs(vCurrent.Y - destY) < 1)
            //    destY = vCurrent.Y;
            //if (Math.Abs(vCurrent.Z - destZ) < 1)
            //    destZ = vCurrent.Z;

            X = destX;
            Y = destY;
            Z = destZ;

            //var collisionInfo = IconInteractionUtility.GetCollisionInfo(vTarget.X, vTarget.Y, vTarget.Z, destX, destY, destZ);
            //Vector3 targetPosition = GetCollisionPoint(collisionInfo);
            //if (targetPosition.X == 0 && targetPosition.Y == 0 && targetPosition.Z == 0)
            //{
            //    X = destX;
            //    Y = destY;
            //    Z = destZ;
            //}
            //else
            //{
            //    X = targetPosition.X;
            //    Y = targetPosition.Y;
            //    Z = targetPosition.Z;
            //}
        }

        public void SetTargetFacing(Vector3 facingDirectionVector)
        {
            Vector3 currentPositionVector = GetPositionVector();
            Matrix newRotationMatrix = Matrix.CreateLookAt(currentPositionVector, facingDirectionVector, Vector3.Up);
            SetTargetAttribute(56, -1 * newRotationMatrix.M11);
            SetTargetAttribute(64, newRotationMatrix.M13);
            SetTargetAttribute(80, newRotationMatrix.M31);
            SetTargetAttribute(88, -1 * newRotationMatrix.M33);
        }

        public Vector3 GetRotationVector(double rotaionAngle, double rotationAxisX = 0, double rotationAxisY = 1, double rotationAxisZ = 0)
        {
            double rotationAngleRadian = GetRadianAngle(rotaionAngle);
            double tr = 1 - Math.Sin(rotationAngleRadian);
            //a1 = (t(r) * X * X) + cos(r)
            var a1 = tr * rotationAxisX * rotationAxisX + Math.Cos(rotationAngleRadian);
            //a2 = (t(r) * X * Y) - (sin(r) * Z)
            var a2 = tr * rotationAxisX * rotationAxisY - Math.Sin(rotationAngleRadian) * rotationAxisZ;
            //a3 = (t(r) * X * Z) + (sin(r) * Y)
            var a3 = tr * rotationAxisX * rotationAxisZ + Math.Sin(rotationAngleRadian) * rotationAxisY;
            //b1 = (t(r) * X * Y) + (sin(r) * Z)
            var b1 = tr * rotationAxisX * rotationAxisY + Math.Sin(rotationAngleRadian) * rotationAxisZ;
            //b2 = (t(r) * Y * Y) + cos(r)
            var b2 = tr * rotationAxisY * rotationAxisY + Math.Cos(rotationAngleRadian);
            //b3 = (t(r) * Y * Z) - (sin(r) * X)
            var b3 = tr * rotationAxisY * rotationAxisZ - Math.Sin(rotationAngleRadian) * rotationAxisX;
            //c1 = (t(r) * X * Z) - (sin(r) * Y)
            var c1 = tr * rotationAxisX * rotationAxisZ - Math.Sin(rotationAngleRadian) * rotationAxisY;
            //c2 = (t(r) * Y * Z) + (sin(r) * X)
            var c2 = tr * rotationAxisY * rotationAxisZ + Math.Sin(rotationAngleRadian) * rotationAxisX;
            //c3 = (t(r) * Z * Z) + cos (r)
            var c3 = tr * rotationAxisZ * rotationAxisZ + Math.Cos(rotationAngleRadian);

            Vector3 facingVector = GetFacingVector();
            var vX = (float)(a1 * facingVector.X + a2 * facingVector.Y + a3 * facingVector.Z);
            var vY = (float)(b1 * facingVector.X + b2 * facingVector.Y + b3 * facingVector.Z);
            var vZ = (float)(c1 * facingVector.X + c2 * facingVector.Y + c3 * facingVector.Z);

            return new Vector3(vX, vY, vZ);
        }
        public double GetRadianAngle(double angle)
        {
            return (Math.PI / 180) * angle;
        }
        #region Equality Comparer and Operator Overloading
        public static bool operator ==(Position a, Position b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(Position a, Position b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is Position)
                return this == (Position)obj;
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }
}
