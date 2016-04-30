using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared.Models.ProcessCommunicator
{
    public class Position : MemoryInstance, IMemoryElementPosition
    {
        public Position(bool initFromCurrentTarget = true) : base(initFromCurrentTarget) { }

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

        public Position Clone(bool preserveTargetPointer = true, uint oldTargetPointer = 0)
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

        public bool IsWithin(float maxDistance, IMemoryElementPosition From)
        {
            bool isWithinDistance = false;
            Vector3 my_pos = new Vector3(X, Y, Z);
            Vector3 target_pos = new Vector3(From.X, From.Y, From.Z);
            float dist = Vector3.Distance(my_pos, target_pos);
            if ((dist >= 0 && dist <= maxDistance) || (dist < 0 && dist >= -maxDistance))
            {
                isWithinDistance = true;
            }

            return isWithinDistance;
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
