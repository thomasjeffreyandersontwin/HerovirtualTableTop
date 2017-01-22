using System;
using Microsoft.Xna.Framework;

namespace HeroVirtualTableTop.Desktop

{
    public class PositionImpl : Position
    {
        public float Facing { get; set; }


        public float Pitch { get; set; }

        public float Roll { get; set; }

        private Vector3 _vector;
        public Vector3 Vector {
            get {
                if (_vector == null)
                {
                    _vector = new Vector3(X, Y, Z);
                }
                return _vector;
            } 
        }
        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public Position Duplicate()
        {
            throw new NotImplementedException();
        }

        public bool IsWithin(float dist, Position position, out float calculatedDistance)
        {
            throw new NotImplementedException();
        }

        public void MoveTo(Position destination)
        {
            throw new NotImplementedException();
        }

        public float DistanceFrom(Position targetPos)
        {
            Vector3 targetV = targetPos.Vector;
            return Vector3.Distance(Vector, targetV);
        }
    }


}