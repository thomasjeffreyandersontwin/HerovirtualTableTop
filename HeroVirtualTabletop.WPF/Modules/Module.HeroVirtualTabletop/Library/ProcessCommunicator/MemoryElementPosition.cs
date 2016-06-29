using Microsoft.Xna.Framework;
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

            return isWithinDistance;
        }

        public Vector3 GetTargetInFacingDirection()
        {
            var facingX = GetAttributeAsFloat(80);
            var facingY = GetAttributeAsFloat(84);
            var facingZ = GetAttributeAsFloat(88);
            Vector3 facingDirection = new Vector3(facingX, facingY, facingZ);
            Vector3 currentPos = new Vector3(X, Y, Z);
            // Calculate a point fairly distant along the facing direction
            var px = currentPos.X + facingDirection.X * 10000;
            var py = currentPos.Y + facingDirection.Y * 10000;
            var pz = currentPos.Z + facingDirection.Z * 10000;
            return new Vector3(px, py, pz);
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
            return this == (Position)obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }
}
