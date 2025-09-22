## Create a simple pathfinding demo:
- the map should consist of square tiles that are either traversable (0), obstacle (1) or a cover (2) and create an orthogonally connected grid (i.e. each inner tile has 4 neighbours, edge tile has 3 neighbours and corner tile has 2 neighbours). you can move from each tile in one of 4 directions (N, S, E, W) unless you're on an edge of the map or there's an obstacle blocking your way. each move between neighbouring tiles has the same "cost" (1).
- map should contain a player unit and an enemy unit. They can only be placed on traversable tile.
- unit may only move through traversable tile and attack through traversable and cover tiles.
-player unit has `MoveRange` and `AttackRange` parameters.
-player unit may only move to other tile if move path to that tile is shorter than `MoveRange` and attack enemy unit only if attack path to enemy is shorter than `AttackRange`.
-to display units use default Unity character model
- the goal of the exercise is to use a pathfinding algorithm and architecture optimally suited for the task. please explain what algorithm you've chosen and why? if you've made any adjustments to its standard implementation also explain what and why?

## The end user should be able to:
- adjust the size of the map (either via a config file or during runtime - i.e. in an edit mode of the demo)
- adjust the placement of obstacles and units and player unit parameters(preferably during runtime)
- select traversable tile on the map and be shown a optimal path between player unit and it. If tile contains enemy show attack path instead. If path is too long (out of `MoveRange` or `AttackRange`) show full path and indicate the "out of range" segment
- freely look around the map

## For Extra Credit:
-if possible move path is selected add option to move unit to destination on click. make the default Unity character model run the path on the map
-if possible attack path is selected add option to destroy enemy on click. If selecting tile with enemy unit and it is out of attack range, show path to closest tile from which unit can attack and an attack path from that tile. If both path are possible click should move unit to attack position and then enemy should be destroyed.


Please submit a working executable and the source project.
