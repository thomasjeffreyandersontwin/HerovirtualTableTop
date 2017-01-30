using System;
using Microsoft.Xna.Framework;

namespace HeroVirtualTableTop.Desktop

{
    public class PositionImpl : Position
    {
        
        public float Facing { get; set; }


        public float Pitch { get; set; }

        public float Roll { get; set; }

        public Vector3 Vector => new Vector3(X, Y, Z);

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

        public Position JustMissedPosition
        {
            get
            {
                Position missed = new PositionImpl();
                var rand = new Random();
                var randomOffset = rand.Next(2, 7);
                var multiplyOffset = rand.Next(11, 20);
                var multiplyFactorX = multiplyOffset % 2 == 0 ? 1 : -1;
                missed.X = X + randomOffset * multiplyFactorX;
                multiplyOffset = rand.Next(11, 20);
                var multiplyFactorY = multiplyOffset % 2 == 0 ? 1 : -1;
                missed.Y = Y + 5.0f + randomOffset * multiplyFactorY;
                multiplyOffset = rand.Next(11, 20);
                var multiplyFactorZ = multiplyOffset % 2 == 0 ? 1 : -1;
                missed.Z = Z + randomOffset * multiplyFactorZ;
                return missed;
            }

            set { }
        }


        public float DistanceFrom(Position targetPos)
        {
            var targetV = targetPos.Vector;
            return Vector3.Distance(Vector, targetV);
        }

        public void TurnTowards(Position position)
        {
            throw new NotImplementedException();
        }
    }
}