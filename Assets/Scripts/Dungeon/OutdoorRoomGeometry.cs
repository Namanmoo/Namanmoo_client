using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Dungeon
{
    public static class OutdoorRoomGeometry
    {
        public const float GroundSize = 64f;
        public const float SafetyPadding = 3f;

        public static Rect GroundBounds(RoomShape shape)
        {
            Vector2 halfSize = Vector2.one * (GroundSize * 0.5f);
            return Rect.MinMaxRect(
                shape.Bounds.center.x - halfSize.x,
                shape.Bounds.center.y - halfSize.y,
                shape.Bounds.center.x + halfSize.x,
                shape.Bounds.center.y + halfSize.y);
        }

        public static Rect SafetyBounds(RoomShape shape)
        {
            Rect bounds = shape.Bounds;
            return Rect.MinMaxRect(
                bounds.xMin - SafetyPadding,
                bounds.yMin - SafetyPadding,
                bounds.xMax + SafetyPadding,
                bounds.yMax + SafetyPadding);
        }

        public static IReadOnlyList<Vector2> SafetyBoundary(RoomShape shape)
        {
            Rect bounds = SafetyBounds(shape);
            return new[]
            {
                new Vector2(bounds.xMin, bounds.yMin),
                new Vector2(bounds.xMin, bounds.yMax),
                new Vector2(bounds.xMax, bounds.yMax),
                new Vector2(bounds.xMax, bounds.yMin),
                new Vector2(bounds.xMin, bounds.yMin)
            };
        }
    }
}
