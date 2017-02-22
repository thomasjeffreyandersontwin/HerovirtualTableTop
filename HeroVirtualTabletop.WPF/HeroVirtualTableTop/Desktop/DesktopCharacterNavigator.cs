using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Ink;
using HeroVirtualTableTop.Movement;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Library.Utility;

namespace HeroVirtualTableTop.Desktop
{
    class DesktopNavigatorImpl : DesktopNavigator
    {
        public bool IsMovingToDestination { get; set; }
        public bool UsingGravity { get; set; }
        public float Speed { get; set; }
        public float Distance { get; set; }

        public DesktopMemoryCharacter MemoryInstance { get; set; }
        //public List<DesktopCharacterBodyPart> BodyParts { get; set; }

        public Position PositionBeingNavigated { get; set; }

        public Position StopPosition
        {
            get
            {
                if (Collision == Vector3.Zero)
                {
                    return Destination;
                }
                else
                {
                    return new PositionImpl(Collision);
                }
            }
        }
        public Position Destination { get; set; }     
        public Direction Direction { get; set; }
       

        public Desktop.Position OriginalDestination { get; set; }

        public bool WillCollide { get; }
        
        public Vector3 Collision
        {
            get
            {
                float distance = Vector3.Distance(PositionBeingNavigated.Vector, Destination.Vector);
                IconInteractionUtility.Start = PositionBeingNavigated.Vector;
                IconInteractionUtility.Destination = Destination.Vector;
                Vector3 collision = IconInteractionUtility.Collision;
                float collisionDistance = Vector3.Distance(PositionBeingNavigated.Vector, collision);
                if (collision.Length()!=0f && collisionDistance <= distance) // proper collision
                    return collision;            
                return new Vector3();            
            }
        }
        public void NavigateCollisionsToDestination(Position characterPosition, Direction direction, Position destination, float speed,
            bool hasGravity)
        {
            PositionBeingNavigated = characterPosition;
            Direction = direction;
            Destination = destination;
            Speed = speed;
            UsingGravity = hasGravity;
            Navigate();
        }
        private Position _allowableDestination;
        public Position NearestAvailableIncrementalPositionTowardsDestination
        {
            get
            {
                float distance = PositionBeingNavigated.DistanceFrom(StopPosition);
                if (distance < Speed)
                {
                    return StopPosition;
                }
                Vector3 destinationVectorNext = NearestIncrementalVectorTowardsDestination;                   
                if (_allowableDestination == null)
                {
                    _allowableDestination = new PositionImpl(destinationVectorNext);

                }
                else
                {
                    _allowableDestination.X = destinationVectorNext.X;
                    _allowableDestination.Y = destinationVectorNext.Y;
                    _allowableDestination.Z = destinationVectorNext.Z;
                }
                return _allowableDestination;
            }
        }
        public Vector3 NearestIncrementalVectorTowardsDestination
        {
            get
            {
                Vector3 vCurrent = PositionBeingNavigated.Vector;
                Vector3 directionVector = PositionBeingNavigated.FacingVector;
                directionVector.Normalize();
                var destX = vCurrent.X + directionVector.X * Speed;
                var destY = vCurrent.Y + directionVector.Y * Speed;
                var destZ = vCurrent.Z + directionVector.Z * Speed;
                Vector3 dest = new Vector3(destX, destY, destZ);
                dest = PositionBeingNavigated.GetRoundedVector(dest, 2);
                return dest;
            }
        }

        
        public void ApplyGravityToDestination()
        {
            UsingGravity = true;
        }


        public void Navigate()
          {
            float actual = 0f;
            if (PositionBeingNavigated.Equals(Destination))
                return;
            PositionBeingNavigated.Face(StopPosition);
            float calc = 0f;
            while (PositionBeingNavigated.Equals(StopPosition)==false)
            {
               PositionBeingNavigated.MoveTo(NearestAvailableIncrementalPositionTowardsDestination);
            }
        }



        public IconInteractionUtility IconInteractionUtility { get; set; }

        
    }


}
