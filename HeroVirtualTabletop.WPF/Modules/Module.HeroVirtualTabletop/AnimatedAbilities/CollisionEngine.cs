using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class CollisionEngine
    {
        public CollisionInfo FindObstructingObject(Character attacker, Character target, List<Character> otherCharacters)
        {
            return FindObstructingObject(attacker.CurrentPositionVector, target.CurrentPositionVector, otherCharacters);
        }

        private CollisionInfo FindObstructingObject(Vector3 sourcePositionVector, Vector3 targetPositionVector, List<Character> otherCharacters)
        {
            CollisionInfo nearestCollision = null;
            Vector3 sourceFacingTargetVector = targetPositionVector - sourcePositionVector;
            Vector3 targetFacingSourceVector = sourcePositionVector - targetPositionVector;
            if (sourceFacingTargetVector == targetFacingSourceVector)
                return null;
            if(sourceFacingTargetVector != Vector3.Zero)
                sourceFacingTargetVector.Normalize();
            if(targetFacingSourceVector != Vector3.Zero)
                targetFacingSourceVector.Normalize();
            // Calculate points A and B to the left and right of source
            Vector3 pointA = GetAdjacentPoint(sourcePositionVector, sourceFacingTargetVector, true);
            Vector3 pointB = GetAdjacentPoint(sourcePositionVector, sourceFacingTargetVector, false);
            // Calculate points C and D to left and right of target
            Vector3 pointC = GetAdjacentPoint(targetPositionVector, targetFacingSourceVector, true);
            Vector3 pointD = GetAdjacentPoint(targetPositionVector, targetFacingSourceVector, false);
            // Now we have four co-ordinates of rectangle ABCD.  Need to check if any of the other characters falls within this rectangular region
            List<Character> obstructingCharacters = new List<Characters.Character>();
            foreach (Character otherCharacter in otherCharacters)
            {
                if (IsPointWithinRegion(pointA, pointB, pointC, pointD, otherCharacter.CurrentPositionVector))
                {
                    obstructingCharacters.Add(otherCharacter);
                }
            }

            Dictionary<BodyPart, bool> bodyPartMap = new Dictionary<BodyPart, bool>();
            bodyPartMap.Add(BodyPart.BottomMiddle, true);
            bodyPartMap.Add(BodyPart.Middle, true);
            bodyPartMap.Add(BodyPart.TopMiddle, true);
            bodyPartMap.Add(BodyPart.Top, true);

            Dictionary<BodyPart, CollisionInfo> bodyPartCollisionMap = GetCollisionPointsForBodyParts(sourcePositionVector, targetPositionVector, bodyPartMap);
            bool hasCollision = false;
            foreach (BodyPart key in bodyPartCollisionMap.Keys)
            {
                if (bodyPartCollisionMap[key] != null)
                {
                    hasCollision = true;
                    break;
                }

            }
            float collisionDistance = 0f;
            if (hasCollision)
            {
                collisionDistance = bodyPartCollisionMap.Values.Min(d => d != null ? d.CollisionDistance : 10000f);
                nearestCollision = new CollisionInfo { CollidingObject = "WALL", CollisionDistance = collisionDistance};
            }
            else
                collisionDistance = 10000f;
            float minDistance = collisionDistance;
            foreach (Character obsChar in obstructingCharacters)
            {
                float obsDist = Vector3.Distance(sourcePositionVector, obsChar.CurrentPositionVector);
                if (obsDist < minDistance)
                {
                    minDistance = obsDist;
                    nearestCollision = new CollisionInfo { CollidingObject = obsChar, CollisionDistance = obsDist };
                }
            }

            return nearestCollision;
        }

        public CollisionInfo CalculateKnockbackObstruction(Character attacker, Character target, int distance, List<Character> otherCharacters)
        {
            if (target.CurrentPositionVector == attacker.CurrentPositionVector)
                return null;
            float knockbackDistance = distance * 8f + 5;
            Vector3 directionVector = target.CurrentPositionVector - attacker.CurrentPositionVector;
            directionVector.Normalize();
            var destX = target.CurrentPositionVector.X + directionVector.X * knockbackDistance;
            var destY = target.CurrentPositionVector.Y + directionVector.Y * knockbackDistance;
            var destZ = target.CurrentPositionVector.Z + directionVector.Z * knockbackDistance;
            Vector3 destVector = new Vector3(destX, destY, destZ);
            return FindObstructingObject(target.CurrentPositionVector, destVector, otherCharacters);
        }

        private Vector3 GetAdjacentPoint(Vector3 currentPositionVector, Vector3 facingVector, bool isLeft)
        {
            Double rotationAngle = isLeft ? -90 : 90;
            MovementDirection direction = isLeft ? MovementDirection.Left : MovementDirection.Right;
            Vector3 directionVector = GetDirectionVector(rotationAngle, direction, facingVector);
            Vector3 destinationVector = GetDestinationVector(directionVector, 5, currentPositionVector);
            return destinationVector;
        }

        private Vector3 GetDirectionVector(double rotationAngle, MovementDirection direction, Vector3 facingVector)
        {
            float vX, vY, vZ;
            double rotationAxisX = 0, rotationAxisY = 1, rotationAxisZ = 0;
            if (direction == MovementDirection.Upward)
            {
                vX = 0;
                vY = 1;
                vZ = 0;
            }
            else if (direction == MovementDirection.Downward)
            {
                vX = 0;
                vY = -1;
                vZ = 0;
            }
            else

            {
                double rotationAngleRadian = Helper.GetRadianAngle(rotationAngle);
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


                Vector3 facingVectorToDestination = facingVector;
                vX = (float)(a1 * facingVectorToDestination.X + a2 * facingVectorToDestination.Y + a3 * facingVectorToDestination.Z);
                vY = (float)(b1 * facingVectorToDestination.X + b2 * facingVectorToDestination.Y + b3 * facingVectorToDestination.Z);
                vZ = (float)(c1 * facingVectorToDestination.X + c2 * facingVectorToDestination.Y + c3 * facingVectorToDestination.Z);
            }

            return Helper.GetRoundedVector(new Vector3(vX, vY, vZ), 2);
        }

        private Vector3 GetDestinationVector(Vector3 directionVector, float units, Character target)
        {
            return GetDestinationVector(directionVector, units, target.CurrentPositionVector);
        }

        private Vector3 GetDestinationVector(Vector3 directionVector, float units, Vector3 positionVector)
        {
            Vector3 vCurrent = positionVector;
            directionVector.Normalize();
            var destX = vCurrent.X + directionVector.X * units;
            var destY = vCurrent.Y + directionVector.Y * units;
            var destZ = vCurrent.Z + directionVector.Z * units;
            Vector3 dest = new Vector3(destX, destY, destZ);
            dest = Helper.GetRoundedVector(dest, 2);
            return dest;
        }

        private bool IsPointWithinRegion(Vector3 pointA, Vector3 pointB, Vector3 pointC, Vector3 pointD, Vector3 pointX)
        {
            Vector3 lineAB = pointB - pointA;
            Vector3 lineAC = pointC - pointA;
            Vector3 lineAX = pointX - pointA;
            float AXdotAB = Vector3.Dot(lineAX, lineAB);
            float ABdotAB = Vector3.Dot(lineAB, lineAB);
            float AXdotAC = Vector3.Dot(lineAX, lineAC);
            float ACdotAC = Vector3.Dot(lineAC, lineAC);

            return (0 < AXdotAB && AXdotAB < ABdotAB) && (0 < AXdotAC && AXdotAC < ACdotAC);
        }

        private Vector3 GetCollisionVector(Vector3 sourceVector, Vector3 destVector)
        {
            float distance = Vector3.Distance(sourceVector, destVector);
            Vector3 collisionVector = new Vector3(0, 0, 0);
            int numRetry = 3; // try 3 times
            while (numRetry > 0)
            {
                try
                {
                    var collisionInfo = IconInteractionUtility.GetCollisionInfo(sourceVector.X, sourceVector.Y, sourceVector.Z, destVector.X, destVector.Y, destVector.Z);
                    collisionVector = Helper.GetCollisionVector(collisionInfo);
                    float collisionDistance = Vector3.Distance(sourceVector, collisionVector);
                    if (!HasCollision(collisionVector) || collisionDistance <= distance) // proper collision
                        break;
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(500);
                    numRetry--;
                }
            }
            if (float.IsNaN(collisionVector.X) || float.IsNaN(collisionVector.Y) || float.IsNaN(collisionVector.Z))
                collisionVector = new Vector3(0, 0, 0);
            return collisionVector;
        }

        private bool HasCollision(Vector3 collisionVector)
        {
            return !(collisionVector.X == 0f && collisionVector.Y == 0f && collisionVector.Z == 0f);
        }

        private Dictionary<BodyPart, CollisionInfo> GetCollisionPointsForBodyParts(Vector3 currentPositionVector, Vector3 destinationVector, Dictionary<BodyPart, bool> bodyPartMap)
        {
            Dictionary<BodyPart, CollisionInfo> bodyPartCollisionMap = new Dictionary<BodyPart, CollisionInfo>();
            bodyPartCollisionMap.Add(BodyPart.BottomMiddle, null);
            bodyPartCollisionMap.Add(BodyPart.Middle, null);
            bodyPartCollisionMap.Add(BodyPart.TopMiddle, null);
            bodyPartCollisionMap.Add(BodyPart.Top, null);

            Vector3 topOffsetVector = GetBodyPartOffsetVector(BodyPart.Top);
            Vector3 currentTopVector = new Vector3(currentPositionVector.X + topOffsetVector.X, currentPositionVector.Y + topOffsetVector.Y, currentPositionVector.Z + topOffsetVector.Z);
            Vector3 destinationTopVector = new Vector3(destinationVector.X + topOffsetVector.X, destinationVector.Y + topOffsetVector.Y, destinationVector.Z + topOffsetVector.Z);
            Vector3 collisionVectorTop = GetCollisionVector(currentTopVector, destinationTopVector);

            Thread.Sleep(5);

            Vector3 topMiddleOffsetVector = GetBodyPartOffsetVector(BodyPart.TopMiddle);
            Vector3 currentTopMiddleVector = new Vector3(currentPositionVector.X + topMiddleOffsetVector.X, currentPositionVector.Y + topMiddleOffsetVector.Y, currentPositionVector.Z + topMiddleOffsetVector.Z);
            Vector3 destinationTopMiddleVector = new Vector3(destinationVector.X + topMiddleOffsetVector.X, destinationVector.Y + topMiddleOffsetVector.Y, destinationVector.Z + topMiddleOffsetVector.Z);
            Vector3 collisionVectorTopMiddle = GetCollisionVector(currentTopMiddleVector, destinationTopMiddleVector);

            Thread.Sleep(5);

            Vector3 middleOffsetVector = GetBodyPartOffsetVector(BodyPart.Middle);
            Vector3 currentMiddleVector = new Vector3(currentPositionVector.X + middleOffsetVector.X, currentPositionVector.Y + middleOffsetVector.Y, currentPositionVector.Z + middleOffsetVector.Z);
            Vector3 destinationMiddleVector = new Vector3(destinationVector.X + middleOffsetVector.X, destinationVector.Y + middleOffsetVector.Y, destinationVector.Z + middleOffsetVector.Z);
            Vector3 collisionVectorMiddle = GetCollisionVector(currentMiddleVector, destinationMiddleVector);

            Thread.Sleep(5);

            Vector3 bottomMiddleOffsetVector = GetBodyPartOffsetVector(BodyPart.BottomMiddle);
            Vector3 currentBottomMiddleVector = new Vector3(currentPositionVector.X + bottomMiddleOffsetVector.X, currentPositionVector.Y + bottomMiddleOffsetVector.Y, currentPositionVector.Z + bottomMiddleOffsetVector.Z);
            Vector3 destinationBottomMiddleVector = new Vector3(destinationVector.X + bottomMiddleOffsetVector.X, destinationVector.Y + bottomMiddleOffsetVector.Y, destinationVector.Z + bottomMiddleOffsetVector.Z);
            Vector3 collisionVectorBottomMiddle = GetCollisionVector(currentBottomMiddleVector, destinationBottomMiddleVector);

            

            float distanceFromCollisionPoint = 10000f;
            
            if (HasCollision(collisionVectorBottomMiddle) && bodyPartMap[BodyPart.BottomMiddle])
            {
                float collDist = Vector3.Distance(currentBottomMiddleVector, collisionVectorBottomMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.BottomMiddle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = bottomMiddleOffsetVector,
                        CollisionBodyPart = BodyPart.BottomMiddle,
                        CollisionPoint = collisionVectorBottomMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.BottomMiddle] = false;
            }
            if (HasCollision(collisionVectorMiddle) && bodyPartMap[BodyPart.Middle])
            {
                float collDist = Vector3.Distance(currentMiddleVector, collisionVectorMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.Middle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = middleOffsetVector,
                        CollisionBodyPart = BodyPart.Middle,
                        CollisionPoint = collisionVectorMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.Middle] = false;
            }
            if (HasCollision(collisionVectorTopMiddle) && bodyPartMap[BodyPart.TopMiddle])
            {
                float collDist = Vector3.Distance(currentTopMiddleVector, collisionVectorTopMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.TopMiddle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = topMiddleOffsetVector,
                        CollisionBodyPart = BodyPart.TopMiddle,
                        CollisionPoint = collisionVectorTopMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.TopMiddle] = false;
            }
            if (HasCollision(collisionVectorTop) && bodyPartMap[BodyPart.Top])
            {
                float collDist = Vector3.Distance(currentTopVector, collisionVectorTop);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.Top] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = topOffsetVector,
                        CollisionBodyPart = BodyPart.Top,
                        CollisionPoint = collisionVectorTop,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.Top] = false;
            }

            return bodyPartCollisionMap;
        }

        private Vector3 GetBodyPartOffsetVector(BodyPart bodyPart)
        {
            Vector3 bodyPartOffsetVector = new Vector3(-10000, -10000, -10000);
            switch (bodyPart)
            {
                case BodyPart.Bottom:
                    bodyPartOffsetVector = new Vector3(0, 0, 0);
                    break;
                case BodyPart.BottomSemiMiddle:
                    bodyPartOffsetVector = new Vector3(0, 0.75f, 0);
                    break;
                case BodyPart.BottomMiddle:
                    bodyPartOffsetVector = new Vector3(0, 1.5f, 0);
                    break;
                case BodyPart.Middle:
                    bodyPartOffsetVector = new Vector3(0, 3, 0);
                    break;
                case BodyPart.TopMiddle:
                    bodyPartOffsetVector = new Vector3(0, 4.5f, 0);
                    break;
                case BodyPart.Top:
                    bodyPartOffsetVector = new Vector3(0, 6, 0);
                    break;
            }

            return bodyPartOffsetVector;
        }
    }
}
