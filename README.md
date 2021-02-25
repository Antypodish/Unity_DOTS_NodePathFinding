# Unity_DOTS_NodePathFinding

Unity DOTS node based path finding, using Eager Dijkstra modified Shortest Path algorithm


## Scene

Scene represents some terrain with elevations and path nodes.
Instead of mesh path, project utilises nodes, to generate neighbour network of nodes, with possible routes.
These become static, as current system do not allow for changing this network.

Further path planner entities allow for searching best path.
Setting are set for 100 entities by defualt, in OrderNewPathSystem.cs.
Tested with 10k path planner entities. But advise to comment out debugging raycast, which is in PathFindingSystem.


## Generation

At the scene initialization. all nodes are tested against each other, with grouping per elevation.
For example ground path nodes are seprate, from upper levels.
Raycast from each node in same group are casted to each next neighbour node, on same the level.
This is represented by gray color ray lines at initialization.
Red lines indicate, that path to next node have been obstructed by wall, or ramp.
Green lines inform of correct nodes links.
Distances as weights are stored, with relation between nodes.

Regarding elevation, like ram, nodes need to be marked manually, via inspector, with which other node link is allowed.
That should be done in most cases for both upp and down nodes.

That relation is added to network of nodes.


## Path Finding

When starting and ending points are selected, path planner entity is marked as ready for path finding.
Path Finding System job is executed once, and debugging rays are rendered.
White lines indicate tested near routes.
Green lines marke best possible route.


## Controls

Only left and right clicks.
Left click on the node in the game view, is starting point, right click is the end point of the path.

More will come soon.
