# Unity DOTS Node based Path Finding (Weights and Mask)

Unity DOTS node based path finding, using Eager Dijkstra modified Shortest Path algorithm
Please see Unity DOTS forum for discussions.
https://forum.unity.com/threads/dots-node-based-path-finding-eager-dijkstra.1064294/

(See also main branch)


## Scene

Scene represents some terrain with elevations and path nodes.
Instead of mesh path, project utilizes nodes, to generate neighbour network of nodes, with possible routes.
These become static, as current system do not allow for changing this network at runtime.

* ~~ (Weights and Mask) ~~ Optionally each node can have set range (PathNodeLinkRangeComponent), in which will detect other path nodes, and weights (difficulty) with mask (PathNodeMaskWeightsBuffer). See path node scene inspector for more details. If weighs are used, mask must be set accordingly in path planner entity (PathPlannerWeightsMaskComponent).

Further path planner entities allow for searching best path.
Setting are set for 100 entities by default, in OrderNewPathSystem.cs.
Tested with 10k path planner entities. But be advised to comment out debugging raycast, which is in PathFindingSystem.cs.

![Watch the video](https://forum.unity.com/attachments/upload_2021-2-25_2-2-2-png.802490/)


## Generation

At the scene initialization. all nodes are tested against each other, with grouping per elevation. For example ground path nodes are separate, from upper levels. Raycast from each node in same group are cast to each next neighbor node, on same the level. 
This is represented by gray color ray lines at initialization. 
Red lines indicate, that path to next node have been obstructed by wall, or ramp. 
Green lines inform of correct nodes links. 
Distances as weights are stored, with relation between nodes.

* ~~ (Weights and Mask) ~~ If range is set (greater than 0), range will be used. Otherwise each node on the same elevation will be checked. Links will be created accordingly. Different elevation checks is ignored by range.

Regarding elevation, like ram, nodes need to be marked manually, via inspector, with which other node link is allowed. That should be done in most cases for both up and down nodes.

That relation is added to network of nodes.

![Watch the video](https://forum.unity.com/attachments/upload_2021-2-25_2-2-21-png.802493/)


## Path Finding

When starting and ending points are selected, path planner entity is marked as ready for path finding. 
Path Finding System job is executed once, and debugging rays are rendered. 
White lines indicate tested near routes. 
Green lines mark best possible route.

* ~~ (Weights and Mask) ~~ If weights with mask are set and its buffer size is greater than 0, path planner entity will be testing own mask, against each node mask. With middle mouse click can cycle through path planners, which each can have optionally own weigh mask set.


## Controls

* Left click on the node in the game view, is starting point
* Right click is the end point of the path.
* ~~ (Weights and Mask) ~~ Middle click, toggle through path planner entities. Be aware, in the example script, if there is a lot entities, it may take many clicks to cycle them through.

More will come soon.


## Support

If you appricate my work, please star my repo. Many thanks :)
