Prefabs

Add all prefabs from one of the folder to the scene: BuildTree, Crafting, Mission, TechTree and Mixed. 
Each folder contains a graph and an UI prefab. The graph prefab contains the graph logic 
and the UI visualize the graph in a certain way. Under the UI folder the UI item scripts are available, 
they are already linked in the UI prefabs in the listed folder. Do not add them to the project.
The EmptyGraph is an empty graph, which can be used to create the activation graph.
The GraphsManager contains a dictionary, from where the graphs can be get by name. If you 
need it, then add this prefab to your project.

The BuildTree prefabs also need the TimerManager prefab. TimerManager prefab contains the TimerManager
singleton class, which manages timer for animating and for other purposes.
