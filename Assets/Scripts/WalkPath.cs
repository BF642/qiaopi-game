using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    /// <summary>Click-time A* over walkable ground. Returns movement targets only.</summary>
    public static class WalkPath
    {
        private static int MinX,MaxX,MinZ,MaxZ,Width,Depth;
        private const int Mask = ~((1 << 2)|(1 << 30));
        private static Cell[] cachedGrid;
        private static string cachedWorld;
        public static void Invalidate(){cachedGrid=null;cachedWorld=null;}
        private const float Radius = .35f, Nearby = 1.5f;
        private static readonly int[] StepX = { -1, 0, 1, -1, 1, -1, 0, 1 };
        private static readonly int[] StepZ = { -1, -1, -1, 0, 0, 1, 1, 1 };

        private sealed class Cell
        {
            public bool open;
            public float ground;
        }

        public static List<Vector3> Find(Vector3 start, Vector3 goal, string world)
        {
            var empty = new List<Vector3>();
            if (!Finite(start) || !Finite(goal)) return empty;
            var bounds=WorldRegions.Get(world).bounds;
            MinX=Mathf.CeilToInt(bounds.xMin);MaxX=Mathf.FloorToInt(bounds.xMax-.01f);
            MinZ=Mathf.CeilToInt(bounds.yMin);MaxZ=Mathf.FloorToInt(bounds.yMax-.01f);
            Width=MaxX-MinX+1;Depth=MaxZ-MinZ+1;
            Physics.SyncTransforms();
            var grid = cachedWorld==world?cachedGrid:null;
            if(grid==null){
            grid=new Cell[Width*Depth];
            for (int z = MinZ; z <= MaxZ; z++)
                for (int x = MinX; x <= MaxX; x++)
                {
                    float ground;
                    bool open = Walkable(x, z, world, out ground);
                    grid[Index(x, z)] = new Cell { open = open, ground = ground };
                }
            cachedGrid=grid;cachedWorld=world;
            }

            // The character is not snapped onto the grid. Its first waypoint must
            // be close enough and connected by a collision-free capsule sweep.
            int startCell = -1;
            float closestStart = float.PositiveInfinity;
            for (int i = 0; i < grid.Length; i++)
            {
                if (!grid[i].open) continue;
                Vector3 p = Position(i, grid);
                float distance = FlatSquared(start, p);
                if (distance > Nearby * Nearby || distance >= closestStart || !ClearSegment(start, p)) continue;
                startCell = i;
                closestStart = distance;
            }
            if (startCell < 0) return empty;

            // Several nearby targets may straddle an obstacle. Try them in order
            // of distance, so a blocked side does not hide a reachable doorstep.
            var targets = new List<int>();
            for (int i = 0; i < grid.Length; i++)
                if (grid[i].open && FlatSquared(Position(i, grid), goal) <= Nearby * Nearby + .0001f)
                    targets.Add(i);
            targets.Sort((a, b) => FlatSquared(Position(a, grid), goal).CompareTo(FlatSquared(Position(b, grid), goal)));
            if (targets.Count == 0) return empty;

            float goalGround;
            bool exactGoalOpen = Walkable(goal.x, goal.z, world, out goalGround);
            foreach (int target in targets)
            {
                List<int> route = Search(grid, startCell, target);
                if (route.Count == 0) continue;
                if (exactGoalOpen && !ClearSegment(Position(target, grid), goal)) continue;
                var waypoints = new List<Vector3>(route.Count + 1);
                foreach (int cell in route)
                {
                    Vector3 point = Position(cell, grid);
                    if (waypoints.Count == 0 && FlatSquared(start, point) < .01f) continue;
                    waypoints.Add(point);
                }
                Vector3 last = Position(target, grid);
                if (exactGoalOpen && ClearSegment(last, goal))
                {
                    Vector3 endpoint = new Vector3(goal.x, goalGround, goal.z);
                    if (waypoints.Count == 0 || FlatSquared(waypoints[waypoints.Count - 1], endpoint) > .0001f)
                        waypoints.Add(endpoint);
                }
                else if (waypoints.Count == 0)
                    waypoints.Add(last);
                return waypoints;
            }
            return empty;
        }

        private static List<int> Search(Cell[] grid, int start, int goal)
        {
            int count = grid.Length;
            var cost = new float[count];
            var previous = new int[count];
            var visited = new bool[count];
            var queued = new bool[count];
            var open = new List<int>();
            for (int i = 0; i < count; i++) { cost[i] = float.PositiveInfinity; previous[i] = -1; }
            cost[start] = 0f; open.Add(start); queued[start] = true;
            while (open.Count > 0)
            {
                int bestAt = 0;
                float bestScore = cost[open[0]] + Heuristic(open[0], goal);
                for (int i = 1; i < open.Count; i++)
                {
                    float score = cost[open[i]] + Heuristic(open[i], goal);
                    if (score < bestScore) { bestAt = i; bestScore = score; }
                }
                int current = open[bestAt];
                open.RemoveAt(bestAt); queued[current] = false;
                if (current == goal)
                {
                    var route = new List<int>();
                    for (int node = goal; node >= 0; node = previous[node]) route.Add(node);
                    route.Reverse();
                    return route;
                }
                visited[current] = true;
                int x = X(current), z = Z(current);
                for (int d = 0; d < StepX.Length; d++)
                {
                    int nx = x + StepX[d], nz = z + StepZ[d];
                    if (nx < MinX || nx > MaxX || nz < MinZ || nz > MaxZ) continue;
                    int next = Index(nx, nz);
                    if (!grid[next].open || visited[next]) continue;
                    bool diagonal = StepX[d] != 0 && StepZ[d] != 0;
                    // Both adjacent cardinal cells must be clear: never cut a corner.
                    if (diagonal && (!grid[Index(nx, z)].open || !grid[Index(x, nz)].open)) continue;
                    if (Mathf.Abs(grid[next].ground - grid[current].ground) > .3f) continue;
                    // Grid nodes alone can miss a thin collider between two cells.
                    if (!ClearSegment(Position(current, grid), Position(next, grid))) continue;
                    float nextCost = cost[current] + (diagonal ? 1.41421356f : 1f);
                    if (nextCost >= cost[next]) continue;
                    cost[next] = nextCost; previous[next] = current;
                    if (!queued[next]) { queued[next] = true; open.Add(next); }
                }
            }
            return new List<int>();
        }

        private static bool Walkable(float x, float z, string world, out float ground)
        {
            ground = 0f;
            if (x < MinX || x > MaxX || z < MinZ || z > MaxZ) return false;
            if (world == "harbor" && z > 21.3f && Mathf.Abs(x) > 5.65f) return false;
            RaycastHit hit;
            if (!Physics.Raycast(new Vector3(x, 4f, z), Vector3.down, out hit, 8f, Mask, QueryTriggerInteraction.Ignore)) return false;
            if (hit.point.y > .5f || hit.normal.y < .65f) return false;
            ground = hit.point.y;
            return !Physics.CheckCapsule(new Vector3(x, .55f, z), new Vector3(x, 1.65f, z), Radius, Mask, QueryTriggerInteraction.Ignore);
        }

        private static bool ClearSegment(Vector3 from, Vector3 to)
        {
            Vector3 movement = to - from;
            movement.y = 0f;
            float distance = movement.magnitude;
            if (distance < .001f) return true;
            return !Physics.CapsuleCast(new Vector3(from.x, .55f, from.z), new Vector3(from.x, 1.65f, from.z),
                Radius, movement / distance, distance, Mask, QueryTriggerInteraction.Ignore);
        }

        private static int Index(int x, int z) { return (z - MinZ) * Width + x - MinX; }
        private static int X(int index) { return index % Width + MinX; }
        private static int Z(int index) { return index / Width + MinZ; }
        private static Vector3 Position(int index, Cell[] grid) { return new Vector3(X(index), grid[index].ground, Z(index)); }
        private static float FlatSquared(Vector3 a, Vector3 b) { float x = a.x - b.x, z = a.z - b.z; return x * x + z * z; }
        private static float Heuristic(int a, int b)
        {
            int x = Mathf.Abs(X(a) - X(b)), z = Mathf.Abs(Z(a) - Z(b));
            int diagonal = Mathf.Min(x, z);
            return Mathf.Max(x, z) + .41421356f * diagonal;
        }
        private static bool Finite(Vector3 p)
        {
            return !float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.y)
                && !float.IsNaN(p.z) && !float.IsInfinity(p.z);
        }
    }
}
