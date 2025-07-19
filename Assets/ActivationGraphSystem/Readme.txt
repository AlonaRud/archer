Activation Graph System verion 1.41

Add a graph prefab (under prefab folder the EmptyGraph prefab) to the scene e.g. EmptyGraph or open an example scene. 
After this you can call in the “Window” menu the “Activation Graph System”. The graph editor window opens. 
By right click in the node editor window (left window) a context menu opens, where you have the controls for adding 
and removing nodes and more.

Controls are also binded to keyboard keys: 
• Delete: remove node 
• d: remove node 
• a: add task node 
• o: add operator node 
• w: add Won/Victory node 
• f: add failure node 
• t: add timer conditon node 
• u: add user condition node 
• e: add arrival condition node 
• r: add defeat condition node 
• g: add survive condition node 

After you added some nodes, you also want to connect them with each other. Hold the left control (for OSX 'X') key down 
for success connection or alt (for OSX 'C') for failure connection and click on a node with the mouse, then drag and drop 
the connection to the another node. If you want to remove a connection, then make the same with the control 
(not alt) key again. If the node is outside the visible screen area at connecting the nodes, then move the mouse to the side if the 
window to activate the side scrolling. If you don't have the middle mouse button, then click on left control (for OSX 'X')
and click on the side scrolling area to scroll the view.

You can build up the logical behavior as a graph with lot of possibilities, that satisfy a lot of systems like tech tree,
research tree, build tree, crafting system, skill system, mission system and so on. The UI has the task to show the 
result of the graph and to control it by activating conditions. In the example scene you can see how simple the UI is,
because all the ligic are in the graph.

I'm a game developer, not an asset developer, so I use my assests in my own game (RTS) for mission system, build tree, 
tech tree, research tree and more. If you have any trouble with the asset, then post it in the asset forum or
write an e-mail to support@metadesc.com.

Online Manual and API Documentation:

http://www.metadesc.com/studio/index.php/activation-graph-system.html
