using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.SceneManagement;
using System;

namespace ActivationGraphSystem {
	/// <summary>
	/// The node editor for the task system. The editor searches for the first ActivationGraphManager in the scene.
	/// Under the ActivationGraphManager game object are then the task and condition nodes. 
	/// These nodes will be vizualized in this node editor.
	/// Just one ActivationGraphManager is allowed in a scene.
	/// For implementing a new node in this complex editor, the best way is to follow a similar node imlementation
	/// and implement it in a similar way. I would not recommend to change anything else in this class without 
	/// deeper knowledge or experience in Unity editor scripting.
	/// </summary>
	public class ActivationGraphEditor : EditorWindow {

		// For side scrolling.
		private bool doDebugSideScrolling = false;
		private float sideScrolldist = 25f;
		private float scrollSpeedFactor = 0.2f;

		bool showBigDescriptionFields = false;

		static ActivationGraphEditor window;
		// The inspector width.
		private int inspectorWidth = 400;
		// The scroll position of the inspector.
		private Vector2 scrollPosInspector;
		// State for UI control.
		private enum ControlState {
		None,
		DrawTransition,
		DragWindow}
;

		private ControlState controlState = ControlState.None;
        
		private Texture2D Logo;
		private Texture2D Box;
		private Texture2D BoxSuccess;
		private Texture2D BoxSuccess2;
		private Texture2D EditorBG;
		private Texture2D SelectionBG;
		private Texture2D LineTexture;
		// Own GUI skin, exists in Resources folder.
		private GUISkin guiSkin;

		// All nodes, which must be painted.
		private List<NodeBase> nodes = new List<NodeBase>();
		ActivationGraphManager[] AGManagers;
		ActivationGraphManager CurrentAGM;
		// To get the editor node for a BaseNode.
		Dictionary<BaseNode, NodeBase> missNodeDict = new Dictionary<BaseNode, NodeBase>();

		private bool mouseLeftDownNow = false;
		private bool mouseLeftUpNow = false;
		private bool leftControlDown = false;
		private bool leftAltDown = false;
		private bool mouseMidDown = false;
		private bool mouseLeftDown = false;
		// The nodes can be placed form 0 to 20000 pixels.
		// The editor starts in the middle of these borders.
		private float panX = -10000;
		private float panY = -10000;

		private Vector2 currentPadRootMousePos = Vector2.zero;
		private bool inEditorWindowFocus = true;

		// The inspector window on the right side.
		private Rect inspectorRect {
			get {
				return new Rect(position.width - inspectorWidth, 0, inspectorWidth, position.height);
			}
		}

		// The main window with nodes.
		private Rect mainRect {
			get {
				return new Rect(0, 0, position.width - inspectorWidth, position.height);
			}
		}

		// Needed to lost focus for textareas, if the mouse is not over them.
		List<Rect> currentTextFields = new List<Rect>();

		// Colors for components.
		private Color32 MissN = new Color32(0, 100, 0, 255);
		private Color32 MissA = new Color32(0, 180, 0, 255);
		private Color32 CondN = new Color32(0, 75, 100, 255);
		private Color32 CondA = new Color32(0, 150, 210, 255);
		private Color32 WinN = new Color32(150, 140, 0, 255);
		private Color32 WinA = new Color32(190, 180, 0, 255);
		private Color32 FailN = new Color32(100, 20, 0, 220);
		private Color32 FailA = new Color32(210, 30, 0, 220);

		private Color32 YN = new Color32(160, 120, 0, 255);
		private Color32 YA = new Color32(250, 120, 0, 255);
		private Color32 GN = new Color32(0, 100, 0, 255);
		private Color32 GA = new Color32(0, 180, 0, 255);
		private Color32 GA2 = new Color32(0, 255, 0, 255);
		private Color32 RN = new Color32(120, 0, 0, 255);
		private Color32 RA = new Color32(255, 0, 0, 255);
		private Color32 RA2 = new Color32(0, 240, 240, 255);

		private Color32 TC = new Color32(30, 30, 30, 255);

		private Color32 TNC = new Color32(200, 230, 200, 255);
		private Color32 TMNC = new Color32(255, 230, 255, 255);
		private Color32 TSDC = new Color32(255, 230, 220, 255);
		private Color32 TDC = new Color32(200, 240, 255, 255);
		private Color32 TSUC = new Color32(240, 240, 170, 255);
		private Color32 TFAC = new Color32(250, 170, 170, 255);
		private Color32 ANDC = new Color32(250, 220, 0, 255);
		private Color32 TIMEC = new Color32(100, 255, 100, 255);

		private Color32 LOC = new Color32(220, 180, 120, 255);
		private Color32 LBC = new Color32(150, 180, 220, 255);

		private Color32 CoordC = new Color32(220, 140, 80, 255);

		private Color32 ContTextColor = new Color32(140, 100, 50, 255);
		private Color32 ContManTextColor = new Color32(220, 140, 80, 255);

		private Color32 AGMPopupTextColor = new Color32(220, 180, 120, 255);

		private Color32 SelectionColor = new Color32(255, 255, 255, 255);
		private Color32 SelectionColorTransparent = new Color32(255, 255, 255, 0);

		private Color32 TaskBg = new Color32(240, 240, 240, 255);
		private Color32 OpBg = new Color32(215, 200, 150, 255);
		private Color32 SuccBg = new Color32(160, 215, 180, 255);
		private Color32 FailBg = new Color32(240, 180, 170, 255);
		private Color32 CondBg = new Color32(180, 210, 230, 255);
		private Color32 CMBg = new Color32(200, 170, 150, 255);
		private Color32 ContBg = new Color32(200, 170, 150, 255);

		// In play mode or not.
		private bool isPlaying = false;

		// Index of the currently selected graph manager. Needed at switching the graph in the running mode.
		static int lastManagerIndex = 0;

		// The currently selected node.
		NodeBase selectedNode = null;
		// The currently selected node.
		NodeBase selectedFirstConnNode = null;
		// the current mouse position.
		Vector2 currentMousePos = Vector2.zero;

		// In this case OnHierarchyChange creates this window.
		static bool startUpUnity = false;

		// Rect for selection.
		Rect selectionRect = new Rect();

		/// <summary>
		/// Unity starts up, an opened graph editor must be updated.
		/// </summary>
		private void OnHierarchyChange() {
			if (startUpUnity) {
				ShowEditor();
			}
		}

		[MenuItem("Window/Activation Graph Editor")]
		static void ShowEditor() {
			window = (ActivationGraphEditor)EditorWindow.GetWindow(typeof(ActivationGraphEditor));
			window.titleContent.text = "Graph Editor";
			window.minSize = new Vector2(600, 300);

			Undo.undoRedoPerformed += window.ShowNodes;
			EditorApplication.playmodeStateChanged += window.OnPlayModeChanged;

			window.Reinitialize();
			startUpUnity = false;
		}

		/// <summary>
		/// At playmode the scene objects are not the same, as in the non play mode.
		/// The node editor must be reinitialized.
		/// </summary>
		private void OnPlayModeChanged() {
			if (EditorApplication.isPlayingOrWillChangePlaymode) {
				Reinitialize();
				isPlaying = true;
			} else {
				Reinitialize();
				isPlaying = false;
			}
		}

		/// <summary>
		/// Also the callback must be rebuilt at changing the non play and play mode.
		/// </summary>
		private void OnEnable() {
			// This happens when Unity calls at start up OnEnable before the scene exists.
			if (AGManagers == null || AGManagers.Length == 0 || CurrentAGM == null || window == null) {
				startUpUnity = true;
				return;
			}
			Undo.undoRedoPerformed += ShowNodes;
			EditorApplication.playmodeStateChanged += OnPlayModeChanged;
			// After the compile and link procedure the editor window must be updated.
			if (!EditorApplication.isPlayingOrWillChangePlaymode) {
				Reinitialize();
			}
		}

		/// <summary>
		/// The inspector triggers the refresh update, about 10 time in a second.
		/// Update() would called much more often. It is a kind of optimization.
		/// </summary>
		private void OnInspectorUpdate() {
			Repaint();
		}

		private void setState(ControlState state) {
			controlState = state;
		}

		/// <summary>
		/// Initialize resources.
		/// </summary>
		private bool init() {
			LineTexture = AssetDatabase.LoadAssetAtPath("Assets/ActivationGraphSystem/Resources/LineTexture.png", typeof(Texture2D)) as Texture2D;
			Logo = AssetDatabase.LoadAssetAtPath("Assets/ActivationGraphSystem/Resources/metadesc_logo_32.png", typeof(Texture2D)) as Texture2D;
			EditorBG = AssetDatabase.LoadAssetAtPath("Assets/ActivationGraphSystem/Resources/EditorBackground.png", typeof(Texture2D)) as Texture2D;
			SelectionBG = AssetDatabase.LoadAssetAtPath("Assets/ActivationGraphSystem/Resources/EditorBackground.png", typeof(Texture2D)) as Texture2D;
			SelectionBG.wrapMode = TextureWrapMode.Repeat;
			Box = AssetDatabase.LoadAssetAtPath("Assets/ActivationGraphSystem/Resources/Box.png", typeof(Texture2D)) as Texture2D;
			BoxSuccess = AssetDatabase.LoadAssetAtPath("Assets/ActivationGraphSystem/Resources/BoxSuccess.png", typeof(Texture2D)) as Texture2D;
			BoxSuccess2 = AssetDatabase.LoadAssetAtPath("Assets/ActivationGraphSystem/Resources/BoxSuccess2.png", typeof(Texture2D)) as Texture2D;
			EditorBG.wrapMode = TextureWrapMode.Repeat;

			guiSkin = AssetDatabase.LoadAssetAtPath("Assets/ActivationGraphSystem/Resources/GUISkin.guiskin", typeof(GUISkin)) as GUISkin;

			oldDraggedWindowPos = Vector2.zero;

			return refreshManagers();
		}

		private bool refreshManagers() {
			AGManagers = FindObjectsOfType<ActivationGraphManager>();
			if (AGManagers.Length == 0) {
				return false;
			}

			if (lastManagerIndex < AGManagers.Length)
				CurrentAGM = AGManagers[lastManagerIndex];

			if (CurrentAGM == null)
				CurrentAGM = AGManagers[0];

			panX = -CurrentAGM.PosInNodeEditor.x;
			panY = -CurrentAGM.PosInNodeEditor.y;
			return true;
		}

		private void Reinitialize() {
			ShowNodes();
		}

		private void ShowNodes() {

			// Clear selection, else it would be asynchron.
			foreach (NodeBase node in selectedNodes) {
				node.IsSelected = false;
			}
			selectedNodes.Clear();

			if (init() == false) {
				// At starting Unity this will be also called before the scene exists.
				// In this case we cannot find any ActivationGraphManagers.
				return;
			}

			if (AGManagers.Length == 0) {
				Debug.LogError("No ActivationGraphManager instance is available in the scene. Is it maybe disabled?");
				window.Close();
				return;
			} else if (CurrentAGM == null) {
				if (lastManagerIndex < AGManagers.Length)
					CurrentAGM = AGManagers[lastManagerIndex];

				if (CurrentAGM == null)
					CurrentAGM = AGManagers[0];
			}

			if (CurrentAGM == null) {
				Debug.LogError("No ActivationGraphManager instance is available in the scene. Is it maybe disabled?");
				return;
			}

			panX = -CurrentAGM.PosInNodeEditor.x;
			panY = -CurrentAGM.PosInNodeEditor.y;

			BaseNode[] tasks = CurrentAGM.transform.GetComponentsInChildren<BaseNode>();

			missNodeDict.Clear();
			nodes.Clear();

			foreach (BaseNode miss in tasks) {
				AddNode(miss);
			}
			Repaint();
		}

		/// <summary>
		/// Add a node for the node editor. For initialization at the beginning.
		/// </summary>
		/// <param name="baseNode"></param>
		private void AddNode(BaseNode baseNode) {
			if (baseNode.IsHidden)
				return;

			NodeBase node = CreateInstance<NodeBase>();

			if (baseNode is OperatorNode) {
				node.Width = 50;
				node.height = 50;
			} else if (baseNode is TaskDataNode) {
				node.Width = 90;
				node.height = 60;
			} else if (baseNode is ConditionTimer) {
				node.Width = 80;
				node.height = 80;
			} else if (baseNode is ConditionBase) {
				node.Width = 80;
				node.height = 60;
			} else if (baseNode is ContainerManager) {
				node.Width = 80;
				node.height = 80;
			} else if (baseNode is Container) {
				node.Width = 80;
				node.height = 66;
			} else if (baseNode is SuccessEnd || baseNode is FailureEnd) {
				node.Width = 80;
				node.height = 50;
			} else {
				node.Width = 80;
				node.height = 80;
			}

			node.NodeRect = new Rect(baseNode.PosInNodeEditor.x, baseNode.PosInNodeEditor.y, node.Width, node.height);
			node.TheBaseNode = baseNode;
			node.TheEditor = this;

			nodes.Add(node);
			missNodeDict.Add(baseNode, node);
		}

		/// <summary>
		/// For adding new nodes dynamically in the editor.
		/// </summary>
		/// <param name="baseNode"></param>
		/// <param name="pos"></param>
		private NodeBase AddNode(BaseNode baseNode, Vector2 pos) {
			NodeBase node = CreateInstance<NodeBase>();

			if (baseNode is OperatorNode) {
				node.Width = 50;
				node.height = 50;
			} else if (baseNode is TaskDataNode) {
				node.Width = 90;
				node.height = 60;
			} else if (baseNode is ConditionTimer) {
				node.Width = 80;
				node.height = 80;
			} else if (baseNode is ConditionBase) {
				node.Width = 80;
				node.height = 60;
			} else if (baseNode is ContainerManager) {
				node.Width = 80;
				node.height = 80;
			} else if (baseNode is Container) {
				node.Width = 80;
				node.height = 66;
			} else if (baseNode is SuccessEnd || baseNode is FailureEnd) {
				node.Width = 80;
				node.height = 50;
			} else {
				node.Width = 80;
				node.height = 80;
			}

			// Set the default type for the new node.
			baseNode.Type = CurrentAGM.DefaultType;

			node.NodeRect = new Rect(pos.x - node.Width / 2, pos.y - node.height / 2, node.Width, node.height);
			node.TheBaseNode = baseNode;
			node.TheEditor = this;

			nodes.Add(node);
			missNodeDict.Add(baseNode, node);

			return node;
		}

		private void RemoveNode(BaseNode baseNode) {
			nodes.Remove(missNodeDict[baseNode]);
			missNodeDict.Remove(baseNode);

			if (baseNode is TaskBase) {
				TaskBase task = baseNode as TaskBase;
				if (CurrentAGM.CurrentTasks.Contains(task)) {
					Undo.RecordObject(CurrentAGM, "Remove node");
					CurrentAGM.CurrentTasks.Remove(task);
					if (task is TaskDataNode) {
						TaskDataNode dataTask = (TaskDataNode)task;
						if (CurrentAGM.Tasks.Contains(dataTask)) {
							CurrentAGM.Tasks.Remove(dataTask);
						}
					}
					EditorUtility.SetDirty(CurrentAGM);
				}
			}

			Undo.DestroyObjectImmediate(baseNode.gameObject);
		}

		private void OnSelectionChange() {
			ShowNodes();
		}

		private void OnFocus() {
			// If the window is in a tab, but at playing the scene it is hidden.
			// After selecting the window in the running scene, the window is null.
			// Hidden windows won't be updated after running the scene, so it must 
			// be created after selecting this window.
			if (window == null) {
				ShowEditor();
			}
			// We can change now the keyboard focus to 0
			inEditorWindowFocus = true;
		}

		/// <summary>
		/// If we are outside of the editor window, this is the case at 
		/// selecting a gameobject for the triggers, then the keyboard control
		/// must not be set to 0! Else, the game object wouldn't be set.
		/// </summary>
		private void OnLostFocus() {
			// Stop changing the keyboard focus to 0!
			inEditorWindowFocus = false;
		}

		List<NodeBase> selectedNodes = new List<NodeBase>();
		bool mousePressed = false;
		bool mouseRightNowUp = false;
        
		Dictionary<NodeBase, Vector3> startDrags = new Dictionary<NodeBase, Vector3>();

		Vector2 oldDraggedWindowPos = Vector2.zero;
		NodeBase oldSelectedNode = null;

		/// <summary>
		/// Paint here.
		/// </summary>
		private void OnGUI() {
			// This happens when Unity calls at start up OnEnable before the scene exists.
			if (window == null) {
				return;
			}

			// Draw logo.
			GUI.DrawTexture(new Rect(5, 2, Logo.width, Logo.height), Logo);


			Rect popupRect = new Rect(mainRect.width - 204, 25, 200, 20);
			Rect selPopupRect = new Rect(mainRect.width - 204, 75, 200, 20);

			// If the scene changes.
			if (CurrentAGM == null) {
				ShowNodes();
			}

			// Wait until the user selects a new manager.
			if (CurrentAGM != null) {

				GUI.skin = guiSkin;

				Event currentEvent = Event.current;
				currentMousePos = currentEvent.mousePosition;

				// Reset values.
				mouseLeftDownNow = false;
				mouseLeftUpNow = false;
				mouseRightNowUp = false;

				if (mainRect.Contains(currentMousePos) && !popupRect.Contains(currentMousePos) && !selPopupRect.Contains(currentMousePos)) {
					if (inEditorWindowFocus && !NodeAtPos(currentMousePos)) {
						GUIUtility.keyboardControl = 0;
					}

					int controlID = GUIUtility.GetControlID(FocusType.Passive);
					switch(Event.current.GetTypeForControl(controlID)) {
					case EventType.MouseDown:
                        // If you have a rectangle for your control then you should
                        // perform bounds check here...
						mousePressed = true;

						if (Event.current.button == 0) {
							mouseLeftDown = true;
							mouseLeftDownNow = true;

							if (GUIUtility.hotControl == controlID) {
								Event.current.Use();

								currentMousePos = currentEvent.mousePosition;
							}

							if (mouseLeftDownNow && !NodeAtPos(currentMousePos)) {
								selectionRect.position = currentMousePos;
								GUIUtility.keyboardControl = 0;
							}

						} else if (Event.current.button == 1) {
							GUIUtility.hotControl = 0;
							Event.current.Use();

						} else if (Event.current.button == 2) {
							currentPadRootMousePos = currentMousePos;
							GUIUtility.hotControl = controlID;
							Event.current.Use();

							mouseMidDown = true;
						}

						break;
					case EventType.MouseMove:
						if (GUIUtility.hotControl == controlID) {
							Event.current.Use();
							//currentMousePos = currentEvent.mousePosition;

						} else {
							Event.current.Use();

							currentMousePos = currentEvent.mousePosition;
							if (mouseLeftDownNow && selectionRect.position != Vector2.zero) {
								selectionRect.size = currentMousePos - selectionRect.position;
							}
						}
						break;

					case EventType.MouseDrag:
                            // Only update mouse position on move or drag.
						if (GUIUtility.hotControl == controlID) {
							Event.current.Use();

							currentMousePos = currentEvent.mousePosition;

							if (mouseMidDown) {
								panX += currentMousePos.x - currentPadRootMousePos.x;
								panY += currentMousePos.y - currentPadRootMousePos.y;
								currentPadRootMousePos = currentMousePos;
							}
						}
						break;

					case EventType.MouseUp:
						mousePressed = false;
						mouseRightNowUp = true;
						mouseLeftUpNow = true;

                            // If this control is currently active.
						if (GUIUtility.hotControl == controlID) {
							GUIUtility.hotControl = 0;
							Event.current.Use();

							currentMousePos = currentEvent.mousePosition;

							// Connect
							if (mouseMidDown == false && leftControlDown == true) {
								NodeBase curNode = NodeAtPos(currentMousePos);
								if (curNode && selectedFirstConnNode && selectedFirstConnNode.TheBaseNode is BaseNode) {

									if (curNode.TheBaseNode is TaskBase) {
										TaskBase currTask = curNode.TheBaseNode as TaskBase;

										Task selectedTask = null;
										if (selectedFirstConnNode.TheBaseNode is BaseNode)
											selectedTask = selectedFirstConnNode.TheBaseNode as Task;

										// Source is task node
										if (curNode && selectedTask && !selectedTask.TasksActivatedAfterSuccess.Contains(currTask)
										                                      && !selectedTask.TasksActivatedAfterFailed.Contains(currTask)) {

											// Store the state of the object, if there will be changes
											Undo.RecordObject(selectedTask, "Create connection");
											Undo.RecordObject(currTask, "Create connection");

											if (currTask is SuccessEnd)
												selectedTask.AddSuccess(currTask);
											else if (currTask is FailureEnd)
												selectedTask.AddFailed(currTask);
											else
												selectedTask.AddSuccess(currTask);

											EditorUtility.SetDirty(currTask);
											EditorUtility.SetDirty(selectedTask);

										} else if (curNode && selectedTask && (selectedTask.TasksActivatedAfterSuccess.Contains(currTask)
										                                             || selectedTask.TasksActivatedAfterFailed.Contains(currTask))) {
											// Disconnect

											// Store the state of the object, if there will be changes
											Undo.RecordObject(selectedTask, "Create connection");
											Undo.RecordObject(currTask, "Create connection");

											if (currTask is SuccessEnd)
												selectedTask.Remove(currTask);
											else if (currTask is FailureEnd)
												selectedTask.Remove(currTask);
											else
												selectedTask.Remove(currTask);
											EditorUtility.SetDirty(selectedTask);
											EditorUtility.SetDirty(currTask);
										}

										// Source is condition node
										if (curNode && selectedFirstConnNode.TheBaseNode is ConditionBase) {
											ConditionBase selectedCond = selectedFirstConnNode.TheBaseNode as ConditionBase;

											if (curNode.TheBaseNode is Task) {
												Task currentTask = curNode.TheBaseNode as Task;

												if (curNode && selectedFirstConnNode && !currentTask.Conditions.Contains(selectedCond)) {

													// Store the state of the object, if there will be changes
													Undo.RecordObject(currentTask, "Create condition connection");
													EditorUtility.SetDirty(currentTask);

													currentTask.Add(selectedCond);

												} else if (curNode && selectedFirstConnNode && currentTask.Conditions.Contains(selectedCond)) {

													// Store the state of the object, if there will be changes
													Undo.RecordObject(currentTask, "Create condition connection");
													EditorUtility.SetDirty(currentTask);

													currentTask.Remove(selectedCond);
												}
											}
										}

									} else if (curNode.TheBaseNode is ConditionBase) {
										ConditionBase currentCond = curNode.TheBaseNode as ConditionBase;

										Task selectedTask = null;
										if (selectedFirstConnNode.TheBaseNode is BaseNode)
											selectedTask = selectedFirstConnNode.TheBaseNode as Task;

										if (curNode && selectedTask && !selectedTask.Conditions.Contains(currentCond)) {

											// Store the state of the object, if there will be changes
											Undo.RecordObject(selectedTask, "Create condition connection");

											selectedTask.Add(currentCond);
											EditorUtility.SetDirty(selectedTask);

										} else if (curNode && selectedTask && selectedTask.Conditions.Contains(currentCond)) {

											// Store the state of the object, if there will be changes
											Undo.RecordObject(selectedTask, "Create condition connection");

											selectedTask.Remove(currentCond);
											EditorUtility.SetDirty(selectedTask);
										}
									}

								}

							} else if (mouseMidDown == false && leftAltDown == true) {

								NodeBase curNode = NodeAtPos(currentMousePos);
								if (curNode && selectedFirstConnNode && selectedFirstConnNode.TheBaseNode is Task) {
									Task selectedTask = selectedFirstConnNode.TheBaseNode as Task;


									if (curNode.TheBaseNode is TaskBase) {
										TaskBase currentTask = curNode.TheBaseNode as TaskBase;

										if (curNode && selectedFirstConnNode && !selectedTask.TasksActivatedAfterFailed.Contains(currentTask)
										                                      && !selectedTask.TasksActivatedAfterSuccess.Contains(currentTask)) {

											// Store the state of the object, if there will be changes
											Undo.RecordObject(selectedTask, "Create connection");
											Undo.RecordObject(currentTask, "Create connection");

											selectedTask.AddFailed(currentTask);
											EditorUtility.SetDirty(selectedTask);
											EditorUtility.SetDirty(currentTask);

										} else if (curNode && selectedFirstConnNode && (selectedTask.TasksActivatedAfterFailed.Contains(currentTask)
										                                             || selectedTask.TasksActivatedAfterSuccess.Contains(currentTask))) {

											// Store the state of the object, if there will be changes
											Undo.RecordObject(selectedTask, "Create connection");
											Undo.RecordObject(currentTask, "Create connection");

											selectedTask.Remove(currentTask);
											EditorUtility.SetDirty(selectedTask);
											EditorUtility.SetDirty(currentTask);
										}
									}
								}
							}
						}

						mouseMidDown = false;
						mouseLeftDown = false;
						selectedFirstConnNode = null;
						break;
					case EventType.KeyDown:
                            // If this control is currently active.
#if UNITY_EDITOR_OSX
                            if (currentEvent.keyCode == KeyCode.X) {
#else
						if (currentEvent.keyCode == KeyCode.LeftControl) {
#endif
							GUIUtility.hotControl = controlID;
							Event.current.Use();

							leftControlDown = true;
							currentMousePos = currentEvent.mousePosition;

#if UNITY_EDITOR_OSX
                            } else if (currentEvent.keyCode == KeyCode.C) {
#else
						} else if (currentEvent.keyCode == KeyCode.LeftAlt) {
#endif
							GUIUtility.hotControl = controlID;
							Event.current.Use();

							leftAltDown = true;
							currentMousePos = currentEvent.mousePosition;

						} else if (currentEvent.keyCode == KeyCode.Delete) {
							Event.current.Use();
							// Remove node
							ContextCallback("RemoveNode");
						} else if (currentEvent.keyCode == KeyCode.D) {
							Event.current.Use();
							// Duplicate node/nodes
							ContextCallback("Duplicate");
						} else if (currentEvent.keyCode == KeyCode.S) {
							Event.current.Use();
							// Toggle node selection
							ContextCallback("ToggleNodeSelection");
						} else if (currentEvent.keyCode == KeyCode.A) {
							Event.current.Use();
							// Add task
							ContextCallback("AddTask");
						} else if (currentEvent.keyCode == KeyCode.U) {
							Event.current.Use();
							// Add condition
							ContextCallback("AddUserCondition");
						} else if (currentEvent.keyCode == KeyCode.T) {
							Event.current.Use();
							// Add condition
							ContextCallback("AddTimerCondition");
						} else if (currentEvent.keyCode == KeyCode.E) {
							Event.current.Use();
							// Add condition
							ContextCallback("AddArrivalCondition");
						} else if (currentEvent.keyCode == KeyCode.R) {
							Event.current.Use();
							// Add condition
							ContextCallback("AddDefeatCondition");
						} else if (currentEvent.keyCode == KeyCode.G) {
							Event.current.Use();
							// Add condition
							ContextCallback("AddSurviveCondition");
						} else if (currentEvent.keyCode == KeyCode.W) {
							Event.current.Use();
							// Add win task
							ContextCallback("AddSuccessEnd");
						} else if (currentEvent.keyCode == KeyCode.F) {
							Event.current.Use();
							// Add failure task
							ContextCallback("AddFailureEnd");
						} else if (currentEvent.keyCode == KeyCode.O) {
							Event.current.Use();
							// Add failure task
							ContextCallback("AddOperatorNode");
						}

						break;

					case EventType.KeyUp:
                            // If this control is currently active.
						if (GUIUtility.hotControl == controlID) {
							GUIUtility.hotControl = 0;
							Event.current.Use();

#if UNITY_EDITOR_OSX
                            if (currentEvent.keyCode == KeyCode.X) {
#else
							if (currentEvent.keyCode == KeyCode.LeftControl) {
#endif
								leftControlDown = false;
#if UNITY_EDITOR_OSX
                                } else if (currentEvent.keyCode == KeyCode.C) {
#else
							} else if (currentEvent.keyCode == KeyCode.LeftAlt) {
#endif
								leftAltDown = false;
							}
							currentMousePos = currentEvent.mousePosition;
						}
						break;

					}

				} else if (inEditorWindowFocus && currentTextFields.Count > 0 && !posIn(currentEvent.mousePosition, currentTextFields) && inspectorRect.Contains(currentMousePos)) {
					// These two states are helper attributes, must be reseted when leaving the node view,
					// else they remember the last pressed state.
					leftControlDown = false;
					leftAltDown = false;
					// Remove keyboard focus if mouse leaving the textareas.
					GUIUtility.keyboardControl = 0;

				} else {
					// These two states are helper attributes, must be reseted when leaving the node view,
					// else they remember the last pressed state.
					leftControlDown = false;
					leftAltDown = false;
				}

				// Determine the currently selected node.
				if (mouseLeftDown && controlState == ControlState.None && GUIUtility.hotControl == 0) {
					selectedNode = NodeAtPos(currentMousePos);
					if (selectedNode != oldSelectedNode && !selectedNodes.Contains(selectedNode)) {
						foreach (NodeBase nb in selectedNodes) {
							nb.IsSelected = false;
						}
						selectedNodes.Clear();

						if (selectedNode != null) {
							if (oldSelectedNode) {
								oldSelectedNode.IsSelected = false;
								if (selectedNodes.Contains(oldSelectedNode))
									selectedNodes.Remove(oldSelectedNode);
							}
							selectedNode.IsSelected = true;
							selectedNodes.Add(selectedNode);
						} else {
							if (oldSelectedNode) {
								oldSelectedNode.IsSelected = false;
								if (selectedNodes.Contains(oldSelectedNode))
									selectedNodes.Remove(oldSelectedNode);
							}
						}
						oldSelectedNode = selectedNode;
					}
				}
                
				if (mouseLeftDown && (leftControlDown || leftAltDown) && GUIUtility.hotControl != 0) {
					setState(ControlState.DrawTransition);
					if (selectedFirstConnNode == null)
						selectedFirstConnNode = NodeAtPos(currentMousePos);
				} else if (mouseLeftDown) {
					setState(ControlState.DragWindow);
				} else {
					setState(ControlState.None);
				}

				if (currentEvent.type == EventType.ContextClick) {

					setState(ControlState.None);

					// Get the node under the cursor.
					NodeBase selNode = NodeAtPos(currentMousePos);

					if (selNode) {
						GenericMenu menu = new GenericMenu();

						menu.AddItem(new GUIContent("Duplicate"), false, ContextCallback, "Duplicate");
						menu.AddItem(new GUIContent("Remove Node"), false, ContextCallback, "RemoveNode");
						menu.AddItem(new GUIContent("Clear Connections"), false, ContextCallback, "ClearConnections");
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Select Node In Inspector"), false, ContextCallback, "SelectNodeInInspector");
						menu.AddItem(new GUIContent("Focus Node In Scene"), false, ContextCallback, "FocusNodeInScene");

						menu.ShowAsContext();
						currentEvent.Use();

					} else {
						GenericMenu menu = new GenericMenu();

						menu.AddItem(new GUIContent("Add Task"), false, ContextCallback, "AddTask");
						menu.AddItem(new GUIContent("Add Operator Node"), false, ContextCallback, "AddOperatorNode");
						menu.AddItem(new GUIContent("Add Success End"), false, ContextCallback, "AddSuccessEnd");
						menu.AddItem(new GUIContent("Add Failure End"), false, ContextCallback, "AddFailureEnd");
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Add Timer Condition"), false, ContextCallback, "AddTimerCondition");
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Add User Condition"), false, ContextCallback, "AddUserCondition");
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Add Arrival Condition"), false, ContextCallback, "AddArrivalCondition");
						menu.AddItem(new GUIContent("Add Defeat Condition"), false, ContextCallback, "AddDefeatCondition");
						menu.AddItem(new GUIContent("Add Survive Condition"), false, ContextCallback, "AddSurviveCondition");
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Add Container"), false, ContextCallback, "AddContainer");
						menu.AddItem(new GUIContent("Add Container Manager"), false, ContextCallback, "AddContainerManager");

						menu.ShowAsContext();
						currentEvent.Use();

					}
				}

				// Make the selection rect invisible aften the mouse button is up.
				if (mouseLeftUpNow) {
					selectionRect = new Rect(0, 0, 0, 0);
				}

				if (controlState != ControlState.DrawTransition && mouseLeftDown && !mouseMidDown) {
					if (mouseLeftDown && selectedNode != null
					                   && selectionRect.position == Vector2.zero && startDrags.Count == 0) {
						foreach (NodeBase nb in selectedNodes) {
							startDrags.Add(nb, nb.NodeRect.center);
						}
					} else if (mouseRightNowUp) {
						startDrags.Clear();
					}

					// Set the selection rect size and mark the nodes as selected inside the rect.
					if (mouseLeftDown && selectedNode == null) {
						selectionRect.size = currentMousePos - selectionRect.position;

						// Select the nodes in the rect. Also reselect here, so we have to synchronize
						// the list.
						foreach (NodeBase nb in selectedNodes) {
							nb.IsSelected = false;
						}
						selectedNodes.Clear();
						selectedNodes = nodesInRect(selectionRect);
						foreach (NodeBase nb in selectedNodes) {
							nb.IsSelected = true;
						}
					}

					Vector2 diffFromCenter = Vector2.zero;
					if (mouseLeftDownNow) {
						oldDraggedWindowPos = currentMousePos;
					} else if (mousePressed && selectedNode != null) {
						if (selectedNode != null) {
							diffFromCenter = currentMousePos - (selectedNode.NodeRect.center + new Vector2(panX, panY));
							Vector2 newPos = new Vector2(currentMousePos.x, currentMousePos.y);
							Vector2 diff = newPos - oldDraggedWindowPos;
							foreach (NodeBase nb in selectedNodes) {
								nb.NodeRect.center += diff;
							}

							oldDraggedWindowPos = newPos;
						}
					}
				}
                
				// Side scrolling.
				if (mouseLeftDown && !mouseMidDown) {
					Rect theMainRect = mainRect;
					// 1. defne the four side scroll areas.
					if (theMainRect.Contains(currentMousePos)) {

						Rect topRect = new Rect(theMainRect.x, theMainRect.y, theMainRect.width, sideScrolldist);
						Rect bottomRect = new Rect(theMainRect.x, theMainRect.y + theMainRect.height - sideScrolldist, theMainRect.width, sideScrolldist);
						Rect rigthRect = new Rect(theMainRect.x + theMainRect.width - sideScrolldist, theMainRect.y, sideScrolldist, theMainRect.height);
						Rect leftRect = new Rect(theMainRect.x, theMainRect.y, sideScrolldist, theMainRect.height);

						if (doDebugSideScrolling) {
							GUI.DrawTexture(bottomRect, Logo);
							GUI.DrawTexture(topRect, Logo);
							GUI.DrawTexture(rigthRect, Logo);
							GUI.DrawTexture(leftRect, Logo);
							GUI.Label(new Rect(currentMousePos.x, currentMousePos.y, 100, 18), "mouse");
						}

						if (bottomRect.Contains(currentMousePos)) {
							float bottomStrength = Mathf.Abs(currentMousePos.y - theMainRect.y - theMainRect.height + sideScrolldist);
							if (doDebugSideScrolling)
								GUI.Label(new Rect(currentMousePos.x, currentMousePos.y, 100, 18), "mouse b " + bottomStrength);
							panY -= bottomStrength * scrollSpeedFactor;
						} else if (topRect.Contains(currentMousePos)) {
							float topStrength = Mathf.Abs(sideScrolldist - (currentMousePos.y - theMainRect.y));
							if (doDebugSideScrolling)
								GUI.Label(new Rect(currentMousePos.x, currentMousePos.y, 100, 18), "mouse t " + topStrength);
							panY += topStrength * scrollSpeedFactor;
						} else if (rigthRect.Contains(currentMousePos)) {
							float rightStrength = Mathf.Abs(currentMousePos.x - theMainRect.x - theMainRect.width + sideScrolldist);
							if (doDebugSideScrolling)
								GUI.Label(new Rect(currentMousePos.x, currentMousePos.y, 100, 18), "mouse r " + rightStrength);
							panX -= rightStrength * scrollSpeedFactor;
						} else if (leftRect.Contains(currentMousePos)) {
							float leftStrength = Mathf.Abs(sideScrolldist - (currentMousePos.x - theMainRect.x));
							if (doDebugSideScrolling)
								GUI.Label(new Rect(currentMousePos.x, currentMousePos.y, 100, 18), "mouse l " + leftStrength);
							panX += leftStrength * scrollSpeedFactor;
						}
					}
				}

				Vector2 currentPos = new Vector2(Mathf.Abs(panX), Mathf.Abs(panY));
				if (CurrentAGM.PosInNodeEditor != currentPos) {
					Undo.RecordObject(CurrentAGM, "View Position changed");
					CurrentAGM.PosInNodeEditor = currentPos;
					EditorUtility.SetDirty(CurrentAGM);
				}

				GUILayout.BeginArea(mainRect);
				// Paint the grid background.
				GUI.DrawTextureWithTexCoords(new Rect(0, 0, mainRect.width, mainRect.height), EditorBG,
					new Rect(-panX / EditorBG.width, panY / EditorBG.height, mainRect.width / EditorBG.width, mainRect.height / EditorBG.height));
                
				// This group for moving the content with the middle mouse.
				GUI.BeginGroup(new Rect(panX, panY, 20000 - panX, 20000 - panY));

				BeginWindows();
				for (int i = 0; i < nodes.Count; i++) {
					if (nodes[i] != null) {
						// Paint the type background, if available.
						if ((int)nodes[i].TheBaseNode.Type < CurrentAGM.TypeColors.Count) {
							float sideReduction = 2;
							float sideWidthX = 18 * nodes[i].NodeRect.width / 128;
							float sideWidthY = 18 * nodes[i].NodeRect.height / 128;
                            
							float sideReductionSel = (nodes[i] == selectedNode) ? 4 : 2;
							float centerX = nodes[i].NodeRect.width / 2;
							float centerY = nodes[i].NodeRect.height / 2;

							// For selection
							Color32 selectionColor = nodes[i].IsSelected ? SelectionColor : SelectionColorTransparent;
							float right = nodes[i].NodeRect.x + nodes[i].NodeRect.width;
							float left = nodes[i].NodeRect.x;
							float bottom = nodes[i].NodeRect.y + nodes[i].NodeRect.height;
							float top = nodes[i].NodeRect.y;
							float width = nodes[i].NodeRect.width;
							float height = nodes[i].NodeRect.height;

							EditorGUI.DrawRect(new Rect(right - sideReductionSel, top, sideReductionSel, height), selectionColor);
							EditorGUI.DrawRect(new Rect(left, top, sideReductionSel, height), selectionColor);
							EditorGUI.DrawRect(new Rect(left, bottom - sideReductionSel, width, sideReductionSel), selectionColor);
							EditorGUI.DrawRect(new Rect(left, top, width, sideReductionSel), selectionColor);

							// For type color
							EditorGUI.DrawRect(new Rect(
								nodes[i].NodeRect.x + sideReduction, nodes[i].NodeRect.y + centerY - sideWidthY,
								nodes[i].NodeRect.width - sideReduction * 2, sideWidthY * 2),
								CurrentAGM.TypeColors[(int)nodes[i].TheBaseNode.Type]);

							EditorGUI.DrawRect(new Rect(
								nodes[i].NodeRect.x + centerX - sideWidthX, nodes[i].NodeRect.y + sideReduction,
								sideWidthX * 2, nodes[i].NodeRect.height - sideReduction * 2),
								CurrentAGM.TypeColors[(int)nodes[i].TheBaseNode.Type]);
						}
						// Paint the node.
						nodes[i].NodeRect = GUI.Window(i, nodes[i].NodeRect, DrawNodeWindow, "");
					}
				}
				EndWindows();

				// Draw connection to the mouse.
				if (selectedFirstConnNode != null && controlState == ControlState.DrawTransition) {
					Vector2 pannerMousePos = new Vector2(currentMousePos.x - panX, currentMousePos.y - panY);
					if (leftControlDown) {
						DrawNodeCurve(selectedFirstConnNode.NodeRect, pannerMousePos, Color.green);
					} else if (leftAltDown) {
						DrawNodeCurve(selectedFirstConnNode.NodeRect, pannerMousePos, Color.red);
					}
				}

				// Do forward connection.
				if (CurrentAGM) {
					BaseNode[] missTasks = CurrentAGM.transform.GetComponentsInChildren<BaseNode>();
					foreach (BaseNode baseNode in missTasks) {
						if (baseNode is Task) {
							Task task = baseNode as Task;
							if (task && baseNode) {
								foreach (BaseNode missNext in task.TasksActivatedAfterSuccess) {
									if (missNext && missNodeDict.ContainsKey(baseNode) && missNodeDict.ContainsKey(missNext))
										DrawNodeCurve(missNodeDict[baseNode], missNodeDict[missNext], MissA);
								}
								foreach (BaseNode missNext in task.TasksActivatedAfterFailed) {
									if (missNext && missNodeDict.ContainsKey(baseNode) && missNodeDict.ContainsKey(missNext))
										DrawNodeCurve(missNodeDict[baseNode], missNodeDict[missNext], FailA);
								}
								foreach (BaseNode missNext in task.Conditions) {
									if (missNext && missNodeDict.ContainsKey(baseNode) && missNodeDict.ContainsKey(missNext))
										DrawNodeCurve(missNodeDict[baseNode], missNodeDict[missNext], CondA);
								}
							}
						}
					}
				}
				GUI.EndGroup();

				// Draw the selection rectangle. 0.1f just get the left upper transparent corner, I know, it is a bit lazy.
				GUI.DrawTextureWithTexCoords(selectionRect, SelectionBG,
					new Rect(-panX / SelectionBG.width, panY / SelectionBG.height, 0.01f, 0.01f));

				// Draw coordinates
				GUI.color = CoordC;
				GUI.Label(new Rect(mainRect.width / 2 - 100, 4, 200, 20), (Mathf.Abs(panX)) + ", " + (Mathf.Abs(panY)));

				GUILayout.EndArea();

				GUILayout.BeginArea(inspectorRect, GUI.skin.box);
				DrawInspectorWindow();
				GUILayout.EndArea();

				// Default type selection.
				GUI.color = Color.white;
				GUIStyle typeSelStyle = new GUIStyle(EditorStyles.popup);
				// Paint the type color, if available.
				typeSelStyle.normal.textColor = LBC;
				if ((int)CurrentAGM.DefaultType < CurrentAGM.TypeColors.Count) {
					typeSelStyle.normal.textColor = CurrentAGM.TypeColors[(int)CurrentAGM.DefaultType];
				}
				typeSelStyle.fontSize = 16;
				typeSelStyle.fixedHeight = 24;

				// Paint the type selection popup.
				BaseNode.Types oldType = CurrentAGM.DefaultType;
				BaseNode.Types newType = (BaseNode.Types)EditorGUI.EnumPopup(selPopupRect, oldType, typeSelStyle);
				// Just change the text if the textarea has been changed.
				if (newType != oldType) {
					Undo.RecordObject(CurrentAGM, "Item Value Changed");
					CurrentAGM.DefaultType = newType;
					EditorUtility.SetDirty(CurrentAGM);
				}

				GUI.color = LBC;
				GUI.Label(new Rect(mainRect.width - 304, 54, 300, 20), "Default Node Type");

				// For updating nodes runtime. At runtime nodes can be added dynamically to the graph,
				// that does not update the editor view to do not influence to much CPU performance.
				// But you can manually apply the performance hungry update, where each node will 
				// be recreated.
				if (GUI.Button(new Rect(mainRect.width - 64, 110, 60, 40), "Update")) {
					Reinitialize();
				}
			}

			// Manager must be always selectable.
			GUI.color = Color.white;
			GUIStyle popupStyle = new GUIStyle(EditorStyles.popup);
			popupStyle.fontSize = 16;
			popupStyle.normal.textColor = AGMPopupTextColor;
			popupStyle.fixedHeight = 24;

			// Paint the manager selection popup.
			List<string> managerNames = new List<string>();
			foreach (ActivationGraphManager agm in AGManagers) {
				// After starting the game, the agm can be null, because a clone of the graph will be created.
				if (agm)
					managerNames.Add(agm.gameObject.name);
			}
			int oldIndex = System.Array.IndexOf(AGManagers, CurrentAGM);
			int newIndex = EditorGUI.Popup(popupRect, "", oldIndex, managerNames.ToArray(), popupStyle);
			// Just change the text if the textarea has been changed.
			if (newIndex != oldIndex) {
				lastManagerIndex = newIndex;
				CurrentAGM = AGManagers[newIndex];

				panX = -CurrentAGM.PosInNodeEditor.x;
				panY = -CurrentAGM.PosInNodeEditor.y;
				ShowNodes();
			}

			GUI.color = YA;
			GUI.Label(new Rect(mainRect.width - 304, 4, 300, 20), "Activation Graph");
		}

		/// <summary>
		/// It collects the nodes for the selection rect.
		/// </summary>
		/// <param name="rect"></param>
		/// <returns></returns>
		private List<NodeBase> nodesInRect(Rect rect) {
			rect = CorrectRectForMinusWidthHeight(rect);

			List<NodeBase> nodesRet = new List<NodeBase>();
			Rect pannedPos = new Rect(rect.position.x - panX, rect.position.y - panY, rect.width, rect.height);
			foreach (NodeBase node in nodes) {
				if (node.NodeRect.Overlaps(pannedPos))
					nodesRet.Add(node);
			}
			return nodesRet;
		}

		/// <summary>
		/// Corrects the rect if width or height is minus. Needed for the selection
		/// rect check.
		/// </summary>
		/// <param name="rect"></param>
		/// <returns></returns>
		Rect CorrectRectForMinusWidthHeight(Rect rect) {
			Vector2 p1 = rect.position;
			Vector2 p2 = new Vector2(p1.x + rect.width, p1.y + rect.height);
			Vector2 diff = p1 - p2;
            
			if (diff.x < 0)
				rect.x = p1.x;
			else
				rect.x = p2.x;
			if (diff.y < 0)
				rect.y = p1.y;
			else
				rect.y = p2.y;

			rect.width = Mathf.Abs(diff.x);
			rect.height = Mathf.Abs(diff.y);

			return rect;
		}

		private NodeBase NodeAtPos(Vector2 posIn) {
			Vector2 pos = new Vector2(posIn.x, posIn.y);
			if (!mainRect.Contains(pos))
				return selectedNode;

			Vector2 pannedPos = new Vector2(posIn.x - panX, posIn.y - panY);
			foreach (NodeBase node in nodes) {
				if (node.NodeRect.Contains(pannedPos))
					return node;
			}
			return null;
		}

		private Vector2 getPaddedPos(Vector2 pos) {
			return new Vector2(pos.x - panX, pos.y - panY);
		}

		private void ContextCallback(object obj) {

			//make the passed object to a string
			string clb = obj.ToString();

			//add the node we want
			if (clb.Equals("AddTask")) {

				GameObject go = new GameObject("Task");
				go.isStatic = true;
				go.transform.SetParent(CurrentAGM.transform);
				TaskDataNode task = go.AddComponent<TaskDataNode>();
				AddNode(task, getPaddedPos(currentMousePos));

				Undo.RegisterCreatedObjectUndo(go, "Add Task");

			} else if (clb.Equals("AddSuccessEnd")) {
				GameObject go = new GameObject("Success");
				go.isStatic = true;
				go.transform.SetParent(CurrentAGM.transform);
				SuccessEnd task = go.AddComponent<SuccessEnd>();
				AddNode(task, getPaddedPos(currentMousePos));

				Undo.RegisterCreatedObjectUndo(go, "Add Task");

			} else if (clb.Equals("AddFailureEnd")) {
				GameObject go = new GameObject("Failure");
				go.isStatic = true;
				go.transform.SetParent(CurrentAGM.transform);
				FailureEnd task = go.AddComponent<FailureEnd>();
				AddNode(task, getPaddedPos(currentMousePos));

				Undo.RegisterCreatedObjectUndo(go, "Add Task");

			} else if (clb.Equals("AddUserCondition")) {
				GameObject go = new GameObject("UserC");
				go.isStatic = true;
				go.transform.SetParent(CurrentAGM.transform);
				ConditionUser cond = go.AddComponent<ConditionUser>();
				AddNode(cond, getPaddedPos(currentMousePos));

				Undo.RegisterCreatedObjectUndo(go, "Add User Condition");

			} else if (clb.Equals("AddTimerCondition")) {
				GameObject go = new GameObject("TimerC");
				go.isStatic = true;
				go.transform.SetParent(CurrentAGM.transform);
				ConditionTimer cond = go.AddComponent<ConditionTimer>();
				AddNode(cond, getPaddedPos(currentMousePos));

				Undo.RegisterCreatedObjectUndo(go, "Add Timer Condition");

			} else if (clb.Equals("AddArrivalCondition")) {
				GameObject go = new GameObject("ArrivalC");
				go.isStatic = true;
				go.transform.SetParent(CurrentAGM.transform);
				ConditionArrival cond = go.AddComponent<ConditionArrival>();
				SphereCollider coll = go.AddComponent<SphereCollider>();
				coll.radius = 20;
				coll.isTrigger = true;
				cond.ArrivalCollider = coll;
				AddNode(cond, getPaddedPos(currentMousePos));

				Undo.RegisterCreatedObjectUndo(go, "Add Condition");

			} else if (clb.Equals("AddDefeatCondition")) {
				GameObject go = new GameObject("DefeatC");
				go.isStatic = true;
				go.transform.SetParent(CurrentAGM.transform);
				ConditionDefeat cond = go.AddComponent<ConditionDefeat>();
				AddNode(cond, getPaddedPos(currentMousePos));

				Undo.RegisterCreatedObjectUndo(go, "Add Condition");

			} else if (clb.Equals("AddSurviveCondition")) {
				GameObject go = new GameObject("SurviveC");
				go.isStatic = true;
				go.transform.SetParent(CurrentAGM.transform);
				ConditionSurvive cond = go.AddComponent<ConditionSurvive>();
				AddNode(cond, getPaddedPos(currentMousePos));

				Undo.RegisterCreatedObjectUndo(go, "Add Condition");

			} else if (clb.Equals("AddContainer")) {
				GameObject go = new GameObject("Container");
				go.isStatic = true;
				go.transform.SetParent(CurrentAGM.transform);
				Container cond = go.AddComponent<Container>();
				AddNode(cond, getPaddedPos(currentMousePos));

				Undo.RegisterCreatedObjectUndo(go, "Add Container");

			} else if (clb.Equals("AddContainerManager")) {
				GameObject go = new GameObject("ContMan");
				go.isStatic = true;
				go.transform.SetParent(CurrentAGM.transform);
				ContainerManager cond = go.AddComponent<ContainerManager>();
				AddNode(cond, getPaddedPos(currentMousePos));

				Undo.RegisterCreatedObjectUndo(go, "Add Container Manager");

			} else if (clb.Equals("AddOperatorNode")) {
				GameObject go = new GameObject("Op");
				go.isStatic = true;
				go.transform.SetParent(CurrentAGM.transform);
				OperatorNode cond = go.AddComponent<OperatorNode>();
				AddNode(cond, getPaddedPos(currentMousePos));

				Undo.RegisterCreatedObjectUndo(go, "Add Operator Node");

			} else if (clb.Equals("Duplicate")) {
				duplicateSelectedNodes();

			} else if (clb.Equals("ToggleNodeSelection")) {
				toggleNodeSelection();

			} else if (clb.Equals("RemoveNode")) {
				deleteNodes();

			} else if (clb.Equals("ClearConnections")) {

				// Dont need to click on node for key operations.
				NodeBase selNode = NodeAtPos(currentMousePos);

				// Removing by key the node must be selected.
				if (selNode == null)
					return;

				// Clear to me.
				BaseNode node = selNode.TheBaseNode;

				if (node is TaskBase) {
					TaskBase selectedTask = node as TaskBase;
					BaseNode[] tasks = CurrentAGM.transform.GetComponentsInChildren<BaseNode>();
					foreach (BaseNode baseNode in tasks) {
						if (baseNode is TaskBase) {
							TaskBase baseTask = baseNode as TaskBase;

							// Store the state of the object, if there will be changes
							if (baseTask.TasksActivatedAfterSuccess.Contains(selectedTask)
							                         || baseTask.TasksActivatedAfterFailed.Contains(selectedTask)) {
								Undo.RecordObject(baseTask, "Remove connection");
								Undo.RecordObject(selectedTask, "Remove connection");

								baseTask.Remove(selectedTask);
								selectedTask.Remove(baseTask);

								EditorUtility.SetDirty(baseTask);
								EditorUtility.SetDirty(selectedTask);
							}
						}
					}

					// For prepare undo
					bool hasConditions = false;
					if (selectedTask is Task) {
						Task miss = selectedTask as Task;
						hasConditions = miss.Conditions.Count > 0;
					}

					// Store the state of the object, if there will be changes
					if (selectedTask.TasksActivatedAfterSuccess.Count > 0
					                   || selectedTask.TasksActivatedAfterFailed.Count > 0
					                   || hasConditions) {

						Undo.RecordObject(selectedTask, "Remove connection");
						List<TaskBase> toDirty = new List<TaskBase>();
						foreach (TaskBase b in selectedTask.TasksActivatedAfterSuccess) {
							Undo.RecordObject(b, "Remove connection");
							toDirty.Add(b);
						}
						foreach (TaskBase b in selectedTask.TasksActivatedAfterFailed) {
							Undo.RecordObject(b, "Remove connection");
							toDirty.Add(b);
						}

						selectedTask.ClearConnections();

						EditorUtility.SetDirty(selectedTask);
						foreach (TaskBase b in toDirty) {
							EditorUtility.SetDirty(b);
						}
					}

				} else if (node is ConditionBase) {
					ConditionBase selectedCond = node as ConditionBase;
					BaseNode[] tasks = CurrentAGM.transform.GetComponentsInChildren<BaseNode>();
					foreach (BaseNode taskNode in tasks) {
						if (taskNode is Task) {
							Task task = taskNode as Task;

							// Store the state of the object, if there will be changes
							if (task.Conditions.Contains(selectedCond)) {
								Undo.RecordObject(task, "Remove connection");

								task.Remove(selectedCond);

								EditorUtility.SetDirty(task);
							}
						}
					}
				}

			} else if (clb.Equals("SelectNodeInInspector")) {

				// Dont need to click on node for key operations.
				NodeBase selNode = NodeAtPos(currentMousePos);

				// Removing by key the node must be selected.
				if (selNode == null)
					return;

				Selection.activeGameObject = selNode.TheBaseNode.gameObject;
			} else if (clb.Equals("FocusNodeInScene")) {

				// Dont need to click on node for key operations.
				NodeBase selNode = NodeAtPos(currentMousePos);

				// Removing by key the node must be selected.
				if (selNode == null)
					return;

				Selection.activeGameObject = selNode.TheBaseNode.gameObject;
				if (SceneView.lastActiveSceneView)
					SceneView.lastActiveSceneView.FrameSelected();
			}
		}

		void duplicateSelectedNodes() {
			// Don't need to click on node for key operations.
			NodeBase nodeUnderCursor = NodeAtPos(currentMousePos);
			bool doNotSelectAfterDuplicate = false;

			List<NodeBase> duplicateThese = new List<NodeBase>();
            
			if (nodeUnderCursor == null || nodeUnderCursor.IsSelected) {
				// Duplicate selected nodes.
				foreach (NodeBase bNode in selectedNodes) {
					duplicateThese.Add(bNode);
				}
			} else {
				doNotSelectAfterDuplicate = true;
				duplicateThese.Add(nodeUnderCursor);
			}


			if (selectedNodes.Count == 0)
				return;

			List<NodeBase> clonedNodes = new List<NodeBase>();

			// Clone
			foreach (NodeBase node in duplicateThese) {
				GameObject clone = Instantiate (node.TheBaseNode.gameObject);
				clone.transform.SetParent(node.TheBaseNode.gameObject.transform.parent);
				clone.name = node.TheBaseNode.gameObject.name;

				BaseNode bNode = clone.GetComponent<BaseNode>();
				// Won't have the same coordinates, so no need for move the copy a bit away to see the duplicates.
				NodeBase clonedNode = AddNode(bNode, bNode.PosInNodeEditor);
				clonedNodes.Add(clonedNode);
				Undo.RegisterCreatedObjectUndo(clone, "GameObject Cloned");
			}

			// Deselect old
			if (!doNotSelectAfterDuplicate) {
				foreach (NodeBase node in selectedNodes) {
					node.IsSelected = false;
				}
				selectedNodes.Clear();
			}

			// Disconnect all nodes.
			foreach (NodeBase node in clonedNodes) {
				if (node.TheBaseNode is TaskBase) {
					TaskBase tb = (TaskBase)node.TheBaseNode;

					tb.TasksActivatedAfterSuccess.Clear();
					tb.TasksActivatedAfterFailed.Clear();
					tb.TaskPredecessors.Clear();

					if (node.TheBaseNode is Task) {
						Task task = (Task)node.TheBaseNode;
						for (int i = task.Conditions.Count - 1; i >= 0; i--) {
							ConditionUser cu = task.Conditions[i] as ConditionUser;
							if (cu == null) {
								// Remove conditions, but UserCondition.
								task.Remove(task.Conditions[i]);
							} else if (task.Conditions[i].transform != task.transform) {
								// Remove the external user condition.
								task.RemoveUserCondition(cu);
							} else {
								// Leave the internal user node and set the current type of the AGM.
								cu.Type = CurrentAGM.DefaultType;
							}
						}
					}
				}
			}

			// Select the clones
			if (!doNotSelectAfterDuplicate) {
				foreach (NodeBase node in clonedNodes) {
					node.IsSelected = true;
					selectedNodes.Add(node);
				}
			}

			if (selectedNode != null) {
				selectedNode = clonedNodes.Find(item => item.TheBaseNode.name == selectedNode.TheBaseNode.name);
				oldSelectedNode = selectedNode;
			}

			if (selectedNode == null) {
				selectedNode = clonedNodes[0];
				oldSelectedNode = selectedNode;
			}
		}

		/// <summary>
		/// Toggle selection on the node under the cursor.
		/// </summary>
		void toggleNodeSelection() {
			NodeBase nodeUnderCursor = NodeAtPos(currentMousePos);
			if (nodeUnderCursor == null)
				return;

			nodeUnderCursor.IsSelected = !nodeUnderCursor.IsSelected;

			if (nodeUnderCursor.IsSelected) {
				selectedNodes.Add(nodeUnderCursor);
			} else {
				selectedNodes.Remove(nodeUnderCursor);
			}
		}

		/// <summary>
		/// On nothing or on selected node remove all selected nodes.
		/// On not selected node, remove the node (not the seleccted ones).
		/// </summary>
		void deleteNodes() {
			// Don't need to click on node for key operations.
			NodeBase nodeUnderCursor = NodeAtPos(currentMousePos);

			List<BaseNode> removeThese = new List<BaseNode>();
			List<NodeBase> removeTheseFromSelection = new List<NodeBase>();

			// Removing by key the node must be selected.
			if (nodeUnderCursor == null || nodeUnderCursor.IsSelected) {
				// Remove selected nodes.
				foreach (NodeBase bNode in selectedNodes) {
					removeThese.Add(bNode.TheBaseNode);
					removeTheseFromSelection.Add(bNode);
				}
			} else {
				removeThese.Add(nodeUnderCursor.TheBaseNode);
				removeTheseFromSelection.Add(nodeUnderCursor);
			}

			foreach (NodeBase node in removeTheseFromSelection) {
				node.IsSelected = false;

				if (selectedNodes.Contains(node)) {
					selectedNodes.Remove(node);
				}
			}

			foreach (BaseNode node in removeThese) {
				if (node is TaskBase) {
					TaskBase selectedTask = node as TaskBase;

					Undo.RecordObject(selectedTask, "Remove connection");

					for (int i = selectedTask.TaskPredecessors.Count - 1; i >= 0; i--) {
						TaskBase task = selectedTask.TaskPredecessors[i];
						Undo.RecordObject(task, "Remove connection");

						task.Remove(selectedTask);

						EditorUtility.SetDirty(task);
					}

					for (int i = selectedTask.TasksActivatedAfterSuccess.Count - 1; i >= 0; i--) {
						TaskBase task = selectedTask.TasksActivatedAfterSuccess[i];
						Undo.RecordObject(task, "Remove connection");

						selectedTask.Remove(task);

						EditorUtility.SetDirty(task);
					}

					for (int i = selectedTask.TasksActivatedAfterFailed.Count - 1; i >= 0; i--) {
						TaskBase task = selectedTask.TasksActivatedAfterFailed[i];
						Undo.RecordObject(task, "Remove connection");

						selectedTask.Remove(task);

						EditorUtility.SetDirty(task);
					}

					EditorUtility.SetDirty(selectedTask);

					RemoveNode(node);
				} else if (node is ConditionBase) {
					ConditionBase selectedCond = node as ConditionBase;
					BaseNode[] tasks = CurrentAGM.transform.GetComponentsInChildren<BaseNode>();
					foreach (BaseNode taskNode in tasks) {
						if (taskNode is Task) {
							Task task = taskNode as Task;

							Undo.RecordObject(task, "Remove connection");

							task.Remove(selectedCond);

							EditorUtility.SetDirty(task);
						}
					}
					RemoveNode(node);
				} else {
					RemoveNode(node);
				}
			}
		}

		/// <summary>
		/// Check a position in a list of rects.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="rects"></param>
		/// <returns></returns>
		private bool posIn(Vector2 pos, List<Rect> rects) {
			foreach (Rect r in rects) {
				if (r.Contains(pos))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Draws the window right side, the inspector window.
		/// </summary>
		private void DrawInspectorWindow() {

			// Else, layout cannot be shown correctly.
			if (Event.current.type == EventType.MouseDown && !inspectorRect.Contains(currentMousePos)) {
				return;
			}

			GUI.skin = guiSkin;
			EditorStyles.textField.wordWrap = true;

			Rect cr = mainRect;

			float oneLineHeight = GUI.skin.textArea.lineHeight;
			float pad = GUI.skin.textArea.padding.top + GUI.skin.textArea.padding.bottom;

			scrollPosInspector = EditorGUILayout.BeginScrollView(scrollPosInspector, GUILayout.MaxWidth(inspectorWidth));

			if (selectedNode) {
				BaseNode node = selectedNode.TheBaseNode;
				if (node == null) {
					EditorGUILayout.EndScrollView();
					return;
				}

				if (Event.current.type == EventType.Repaint)
					currentTextFields.Clear();

				if (node is TaskDataNode) {
					GUI.color = TNC;
					GUILayout.Label("TASK NODE");
				} else if (node is OperatorNode) {
					GUI.color = TSDC;
					GUILayout.Label("OPERATOR NODE");
				} else if (node is SuccessEnd) {
					GUI.color = TSUC;
					GUILayout.Label("VICTORY NODE");
				} else if (node is FailureEnd) {
					GUI.color = TFAC;
					GUILayout.Label("FAILURE NODE");
				} else if (node is ConditionUser) {
					GUI.color = TDC;
					GUILayout.Label("USER CONDITION NODE");
				} else if (node is ConditionTimer) {
					GUI.color = TDC;
					GUILayout.Label("TIMER CONDITION NODE");
				} else if (node is ConditionArrival) {
					GUI.color = TDC;
					GUILayout.Label("ARRIVAL CONDITION NODE");
				} else if (node is ConditionDefeat) {
					GUI.color = TDC;
					GUILayout.Label("DEFEAT CONDITION NODE");
				} else if (node is ConditionSurvive) {
					GUI.color = TDC;
					GUILayout.Label("SURVIVE CONDITION NODE");
				} else if (node is Container) {
					GUI.color = LOC;
					GUILayout.Label("CONTAINER");
				} else if (node is ContainerManager) {
					GUI.color = LOC;
					GUILayout.Label("CONTAINER MANAGER");
				}

				GUILayout.Label("Game Object Name");
				string oldText = node.name;
				//string newText = EditorGUI.TextArea(rect, node.name);
				string newText = EditorGUILayout.TextArea(node.name);
				// Remember the rect from the previously created textarea component to
				// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
				if (Event.current.type == EventType.Repaint) {
					Rect lr = GUILayoutUtility.GetLastRect();
					currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
				}
				// Just change the text if the textarea has been changed.
				if (newText != oldText) {
					Undo.RecordObject(node, "Text changed");
					node.name = newText;
					EditorUtility.SetDirty(node);
				}


				GUILayout.Label("Type of the node (Extend the enum, if you need more types.)");

				// The type of the task node.
				TaskDataNode.Types oldType = node.Type;
				TaskDataNode.Types newType = (TaskDataNode.Types)EditorGUILayout.EnumPopup(node.Type, GUILayout.MinHeight(oneLineHeight * 2 + pad));
				// Remember the rect from the previously created textarea component to
				// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
				if (Event.current.type == EventType.Repaint) {
					Rect lr = GUILayoutUtility.GetLastRect();
					currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
				}
				// Just change the text if the textarea has been changed.
				if (newType != oldType) {
					Undo.RecordObject(node, "Text changed");
					node.Type = newType;
					EditorUtility.SetDirty(node);
				}

				if (node is TaskDataNode) {
					TaskDataNode entry = node as TaskDataNode;

					GUI.color = TMNC;
					GUILayout.Label("Task Name");

					oldText = entry.TaskName;
					newText = EditorGUILayout.TextArea(entry.TaskName, GUILayout.MinHeight(oneLineHeight * 2 + pad));
					// Remember the rect from the previously created textarea component to
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					// Just change the text if the textarea has been changed.
					if (newText != oldText) {
						Undo.RecordObject(entry, "Text changed");
						entry.TaskName = newText;
						EditorUtility.SetDirty(entry);
					}

					GUI.color = TSDC;
					GUILayout.Label("Short Description");
					oldText = entry.TaskDescShort;
					newText = EditorGUILayout.TextArea(entry.TaskDescShort, GUILayout.MinHeight(oneLineHeight * 3 + pad));
					// Remember the rect from the previously created textarea component to
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					// Just change the text if the textarea has been changed.
					if (newText != oldText) {
						Undo.RecordObject(entry, "Text changed");
						entry.TaskDescShort = newText;
						EditorUtility.SetDirty(entry);
					}


					// Toggle visibility for description fields.
					EditorGUILayout.BeginHorizontal();

					GUI.color = Color.white;
					// Just for spacing.
					GUILayout.Label("");
					GUILayout.Label("Show Description Fields", GUILayout.Width(120));
					bool oldValueBool = showBigDescriptionFields;
					bool newValueBool = GUILayout.Toggle(oldValueBool, "", GUILayout.Width(16), GUILayout.Height(16));
					if (newValueBool != oldValueBool) {
						showBigDescriptionFields = newValueBool;
					}

					EditorGUILayout.EndHorizontal();


					if (showBigDescriptionFields) {

						GUI.color = TDC;
						GUILayout.Label("Description");
						oldText = entry.TaskDesc;
						newText = EditorGUILayout.TextArea(entry.TaskDesc, GUILayout.MinHeight(oneLineHeight * 10 + pad));
						// Remember the rect from the previously created textarea component to
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						// Just change the text if the textarea has been changed.
						if (newText != oldText) {
							Undo.RecordObject(entry, "Text changed");
							entry.TaskDesc = newText;
							EditorUtility.SetDirty(entry);
						}

						GUI.color = TSUC;
						GUILayout.Label("Success Description");
						oldText = entry.TaskSuccDesc;
						newText = EditorGUILayout.TextArea(entry.TaskSuccDesc, GUILayout.MinHeight(oneLineHeight * 6 + pad));
						// Remember the rect from the previously created textarea component to
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						// Just change the text if the textarea has been changed.
						if (newText != oldText) {
							Undo.RecordObject(entry, "Text changed");
							entry.TaskSuccDesc = newText;
							EditorUtility.SetDirty(entry);
						}

						GUI.color = TFAC;
						GUILayout.Label("Failure Description");
						oldText = entry.TaskFailDesc;
						newText = EditorGUILayout.TextArea(entry.TaskFailDesc, GUILayout.MinHeight(oneLineHeight * 6 + pad));
						// Remember the rect from the previously created textarea component to
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						// Just change the text if the textarea has been changed.
						if (newText != oldText) {
							Undo.RecordObject(entry, "Text changed");
							entry.TaskFailDesc = newText;
							EditorUtility.SetDirty(entry);
						}

					}


					// Set in Active Mode
					GUI.color = GA;
					bool oldValue = CurrentAGM.IsInActiveMode(entry);
					bool newValue = EditorGUILayout.Toggle("Set in Active Mode", oldValue);
					if (newValue != oldValue) {
						Undo.RecordObject(CurrentAGM, "Toggle Set in Active Mode");
						CurrentAGM.SetInActiveMode(entry, newValue);
						EditorUtility.SetDirty(CurrentAGM);
					}

					// Reset after end state.
					GUI.color = YA;
					oldValue = entry.Restartable;
					newValue = EditorGUILayout.Toggle("Restartable", oldValue);
					if (newValue != oldValue) {
						Undo.RecordObject(CurrentAGM, "Restartable");
						entry.Restartable = newValue;
						EditorUtility.SetDirty(CurrentAGM);
					}

					// Activate Outgoings Manually
					GUI.color = Color.white;
					oldValueBool = entry.ActivateOutgoingsManually;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Activate Outg. Manually", "Activate Outgoings Manually"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Activate Outgoings Manually");
						entry.ActivateOutgoingsManually = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Enable activation limit
					GUI.color = Color.white;
					oldValueBool = entry.EnableActivationLimit;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Enable activation limit", "Enable activation limit"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Enable activation limit");
						entry.EnableActivationLimit = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Activation limit
					if (entry.EnableActivationLimit) {
						int oldValueInt = entry.ActivationLimit;
						int newValueInt = EditorGUILayout.IntField(new GUIContent("  Activation limit", "Activation limit"), oldValueInt);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueInt != oldValueInt) {
							Undo.RecordObject(entry, "Activation limit");
							entry.ActivationLimit = newValueInt;
							EditorUtility.SetDirty(entry);
						}
					}


					// Enable min activations
					oldValueBool = entry.EnableActivateAfterNActivations;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Enable min activations", "Enable min activations"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Enable min activations");
						entry.EnableActivateAfterNActivations = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Min activations
					if (entry.EnableActivateAfterNActivations) {
						int oldValueInt = entry.ActivateAfterNActivations;
						int newValueInt = EditorGUILayout.IntField(new GUIContent("  Min activations", "Min activations"), oldValueInt);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueInt != oldValueInt) {
							Undo.RecordObject(entry, "Min activations");
							entry.ActivateAfterNActivations = newValueInt;
							EditorUtility.SetDirty(entry);
						}
					}

					// Current activations
					GUILayout.Label("Current activations: " + entry.CurrentActivations);

					// Check condition timer
					float oldValueFloat = entry.CheckPeriod;
					float newValueFloat = EditorGUILayout.FloatField(new GUIContent("Check conditions timer", "Check conditions timer"), oldValueFloat);
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					if (newValueFloat != oldValueFloat) {
						Undo.RecordObject(entry, "Check conditions timer");
						entry.CheckPeriod = newValueFloat;
						EditorUtility.SetDirty(entry);
					}


					GUI.color = Color.white;
					GUILayout.Label("-   -   -");


					GUI.color = TDC;
					GUILayout.Label("ATTACHED USER CONDITIONS");

					GUILayout.Label("________________________________");

					// Containers
					List<ConditionUser> uConds = entry.GetUserConditions();
					for (int i = 0; i < uConds.Count; i++) {

						EditorGUILayout.BeginHorizontal();

						GUI.color = TDC;
						GUILayout.Label("Name");

						string oldValueString = uConds[i].Name;
						string newValueString = EditorGUILayout.TextField(oldValueString);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueString != oldValueString) {
							Undo.RecordObject(entry, "Condition Name Changed");
							uConds[i].Name = newValueString;
							EditorUtility.SetDirty(entry);
						}

						// Move Up Container
						GUI.color = TDC;
						if (GUILayout.Button(new GUIContent("↑", "Move Up Condition"), GUILayout.Width(20))) {
							if (i < uConds.Count) {
								Undo.RecordObject(entry, "Move Up Condition");
								if (i != 0) {
									entry.InsertUserCondition(i - 1, uConds[i]);
									entry.RemoveUserConditionAt(i + 1);
								}
								EditorUtility.SetDirty(entry);
								return;
							}
						}

						GUILayout.Space(-4);

						// Move Down Container
						GUI.color = TDC;
						if (GUILayout.Button(new GUIContent("↓", "Move Down Condition"), GUILayout.Width(20))) {
							if (i < uConds.Count - 1) {
								Undo.RecordObject(entry, "Move Down Condition");
								if (i < uConds.Count) {
									entry.InsertUserCondition(i + 2, uConds[i]);
									entry.RemoveUserConditionAt(i);
								}
								EditorUtility.SetDirty(entry);
								return;
							}
						}

						GUILayout.Space(-4);

						// Insert Container
						GUI.color = GA;
						if (GUILayout.Button(new GUIContent("+", "Insert Condition"), GUILayout.Width(20))) {
							if (i < uConds.Count) {
								Undo.RecordObject(entry, "Insert Item");
								ConditionUser cu = entry.InsertUserCondition(i + 1);
								cu.Type = CurrentAGM.DefaultType;
								EditorUtility.SetDirty(entry);
								return;
							}
						}

						GUILayout.Space(-4);

						// Remove Container
						GUI.color = RA;
						if (GUILayout.Button(new GUIContent("X", "Remove Condition"), GUILayout.Width(20))) {
							if (i < uConds.Count) {
								Undo.RecordObject(entry, "Condition removed");
								entry.RemoveUserConditionAt(i);
								EditorUtility.SetDirty(entry);
								return;
							}
						}

						// This is not yet supported. If the condition will be extracted, then it must be on an own game object,
						// else after deleting the task or the conditions, all the nodes will be removed, because they are all
						// on the same game object.
						/*
                        GUILayout.Space(-4);

                        // Show condition outside of the task
                        GUI.color = LBC;
                        if (GUILayout.Button(new GUIContent(">", "Show condition outside of the task"), GUILayout.Width(20))) {
                            if (i < uConds.Count) {
                                Undo.RecordObject(entry, "Condition shown");
                                entry.ShowCondition(uConds[i], true);
                                EditorUtility.SetDirty(entry);
                                return;
                            }
                        }*/

						GUILayout.Space(16);

						// Toggle visibility
						GUI.color = Color.white;
						oldValueBool = uConds[i].IsExpanded;
						newValueBool = GUILayout.Toggle(oldValueBool, "", GUILayout.Width(16), GUILayout.Height(16));
						if (newValueBool != oldValueBool) {
							Undo.RecordObject(entry, "Condition shown or hidden");
							uConds[i].IsExpanded = newValueBool;
							EditorUtility.SetDirty(entry);
						}

						EditorGUILayout.EndHorizontal();

						if (i < uConds.Count && uConds[i].IsExpanded) {

							GUI.color = TDC;

							GUILayout.Label("Type of the node (Extend the enum, if you need more types.)");

							// The type of the task node.
							oldType = uConds[i].Type;
							newType = (TaskDataNode.Types)EditorGUILayout.EnumPopup(uConds[i].Type, GUILayout.MinHeight(oneLineHeight * 2 + pad));
							// Remember the rect from the previously created textarea component to
							// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
							if (Event.current.type == EventType.Repaint) {
								Rect lr = GUILayoutUtility.GetLastRect();
								currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
							}
							// Just change the text if the textarea has been changed.
							if (newType != oldType) {
								Undo.RecordObject(uConds[i], "Text changed");
								uConds[i].Type = newType;
								EditorUtility.SetDirty(uConds[i]);
							}

							drawConditionUser(uConds[i], cr, oneLineHeight, pad);

							GUILayout.Label("");
							GUILayout.Label("________________________________");
						}
					}

					// Add Entry
					GUI.color = GA;
					if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Add Condition", "Add Condition"))) {
						Undo.RecordObject(entry, "Condition added");
						ConditionUser cu = entry.AddUserCondition();
						cu.Type = CurrentAGM.DefaultType;
						EditorUtility.SetDirty(entry);
						return;
					}

					// Remove Last Entry
					GUI.color = RA;
					if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Remove Last Condition", "Remove Last Condition"))) {
						if (uConds.Count > 0) {
							Undo.RecordObject(entry, "Condition removed");
							entry.RemoveUserConditionAt(uConds.Count - 1);
							EditorUtility.SetDirty(entry);
							return;
						}
					}

					GUI.color = Color.white;
					GUILayout.Label("-   -   -");

					// Start task external
					GUI.color = YA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Start task external", "Start task external"))) {
						Undo.RecordObject(entry, "Start task external");
						entry.StartTaskExternal();
						EditorUtility.SetDirty(entry);
					}

					// Set Success State
					GUI.color = GA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Set Success State", "Set Success State"))) {
						Undo.RecordObject(entry, "Set Success State");
						entry.SetStateExternal(Task.TaskResult.SuccessType);
						EditorUtility.SetDirty(entry);
					}

					// Set Failure State
					GUI.color = RA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Set Failure State", "Set Failure State"))) {
						Undo.RecordObject(entry, "Set Failure State");
						entry.SetStateExternal(Task.TaskResult.FailureType);
						EditorUtility.SetDirty(entry);
					}

					// Set Running State
					GUI.color = ANDC;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Set Running State", "Set Running State"))) {
						Undo.RecordObject(entry, "Set Running State");
						entry.SetStateExternal(Task.TaskResult.RunningType);
						EditorUtility.SetDirty(entry);
					}

					// Set Inactive State
					GUI.color = YA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Set Inactive State", "Set Inactive State"))) {
						Undo.RecordObject(entry, "Set Inactive State");
						entry.SetStateExternal(Task.TaskResult.InactiveType);
						EditorUtility.SetDirty(entry);
					}

					// Set Disable State
					GUI.color = YA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Set Disable State", "Set Disable State"))) {
						Undo.RecordObject(entry, "Set Disable State");
						entry.SetStateExternal(Task.TaskResult.DisabledType);
						EditorUtility.SetDirty(entry);
					}

					GUI.color = ANDC;
					DrawMethodSelection(entry, "Action At Activate", ref entry.ScriptAtActivate, ref entry.MethodNameAtActivate);
					GUI.color = GA;
					DrawMethodSelection(entry, "Action At Success", ref entry.ScriptAtSuccess, ref entry.MethodNameAtSuccess);
					GUI.color = RA;
					DrawMethodSelection(entry, "Action At Failure", ref entry.ScriptAtFailure, ref entry.MethodNameAtFailure);
					GUI.color = YA;
					DrawMethodSelection(entry, "Action At Disable", ref entry.ScriptAtDisable, ref entry.MethodNameAtDisable);

				} else if (node is OperatorNode) {
					OperatorNode entry = node as OperatorNode;

					GUI.color = TSDC;
					GUILayout.Label("Short Description");
					oldText = entry.ShortDescription;
					newText = EditorGUILayout.TextArea(node.GetShortDescription(), GUILayout.MinHeight(oneLineHeight * 3 + pad));
					// Remember the rect from the previously created textarea component to
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					// Just change the text if the textarea has been changed.
					if (newText != oldText) {
						Undo.RecordObject(entry, "Text changed");
						entry.ShortDescription = newText;
						EditorUtility.SetDirty(entry);
					}

					// Set in Active Mode
					GUI.color = GA;
					bool oldValue = CurrentAGM.IsInActiveMode(entry);
					bool newValue = EditorGUILayout.Toggle(new GUIContent("Set in Active Mode", "Set in Active Mode"), oldValue);
					if (newValue != oldValue) {
						Undo.RecordObject(CurrentAGM, "Toggle Set in Active Mode");
						CurrentAGM.SetInActiveMode(entry, newValue);
						EditorUtility.SetDirty(CurrentAGM);
					}

					// Reset after end state.
					GUI.color = YA;
					oldValue = entry.Restartable;
					newValue = EditorGUILayout.Toggle("Restartable", oldValue);
					if (newValue != oldValue) {
						Undo.RecordObject(CurrentAGM, "Restartable");
						entry.Restartable = newValue;
						EditorUtility.SetDirty(CurrentAGM);
					}

					// And node or not
					GUI.color = ANDC;
					oldValue = entry.IsAndNode;
					newValue = EditorGUILayout.Toggle(new GUIContent("Is And Node", "Is And Node"), entry.IsAndNode);
					if (newValue != oldValue) {
						Undo.RecordObject(entry, "Toggle Is And Node");
						entry.IsAndNode = newValue;
						EditorUtility.SetDirty(entry);
					}

					// Trigger failure
					GUI.color = RA;
					bool oldValueBool = entry.TriggerFailure;
					bool newValueBool = EditorGUILayout.Toggle(new GUIContent("Trigger failure", "Toggle trigger failure"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Toggle trigger failure");
						entry.TriggerFailure = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Trigger success on concurrent tasks
					GUI.color = GA;
					oldValueBool = entry.TriggerSucccessConcurrents;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Success on conc. tasks", "Toggle success on concurrent tasks"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Toggle success on concurrent tasks");
						entry.TriggerSucccessConcurrents = newValueBool;
						if (newValueBool) {
							entry.TriggerFailureConcurrents = false;
							entry.TriggerInactiveConcurrents = false;
						}
						EditorUtility.SetDirty(entry);
					}

					// Trigger failure on concurrent tasks
					GUI.color = RA;
					oldValueBool = entry.TriggerFailureConcurrents;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Failure on conc. tasks", "Toggle failure on concurrent tasks"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Toggle failure on concurrent tasks");
						entry.TriggerFailureConcurrents = newValueBool;
						if (newValueBool) {
							entry.TriggerSucccessConcurrents = false;
							entry.TriggerInactiveConcurrents = false;
						}
						EditorUtility.SetDirty(entry);
					}

					// Trigger inactive on concurrent tasks
					GUI.color = YN;
					oldValueBool = entry.TriggerInactiveConcurrents;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Inactive on conc. tasks", "Toggle inactive on concurrent tasks"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Toggle inactive on concurrent tasks");
						entry.TriggerInactiveConcurrents = newValueBool;
						if (newValueBool) {
							entry.TriggerSucccessConcurrents = false;
							entry.TriggerFailureConcurrents = false;
						}
						EditorUtility.SetDirty(entry);
					}

					// Activate Outgoings Manually
					GUI.color = Color.white;
					oldValueBool = entry.ActivateOutgoingsManually;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Activate Outg. Manually", "Activate Outgoings Manually"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Activate Outgoings Manually");
						entry.ActivateOutgoingsManually = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Enable activation limit
					GUI.color = Color.white;
					oldValueBool = entry.EnableActivationLimit;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Enable activation limit", "Enable activation limit"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Enable activation limit");
						entry.EnableActivationLimit = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Activation limit
					if (entry.EnableActivationLimit) {
						int oldValueInt = entry.ActivationLimit;
						int newValueInt = EditorGUILayout.IntField(new GUIContent("  Activation limit", "Activation limit"), oldValueInt);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueInt != oldValueInt) {
							Undo.RecordObject(entry, "Activation limit");
							entry.ActivationLimit = newValueInt;
							EditorUtility.SetDirty(entry);
						}
					}


					// Enable min activations
					oldValueBool = entry.EnableActivateAfterNActivations;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Enable min activations", "Enable min activations"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Enable min activations");
						entry.EnableActivateAfterNActivations = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Min activations
					if (entry.EnableActivateAfterNActivations) {
						int oldValueInt = entry.ActivateAfterNActivations;
						int newValueInt = EditorGUILayout.IntField(new GUIContent("  Min activations", "Min activations"), oldValueInt);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueInt != oldValueInt) {
							Undo.RecordObject(entry, "Min activations");
							entry.ActivateAfterNActivations = newValueInt;
							EditorUtility.SetDirty(entry);
						}
					}

					// Current activations
					GUILayout.Label("Current activations: " + entry.CurrentActivations);

					// Check condition timer
					float oldValueFloat = entry.CheckPeriod;
					float newValueFloat = EditorGUILayout.FloatField(new GUIContent("Check conditions timer", "Check conditions timer"), oldValueFloat);
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					if (newValueFloat != oldValueFloat) {
						Undo.RecordObject(entry, "Check conditions timer");
						entry.CheckPeriod = newValueFloat;
						EditorUtility.SetDirty(entry);
					}

					// Start task external
					GUI.color = YA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Start task external", "Start task external"))) {
						Undo.RecordObject(entry, "Start task external");
						entry.StartTaskExternal();
						EditorUtility.SetDirty(entry);
					}


					GUI.color = GA;
					// Enable random selection of outgoing activation.
					oldValueBool = entry.IsSuccessOutRandom;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Enable random succ. out.", "Enable random selection of outgoing activation"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Enable random selection of outgoing activation");
						entry.IsSuccessOutRandom = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					GUI.color = ANDC;
					if (entry.IsSuccessOutRandom) {
						int oldValueInt = entry.MinActSuccOutgoing;
						int newValueInt = EditorGUILayout.IntField(new GUIContent("  Min out. activations", "Min out. activations"), oldValueInt);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueInt != oldValueInt) {
							Undo.RecordObject(entry, "Min activations");
							entry.MinActSuccOutgoing = newValueInt;
							EditorUtility.SetDirty(entry);
						}

						oldValueInt = entry.MaxActSuccOutgoing;
						newValueInt = EditorGUILayout.IntField(new GUIContent("  Max out. activations", "Max out. activations"), oldValueInt);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueInt != oldValueInt) {
							Undo.RecordObject(entry, "Min activations");
							entry.MaxActSuccOutgoing = newValueInt;
							EditorUtility.SetDirty(entry);
						}

						foreach (TaskBase task in entry.TasksActivatedAfterSuccess) {

							GUI.color = GA;
							GUILayout.Label("Node: " + task.name);

							GUI.color = ANDC;
							oldValueFloat = entry.SuccessedAncestorProbabbility[entry.TasksActivatedAfterSuccess.IndexOf(task)];
							newValueFloat = EditorGUILayout.FloatField(new GUIContent("Probability", "Probability"), oldValueFloat);
							// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
							if (Event.current.type == EventType.Repaint) {
								Rect lr = GUILayoutUtility.GetLastRect();
								currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
							}
							if (newValueFloat != oldValueFloat) {
								Undo.RecordObject(entry, "Probability");
								entry.SuccessedAncestorProbabbility[entry.TasksActivatedAfterSuccess.IndexOf(task)] = newValueFloat;
								EditorUtility.SetDirty(entry);
							}
						}
					}

					GUI.color = RA;
					// Enable random selection of outgoing activation.
					oldValueBool = entry.IsFailureOutRandom;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Enable random fail. out.", "Enable random selection of outgoing activation"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Enable random selection of outgoing activation");
						entry.IsFailureOutRandom = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					GUI.color = ANDC;
					if (entry.IsFailureOutRandom) {
						int oldValueInt = entry.MinActFailOutgoing;
						int newValueInt = EditorGUILayout.IntField(new GUIContent("  Min out. activations", "Min out. activations"), oldValueInt);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueInt != oldValueInt) {
							Undo.RecordObject(entry, "Min activations");
							entry.MinActFailOutgoing = newValueInt;
							EditorUtility.SetDirty(entry);
						}

						oldValueInt = entry.MaxActFailOutgoing;
						newValueInt = EditorGUILayout.IntField(new GUIContent("  Max out. activations", "Max out. activations"), oldValueInt);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueInt != oldValueInt) {
							Undo.RecordObject(entry, "Min activations");
							entry.MaxActFailOutgoing = newValueInt;
							EditorUtility.SetDirty(entry);
						}

						foreach (TaskBase task in entry.TasksActivatedAfterFailed) {

							GUI.color = RA;
							GUILayout.Label("Node: " + task.name);

							GUI.color = ANDC;
							oldValueFloat = entry.FailedAncestorProbabbility[entry.TasksActivatedAfterFailed.IndexOf(task)];
							newValueFloat = EditorGUILayout.FloatField(new GUIContent("Probability", "Probability"), oldValueFloat);
							// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
							if (Event.current.type == EventType.Repaint) {
								Rect lr = GUILayoutUtility.GetLastRect();
								currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
							}
							if (newValueFloat != oldValueFloat) {
								Undo.RecordObject(entry, "Probability");
								entry.FailedAncestorProbabbility[entry.TasksActivatedAfterFailed.IndexOf(task)] = newValueFloat;
								EditorUtility.SetDirty(entry);
							}
						}
					}

					GUI.color = Color.white;
					GUILayout.Label("-   -   -");


					GUI.color = ANDC;
					DrawMethodSelection(entry, "Action At Activate", ref entry.ScriptAtActivate, ref entry.MethodNameAtActivate);
					GUI.color = GA;
					DrawMethodSelection(entry, "Action At Success", ref entry.ScriptAtSuccess, ref entry.MethodNameAtSuccess);
					GUI.color = RA;
					DrawMethodSelection(entry, "Action At Failure", ref entry.ScriptAtFailure, ref entry.MethodNameAtFailure);
					GUI.color = YA;
					DrawMethodSelection(entry, "Action At Disable", ref entry.ScriptAtDisable, ref entry.MethodNameAtDisable);

				} else if (node is SuccessEnd) {
					SuccessEnd entry = node as SuccessEnd;

					GUI.color = TSDC;
					GUILayout.Label("Short Description");
					oldText = entry.ShortDescription;
					newText = EditorGUILayout.TextArea(node.GetShortDescription(), GUILayout.MinHeight(oneLineHeight * 3 + pad));
					// Remember the rect from the previously created textarea component to
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					// Just change the text if the textarea has been changed.
					if (newText != oldText) {
						Undo.RecordObject(entry, "Text changed");
						entry.ShortDescription = newText;
						EditorUtility.SetDirty(entry);
					}

					// Stop task system
					bool oldValueBool = entry.StopTaskSystem;
					bool newValueBool = EditorGUILayout.Toggle(new GUIContent("Stop task system", "Stop task system"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Stop task system");
						entry.StopTaskSystem = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					GUI.color = ANDC;
					DrawMethodSelection(entry, "Action At Activate", ref entry.ScriptAtActivate, ref entry.MethodNameAtActivate);

				} else if (node is FailureEnd) {
					FailureEnd entry = node as FailureEnd;

					GUI.color = TSDC;
					GUILayout.Label("Short Description");
					oldText = entry.ShortDescription;
					newText = EditorGUILayout.TextArea(node.GetShortDescription(), GUILayout.MinHeight(oneLineHeight * 3 + pad));
					// Remember the rect from the previously created textarea component to
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					// Just change the text if the textarea has been changed.
					if (newText != oldText) {
						Undo.RecordObject(entry, "Text changed");
						entry.ShortDescription = newText;
						EditorUtility.SetDirty(entry);
					}

					// Stop task system
					bool oldValueBool = entry.StopTaskSystem;
					bool newValueBool = EditorGUILayout.Toggle(new GUIContent("Stop task system", "Stop task system"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Stop task system");
						entry.StopTaskSystem = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					GUI.color = Color.white;
					GUILayout.Label("-   -   -");

					GUI.color = ANDC;
					DrawMethodSelection(entry, "Action At Activate", ref entry.ScriptAtActivate, ref entry.MethodNameAtActivate);

				} else if (node is ConditionUser) {
					ConditionUser entry = node as ConditionUser;
					drawConditionUser(entry, cr, oneLineHeight, pad);

					// This is not yet supported. If the condition will be extracted, then it must be on an own game object,
					// else after deleting the task or the conditions, all the nodes will be removed, because they are all
					// on the same game object.
					/*
                    GUI.color = Color.white;
                    GUILayout.Label("-   -   -");
                    // Move container to task
                    GUI.color = LBC;
                    if (GUILayout.Button(new GUIContent("Move User Condition to Task >", "Move User Condition to Task"))) {
                        Task[] tasks = CurrentAGM.GetComponentsInChildren<Task>();
                        if (tasks == null)
                            return;

                        Task[] uCondsTasks = Array.FindAll(tasks, item => item.Conditions.Contains(entry));
                        if (uCondsTasks.Length > 1) {
                            Debug.Log("ActivationGraphSystem: Hiding user condition just works with a single task reference.");
                            return;
                        }

                        if (uCondsTasks.Length == 0) {
                            Debug.Log("ActivationGraphSystem: The user condition is not connected with a task.");
                            return;
                        }

                        if (uCondsTasks[0] != null) {
                            Undo.RecordObject(entry, "User Condition Moved to Task");
                            uCondsTasks[0].ShowCondition(entry, false);
                            EditorUtility.SetDirty(entry);
                            return;
                        }
                    }
                    */

				} else if (node is ConditionArrival) {
					ConditionArrival entry = node as ConditionArrival;

					GUI.color = TSDC;
					GUILayout.Label("Short Description");
					oldText = entry.ShortDescription;
					newText = EditorGUILayout.TextArea(node.GetShortDescription(), GUILayout.MinHeight(oneLineHeight * 3 + pad));
					// Remember the rect from the previously created textarea component to
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					// Just change the text if the textarea has been changed.
					if (newText != oldText) {
						Undo.RecordObject(entry, "Text changed");
						entry.ShortDescription = newText;
						EditorUtility.SetDirty(entry);
					}

					// Is Trigger?
					GUI.color = Color.white;
					bool oldValueBool = entry.IsTrigger;
					bool newValueBool = EditorGUILayout.Toggle(new GUIContent("Is Trigger", "Is Trigger"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Is Trigger");
						entry.IsTrigger = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Enable Timer
					GUI.color = Color.white;
					oldValueBool = entry.EnableTimer;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Enable Timer", "Enable Timer"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Enable Timer");
						entry.EnableTimer = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					if (entry.EnableTimer) {
						// Time value field
						float oldValue = entry.TimerValue;
						float newValue = EditorGUILayout.FloatField(new GUIContent("  Time in sec.", "Timer value"), oldValue);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Timer value changed");
							entry.TimerValue = newValue;
							EditorUtility.SetDirty(entry);
						}
						GUILayout.Label("  Current Time in sec. " + entry.CurrentTimerValue);

						// Timer triggers failure
						GUI.color = RA;
						oldValueBool = entry.TimerSignalFailure;
						newValueBool = EditorGUILayout.Toggle(new GUIContent("  Timer triggers failure", "Timer triggers failure"), oldValueBool);
						if (newValueBool != oldValueBool) {
							Undo.RecordObject(entry, "Timer triggers failure");
							entry.TimerSignalFailure = newValueBool;
							EditorUtility.SetDirty(entry);
						}
					}


					GUI.color = Color.white;
					GUILayout.Label("Arriving objects");

					// Arriving objects
					List<Transform> oldValueList = entry.ArrivingObjects;
					for (int i = 0; i < oldValueList.Count; i++) {
						Transform oldValue = entry.ArrivingObjects[i];
						Transform newValue = EditorGUILayout.ObjectField(new GUIContent("Arriving objects", "Arriving objects"), oldValue, typeof(Transform), true) as Transform;
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Arriving objects changed");
							entry.ArrivingObjects[i] = newValue;
							EditorUtility.SetDirty(entry);
						}
					}

					// Add Entry
					GUI.color = GA;
					if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Add Entry", "Add Entry"))) {
						Undo.RecordObject(entry, "Arriving objects added");
						entry.ArrivingObjects.Add(null);
						EditorUtility.SetDirty(entry);
						return;
					}

					// Remove Last Entry
					GUI.color = RA;
					if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Remove Last Entry", "Remove Last Entry"))) {
						if (entry.ArrivingObjects.Count > 0) {
							Undo.RecordObject(entry, "Arriving objects removed");
							entry.ArrivingObjects.RemoveAt(entry.ArrivingObjects.Count - 1);
							EditorUtility.SetDirty(entry);
							return;
						}
					}

					// Triggers failure
					GUI.color = RA;
					oldValueBool = entry.TriggerFailure;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("  Triggers failure", "Timer triggers failure"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Triggers failure");
						entry.TriggerFailure = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					GUI.color = Color.white;
					GUILayout.Label("-   -   -");

					// Trigger success
					GUI.color = GA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Trigger success", "Trigger success"))) {
						Undo.RecordObject(entry, "Trigger success");
						entry.SetSuccessful();
						EditorUtility.SetDirty(entry);
					}

					// Trigger failure
					GUI.color = RA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Trigger failure", "Trigger failure"))) {
						Undo.RecordObject(entry, "Trigger failure");
						entry.SetFailed();
						EditorUtility.SetDirty(entry);
					}


					GUI.color = ANDC;
					DrawMethodSelection(entry, "Action At Activate", ref entry.ScriptAtActivate, ref entry.MethodNameAtActivate);
					GUI.color = GA;
					DrawMethodSelection(entry, "Action At Success", ref entry.ScriptAtSuccess, ref entry.MethodNameAtSuccess);
					GUI.color = RA;
					DrawMethodSelection(entry, "Action At Failure", ref entry.ScriptAtFailure, ref entry.MethodNameAtFailure);

				} else if (node is ConditionTimer) {
					ConditionTimer entry = node as ConditionTimer;

					GUI.color = TSDC;
					GUILayout.Label("Short Description");
					oldText = entry.ShortDescription;
					newText = EditorGUILayout.TextArea(node.GetShortDescription(), GUILayout.MinHeight(oneLineHeight * 3 + pad));
					// Remember the rect from the previously created textarea component to
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					// Just change the text if the textarea has been changed.
					if (newText != oldText) {
						Undo.RecordObject(entry, "Text changed");
						entry.ShortDescription = newText;
						EditorUtility.SetDirty(entry);
					}

					// Is Trigger?
					GUI.color = Color.white;
					bool oldValueBool = entry.IsTrigger;
					bool newValueBool = EditorGUILayout.Toggle(new GUIContent("Is Trigger", "Is Trigger"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Is Trigger");
						entry.IsTrigger = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Trigger failure
					GUI.color = RA;
					oldValueBool = entry.TriggerFailure;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Trigger failure", "Toggle trigger failure"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Toggle trigger failure");
						entry.TriggerFailure = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					GUI.color = ANDC;
					oldValueBool = entry.IsRandom;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Random time", "Random time"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Toggle random time");
						entry.IsRandom = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					if (entry.IsRandom) {
						float oldValue = entry.MinTime;
						float newValue = EditorGUILayout.FloatField(new GUIContent("Time min in sec.", "Timer value min"), oldValue);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Timer value min changed");
							entry.MinTime = newValue;
							EditorUtility.SetDirty(entry);
						}

						oldValue = entry.MaxTime;
						newValue = EditorGUILayout.FloatField(new GUIContent("Time max in sec.", "Timer value max"), oldValue);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Timer value max changed");
							entry.MaxTime = newValue;
							EditorUtility.SetDirty(entry);
						}
					} else {
						// Time value field
						GUI.color = GA;
						float oldValue = entry.TimerValue;
						float newValue = EditorGUILayout.FloatField(new GUIContent("Time in sec.", "Timer value"), oldValue);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Timer value changed");
							entry.TimerValue = newValue;
							EditorUtility.SetDirty(entry);
						}
					}

					GUI.color = Color.white;
					GUILayout.Label("  Current Time in sec. " + entry.CurrentTimerValue);


					GUILayout.Label("-   -   -");

					// Trigger success
					GUI.color = GA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Trigger success", "Trigger success"))) {
						Undo.RecordObject(entry, "Trigger success");
						entry.SetSuccessful();
						EditorUtility.SetDirty(entry);
					}

					// Trigger failure
					GUI.color = RA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Trigger failure", "Trigger failure"))) {
						Undo.RecordObject(entry, "Trigger failure");
						entry.SetFailed();
						EditorUtility.SetDirty(entry);
					}


					GUI.color = ANDC;
					DrawMethodSelection(entry, "Action At Activate", ref entry.ScriptAtActivate, ref entry.MethodNameAtActivate);
					GUI.color = GA;
					DrawMethodSelection(entry, "Action At Success", ref entry.ScriptAtSuccess, ref entry.MethodNameAtSuccess);
					GUI.color = RA;
					DrawMethodSelection(entry, "Action At Failure", ref entry.ScriptAtFailure, ref entry.MethodNameAtFailure);

				} else if (node is ConditionDefeat) {
					ConditionDefeat entry = node as ConditionDefeat;

					GUI.color = TSDC;
					GUILayout.Label("Short Description");
					oldText = entry.ShortDescription;
					newText = EditorGUILayout.TextArea(node.GetShortDescription(), GUILayout.MinHeight(oneLineHeight * 3 + pad));
					// Remember the rect from the previously created textarea component to
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					// Just change the text if the textarea has been changed.
					if (newText != oldText) {
						Undo.RecordObject(entry, "Text changed");
						entry.ShortDescription = newText;
						EditorUtility.SetDirty(entry);
					}

					// Is Trigger?
					GUI.color = Color.white;
					bool oldValueBool = entry.IsTrigger;
					bool newValueBool = EditorGUILayout.Toggle(new GUIContent("Is Trigger", "Is Trigger"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Is Trigger");
						entry.IsTrigger = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Enable Timer
					GUI.color = Color.white;
					oldValueBool = entry.EnableTimer;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Enable Timer", "Enable Timer"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Enable Timer");
						entry.EnableTimer = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					if (entry.EnableTimer) {
						// Time value field
						float oldValue = entry.TimerValue;
						float newValue = EditorGUILayout.FloatField(new GUIContent("  Time in sec.", "Timer value"), oldValue);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Timer value changed");
							entry.TimerValue = newValue;
							EditorUtility.SetDirty(entry);
						}
						GUILayout.Label("  Current Time in sec. " + entry.CurrentTimerValue);
					}

					GUI.color = Color.white;
					GUILayout.Label("Defeate objects");

					// Defeate objects
					List<Transform> oldValueList = entry.ObjectsToDefeate;
					for (int i = 0; i < oldValueList.Count; i++) {
						Transform oldValue = entry.ObjectsToDefeate[i];
						Transform newValue = EditorGUILayout.ObjectField(new GUIContent("Defeate objects", "Defeate objects"), oldValue, typeof(Transform), true) as Transform;
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Defeate objects changed");
							entry.ObjectsToDefeate[i] = newValue;
							EditorUtility.SetDirty(entry);
						}
					}

					// Add Entry
					GUI.color = GA;
					if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Add Entry", "Add Entry"))) {
						Undo.RecordObject(entry, "Defeate objects added");
						entry.ObjectsToDefeate.Add(null);
						EditorUtility.SetDirty(entry);
						return;
					}

					// Remove Last Entry
					GUI.color = RA;
					if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Remove Last Entry", "Remove Last Entry"))) {
						if (entry.ObjectsToDefeate.Count > 0) {
							Undo.RecordObject(entry, "Defeate objects removed");
							entry.ObjectsToDefeate.RemoveAt(entry.ObjectsToDefeate.Count - 1);
							EditorUtility.SetDirty(entry);
							return;
						}
					}

					// Defeat at least
					GUI.color = Color.white;
					int oldValueInt = entry.DefeateAtLeast;
					int newValueInt = EditorGUILayout.IntField(new GUIContent("Defeat at least", "Defeat at least"), oldValueInt);
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					if (newValueInt != oldValueInt) {
						Undo.RecordObject(entry, "Defeat at least");
						entry.DefeateAtLeast = newValueInt;
						EditorUtility.SetDirty(entry);
					}

					GUI.color = Color.white;
					GUILayout.Label("-   -   -");

					// Trigger success
					GUI.color = GA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Trigger success", "Trigger success"))) {
						Undo.RecordObject(entry, "Trigger success");
						entry.SetSuccessful();
						EditorUtility.SetDirty(entry);
					}

					// Trigger failure
					GUI.color = RA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Trigger failure", "Trigger failure"))) {
						Undo.RecordObject(entry, "Trigger failure");
						entry.SetFailed();
						EditorUtility.SetDirty(entry);
					}


					GUI.color = ANDC;
					DrawMethodSelection(entry, "Action At Activate", ref entry.ScriptAtActivate, ref entry.MethodNameAtActivate);
					GUI.color = GA;
					DrawMethodSelection(entry, "Action At Success", ref entry.ScriptAtSuccess, ref entry.MethodNameAtSuccess);
					GUI.color = RA;
					DrawMethodSelection(entry, "Action At Failure", ref entry.ScriptAtFailure, ref entry.MethodNameAtFailure);

				} else if (node is ConditionSurvive) {
					ConditionSurvive entry = node as ConditionSurvive;

					GUI.color = TSDC;
					GUILayout.Label("Short Description");
					oldText = entry.ShortDescription;
					newText = EditorGUILayout.TextArea(node.GetShortDescription(), GUILayout.MinHeight(oneLineHeight * 3 + pad));
					// Remember the rect from the previously created textarea component to
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					// Just change the text if the textarea has been changed.
					if (newText != oldText) {
						Undo.RecordObject(entry, "Text changed");
						entry.ShortDescription = newText;
						EditorUtility.SetDirty(entry);
					}

					// Is Trigger?
					GUI.color = Color.white;
					bool oldValueBool = entry.IsTrigger;
					bool newValueBool = EditorGUILayout.Toggle(new GUIContent("Is Trigger", "Is Trigger"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Is Trigger");
						entry.IsTrigger = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Enable Timer
					GUI.color = Color.white;
					oldValueBool = entry.EnableTimer;
					newValueBool = EditorGUILayout.Toggle(new GUIContent("Enable Timer", "Enable Timer"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Enable Timer");
						entry.EnableTimer = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					if (entry.EnableTimer) {
						// Time value field
						float oldValue = entry.TimerValue;
						float newValue = EditorGUILayout.FloatField(new GUIContent("  Time in sec.", "Timer value"), oldValue);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Timer value changed");
							entry.TimerValue = newValue;
							EditorUtility.SetDirty(entry);
						}
						GUILayout.Label("  Current Time in sec. " + entry.CurrentTimerValue);
					}

					GUI.color = Color.white;
					GUILayout.Label("Survive objects");

					// Survive objects
					List<Transform> oldValueList = entry.ObjectsToSurvive;
					for (int i = 0; i < oldValueList.Count; i++) {
						Transform oldValue = entry.ObjectsToSurvive[i];
						Transform newValue = EditorGUILayout.ObjectField(new GUIContent("Survive objects", "Survive objects"), oldValue, typeof(Transform), true) as Transform;
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Survive objects changed");
							entry.ObjectsToSurvive[i] = newValue;
							EditorUtility.SetDirty(entry);
						}
					}

					// Add Entry
					GUI.color = GA;
					if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Add Entry", "Add Entry"))) {
						Undo.RecordObject(entry, "Survive objects added");
						entry.ObjectsToSurvive.Add(null);
						EditorUtility.SetDirty(entry);
						return;
					}

					// Remove Last Entry
					GUI.color = RA;
					if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Remove Last Entry", "Remove Last Entry"))) {
						if (entry.ObjectsToSurvive.Count > 0) {
							Undo.RecordObject(entry, "Survive objects removed");
							entry.ObjectsToSurvive.RemoveAt(entry.ObjectsToSurvive.Count - 1);
							EditorUtility.SetDirty(entry);
							return;
						}
					}

					GUI.color = Color.white;
					GUILayout.Label("-   -   -");

					// Trigger success
					GUI.color = GA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Trigger success", "Trigger success"))) {
						Undo.RecordObject(entry, "Trigger success");
						entry.SetSuccessful();
						EditorUtility.SetDirty(entry);
					}

					// Trigger failure
					GUI.color = RA;
					if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Trigger failure", "Trigger failure"))) {
						Undo.RecordObject(entry, "Trigger failure");
						entry.SetFailed();
						EditorUtility.SetDirty(entry);
					}


					GUI.color = ANDC;
					DrawMethodSelection(entry, "Action At Activate", ref entry.ScriptAtActivate, ref entry.MethodNameAtActivate);
					GUI.color = GA;
					DrawMethodSelection(entry, "Action At Success", ref entry.ScriptAtSuccess, ref entry.MethodNameAtSuccess);
					GUI.color = RA;
					DrawMethodSelection(entry, "Action At Failure", ref entry.ScriptAtFailure, ref entry.MethodNameAtFailure);

				} else if (node is Container) {
					Container entry = node as Container;

					GUI.color = TSDC;
					GUILayout.Label("Short Description");
					oldText = entry.ShortDescription;
					newText = EditorGUILayout.TextArea(node.GetShortDescription(), GUILayout.MinHeight(oneLineHeight * 3 + pad));
					// Remember the rect from the previously created textarea component to
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					// Just change the text if the textarea has been changed.
					if (newText != oldText) {
						Undo.RecordObject(entry, "Text changed");
						entry.ShortDescription = newText;
						EditorUtility.SetDirty(entry);
					}

					GUI.color = LBC;
					GUILayout.Label("Container Max Weight Limit");

					float oldValueF = entry.GlobalMaxWeight;
					float newValueF = EditorGUILayout.FloatField(oldValueF);
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					if (newValueF != oldValueF) {
						Undo.RecordObject(entry, "Container GlobalMaxWeight Changed");
						entry.GlobalMaxWeight = newValueF;
						EditorUtility.SetDirty(entry);
					}

					GUI.color = AGMPopupTextColor;
					GUILayout.Label("ITEMS");

					// Items
					List<ContainerItem> oldValueList = entry.Items;
					for (int i = 0; i < oldValueList.Count; i++) {


						EditorGUILayout.BeginVertical(GUILayout.Height(42));
						EditorGUILayout.BeginHorizontal();


						EditorGUILayout.BeginVertical();

						EditorGUILayout.BeginHorizontal();

						GUI.color = LOC;
						GUILayout.Label("Name");

						string oldValue = entry.Items[i].GetName();
						string newValue = EditorGUILayout.TextField(oldValue);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Item Name Changed");
							entry.SetItemName(entry.Items[i], newValue);
							EditorUtility.SetDirty(entry);
						}

						GUI.color = LBC;
						GUILayout.Label("Count");

						int oldValueI = entry.Items[i].Value;
						int newValueI = EditorGUILayout.IntField(oldValueI);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueI != oldValueI) {
							Undo.RecordObject(entry, "Item Value Changed");
							entry.Items[i].Value = newValueI;
							EditorUtility.SetDirty(entry);
						}

						EditorGUILayout.EndHorizontal();


						EditorGUILayout.BeginHorizontal();

						GUI.color = Color.white;
						GUILayout.Label("Weig.");

						oldValueF = entry.Items[i].Weight;
						newValueF = EditorGUILayout.FloatField(oldValueF);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueF != oldValueF) {
							Undo.RecordObject(entry, "Item Weight Changed");
							entry.Items[i].Weight = newValueF;
							EditorUtility.SetDirty(entry);
						}

						GUI.color = Color.white;
						GUILayout.Label("MaxW");

						oldValueF = entry.Items[i].MaxWeightLimit;
						newValueF = EditorGUILayout.FloatField(oldValueF);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueF != oldValueF) {
							Undo.RecordObject(entry, "Item MaxWeightLimit Changed");
							entry.Items[i].MaxWeightLimit = newValueF;
							EditorUtility.SetDirty(entry);
						}

						EditorGUILayout.EndHorizontal();
						EditorGUILayout.EndVertical();


						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.BeginVertical();

						// Move Up Item
						GUI.color = TDC;
						if (GUILayout.Button(new GUIContent("↑", "Move Up Item"))) {
							if (i < entry.Items.Count) {
								Undo.RecordObject(entry, "Move Up Item");
								if (i != 0) {
									ContainerItem ci = entry.Items[i];
									entry.RemoveItemAt(i);
									entry.InsertItem(i - 1, ci);
								}
								EditorUtility.SetDirty(entry);
								return;
							}
						}

						GUILayout.Space(-4);

						// Insert Item
						GUI.color = GA;
						if (GUILayout.Button(new GUIContent("+", "Insert Item"))) {
							if (i < entry.Items.Count) {
								Undo.RecordObject(entry, "Insert Item");
								entry.InsertItem(i + 1, new ContainerItem(entry.GetUniqeItemName(), 1, 1, 10));
								EditorUtility.SetDirty(entry);
								return;
							}
						}

						EditorGUILayout.EndVertical();

						EditorGUILayout.BeginVertical();

						// Move Down Item
						GUI.color = TDC;
						if (GUILayout.Button(new GUIContent("↓", "Move Down Item"))) {
							if (i < entry.Items.Count - 1) {
								Undo.RecordObject(entry, "Move Down Item");
								if (i < entry.Items.Count) {
									ContainerItem ci = entry.Items[i];
									entry.RemoveItemAt(i);
									entry.InsertItem(i + 1, ci);
								}
								EditorUtility.SetDirty(entry);
								return;
							}
						}

						GUILayout.Space(-4);

						// Remove Item
						GUI.color = RA;
						if (GUILayout.Button(new GUIContent("X", "Remove Item"))) {
							if (i < entry.Items.Count) {
								Undo.RecordObject(entry, "Item removed");
								entry.RemoveItemAt(i);
								EditorUtility.SetDirty(entry);
								return;
							}
						}

						EditorGUILayout.EndVertical();
						EditorGUILayout.EndHorizontal();

						// Whole line X end
						EditorGUILayout.EndHorizontal();
						// Whole line Y end
						EditorGUILayout.EndVertical();

					}

					// Add Entry
					GUI.color = GA;
					if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Add Item", "Add Item"))) {
						Undo.RecordObject(entry, "Item added");
						entry.AddItem(entry.GetUniqeItemName(), 1, 1, 10);
						EditorUtility.SetDirty(entry);
						return;
					}

					// Remove Last Entry
					GUI.color = RA;
					if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Remove Last Item", "Remove Last Item"))) {
						if (entry.Items.Count > 0) {
							Undo.RecordObject(entry, "Item removed");
							entry.RemoveItemAt(entry.Items.Count - 1);
							EditorUtility.SetDirty(entry);
							return;
						}
					}

					GUI.color = Color.white;
					GUILayout.Label("-   -   -");

					DrawMethodSelection(entry, "Action At Value Changed", ref entry.ScriptAtValueChanged, ref entry.MethodNameAtValueChanged);

				} else if (node is ContainerManager) {
					ContainerManager entry = node as ContainerManager;

					GUI.color = TSDC;
					GUILayout.Label("Short Description");
					oldText = entry.ShortDescription;
					newText = EditorGUILayout.TextArea(node.GetShortDescription(), GUILayout.MinHeight(oneLineHeight * 3 + pad));
					// Remember the rect from the previously created textarea component to
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					// Just change the text if the textarea has been changed.
					if (newText != oldText) {
						Undo.RecordObject(entry, "Text changed");
						entry.ShortDescription = newText;
						EditorUtility.SetDirty(entry);
					}

					GUI.color = AGMPopupTextColor;
					GUILayout.Label("CONTAINERS");



					EditorGUILayout.BeginHorizontal();

					// Allow distributing by weight in container with the same priority.
					GUI.color = TDC;

					GUILayout.Label("Weight Based Filling for Same Priorities", GUILayout.Width(200));

					bool oldValueBool = entry.WeightBasedFillingForSamePriorities;
					bool newValueBool = GUILayout.Toggle(oldValueBool, "", GUILayout.Width(16));
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(CurrentAGM, "Allow distributing by weight in container with the same priority");
						entry.WeightBasedFillingForSamePriorities = newValueBool;
						EditorUtility.SetDirty(CurrentAGM);
					}

					EditorGUILayout.EndHorizontal();

					GUILayout.Space(20);



					// Containers
					List<ContainerInfo> oldValueList = entry.ContainerInfos;
					for (int i = 0; i < oldValueList.Count; i++) {

						ContainerInfo contInfo = entry.ContainerInfos[i];

						EditorGUILayout.BeginVertical();

						EditorGUILayout.BeginHorizontal();

						GUI.color = TDC;
						GUILayout.Label("Name");

						string oldValue = entry.ContainerInfos[i].Name;
						string newValue = EditorGUILayout.TextField(oldValue);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Container Name Changed");
							entry.ContainerInfos[i].Name = newValue;
							EditorUtility.SetDirty(entry);
						}

						GUILayout.Space(60);

						// Move Up Container
						GUI.color = TDC;
						if (GUILayout.Button(new GUIContent("↑", "Move Up Container"), GUILayout.Width(20))) {
							if (i < entry.ContainerInfos.Count) {
								Undo.RecordObject(entry, "Move Up Container");
								if (i != 0) {
									entry.ContainerInfos.Insert(i - 1, entry.ContainerInfos[i]);
									entry.ContainerInfos.RemoveAt(i + 1);
								}
								EditorUtility.SetDirty(entry);
								return;
							}
						}

						GUILayout.Space(-4);

						// Move Down Container
						GUI.color = TDC;
						if (GUILayout.Button(new GUIContent("↓", "Move Down Container"), GUILayout.Width(20))) {
							if (i < entry.ContainerInfos.Count - 1) {
								Undo.RecordObject(entry, "Move Down Container");
								if (i < entry.ContainerInfos.Count) {
									entry.ContainerInfos.Insert(i + 2, entry.ContainerInfos[i]);
									entry.ContainerInfos.RemoveAt(i);
								}
								EditorUtility.SetDirty(entry);
								return;
							}
						}

						GUILayout.Space(-4);

						// Insert Container
						GUI.color = GA;
						if (GUILayout.Button(new GUIContent("+", "Insert Container"), GUILayout.Width(20))) {
							if (i < entry.ContainerInfos.Count) {
								Undo.RecordObject(entry, "Insert Item");
								entry.ContainerInfos.Insert(i + 1, new ContainerInfo("Container",
									entry.GenerateUniqueIncreasingPriority(0), entry.GenerateUniqueDecreasingPriority(0)));
								EditorUtility.SetDirty(entry);
								return;
							}
						}

						GUILayout.Space(-4);

						// Remove Container
						GUI.color = RA;
						if (GUILayout.Button(new GUIContent("X", "Remove Container"), GUILayout.Width(20))) {
							if (i < entry.ContainerInfos.Count) {
								Undo.RecordObject(entry, "Container removed");
								entry.ContainerInfos.RemoveAt(i);
								EditorUtility.SetDirty(entry);
								return;
							}
						}

						EditorGUILayout.EndHorizontal();

						GUILayout.Space(4);

						EditorGUILayout.BeginHorizontal();

						// Priority for increasing item values in container.
						GUI.color = GA;

						GUILayout.Label("Order +", GUILayout.Width(50));

						int oldValueI = contInfo.IncreasingPriority;
						int newValueI = EditorGUILayout.IntField(oldValueI);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueI != oldValueI) {
							Undo.RecordObject(entry, "Priority for Increasing Values Changed");
							contInfo.IncreasingPriority = newValueI;
							EditorUtility.SetDirty(entry);
						}

						// Lock increasing nodes
						GUI.color = GA;

						GUILayout.Label("Lock", GUILayout.Width(30));

						oldValueBool = contInfo.IsIncreasingLocked;
						newValueBool = GUILayout.Toggle(oldValueBool, "", GUILayout.Width(16));
						if (newValueBool != oldValueBool) {
							Undo.RecordObject(CurrentAGM, "Lock inserting values in container");
							contInfo.IsIncreasingLocked = newValueBool;
							EditorUtility.SetDirty(CurrentAGM);
						}

						// Priority for removing items in container.
						GUI.color = RA;

						GUILayout.Label("Order -", GUILayout.Width(50));

						// Priority for decreasing item values in container.
						oldValueI = contInfo.DecreasingPriority;
						newValueI = EditorGUILayout.IntField(oldValueI);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValueI != oldValueI) {
							Undo.RecordObject(entry, "Priority for Decreasing Values Changed");
							contInfo.DecreasingPriority = newValueI;
							EditorUtility.SetDirty(entry);
						}

						// Lock decreasing nodes
						GUI.color = RA;

						GUILayout.Label("Lock", GUILayout.Width(30));

						oldValueBool = contInfo.IsDecreasingLocked;
						newValueBool = GUILayout.Toggle(oldValueBool, "", GUILayout.Width(16));
						if (newValueBool != oldValueBool) {
							Undo.RecordObject(CurrentAGM, "Lock decreasing values in container");
							contInfo.IsDecreasingLocked = newValueBool;
							EditorUtility.SetDirty(CurrentAGM);
						}


						EditorGUILayout.EndHorizontal();

						if (entry.WeightBasedFillingForSamePriorities) {
							EditorGUILayout.BeginHorizontal();

							// Weight for increasing item values in container.
							GUI.color = GA;

							GUILayout.Label("Same Order Weight +", GUILayout.Width(110));

							oldValueI = contInfo.IncreasingWeight;
							newValueI = EditorGUILayout.IntField(oldValueI);
							// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
							if (Event.current.type == EventType.Repaint) {
								Rect lr = GUILayoutUtility.GetLastRect();
								currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
							}
							if (newValueI != oldValueI) {
								Undo.RecordObject(entry, "Weight for Increasing Values Changed");
								contInfo.IncreasingWeight = newValueI;
								EditorUtility.SetDirty(entry);
							}

							// Weight for removing items in container.
							GUI.color = RA;

							GUILayout.Label("Same Order Weight -", GUILayout.Width(110));

							// Weight for decreasing item values in container.
							oldValueI = contInfo.DecreasingWeight;
							newValueI = EditorGUILayout.IntField(oldValueI);
							// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
							if (Event.current.type == EventType.Repaint) {
								Rect lr = GUILayoutUtility.GetLastRect();
								currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
							}
							if (newValueI != oldValueI) {
								Undo.RecordObject(entry, "Weight for Decreasing Values Changed");
								contInfo.DecreasingWeight = newValueI;
								EditorUtility.SetDirty(entry);
							}

							EditorGUILayout.EndHorizontal();
						}

						GUILayout.Space(20);

						EditorGUILayout.EndVertical();
					}

					// Add Entry
					GUI.color = GA;
					if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Add Container", "Add Container"))) {
						Undo.RecordObject(entry, "Container added");
						entry.ContainerInfos.Add(new ContainerInfo("Container",
							entry.GenerateUniqueIncreasingPriority(0), entry.GenerateUniqueDecreasingPriority(0)));
						EditorUtility.SetDirty(entry);
						return;
					}

					// Remove Last Entry
					GUI.color = RA;
					if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Remove Last Container", "Remove Last Container"))) {
						if (entry.ContainerInfos.Count > 0) {
							Undo.RecordObject(entry, "Container removed");
							entry.ContainerInfos.RemoveAt(entry.ContainerInfos.Count - 1);
							EditorUtility.SetDirty(entry);
							return;
						}
					}

					GUI.color = Color.white;
					GUILayout.Label("-   -   -");

					DrawMethodSelection(entry, "Action At Value Changed", ref entry.ScriptAtValueChanged, ref entry.MethodNameAtValueChanged);
				}

			} else {

				// Sometimes the EditorGUILayout.BeginHorizontal() fails, simply ignore
				// the update and paint it at the new update, which comes in periords.
				try {

					// Show then the manager reletad values.
					GUI.color = LOC;
					GUILayout.Label("MANAGER");
					GUILayout.Label("Game Object Name");

					string oldValue = CurrentAGM.gameObject.name;
					string newValue = EditorGUILayout.TextField(oldValue);
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					if (newValue != oldValue) {
						Undo.RecordObject(CurrentAGM, "Container Name Changed");
						CurrentAGM.gameObject.name = newValue;
						EditorUtility.SetDirty(CurrentAGM);
					}

					GUI.color = TSDC;
					GUILayout.Label("Short Description");
					string oldText = CurrentAGM.ShortDescription;
					string newText = EditorGUILayout.TextArea(CurrentAGM.ShortDescription, GUILayout.MinHeight(oneLineHeight * 3 + pad));
					// Remember the rect from the previously created textarea component to
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					// Just change the text if the textarea has been changed.
					if (newText != oldText) {
						Undo.RecordObject(CurrentAGM, "Text changed");
						CurrentAGM.ShortDescription = newText;
						EditorUtility.SetDirty(CurrentAGM);
					}

					GUI.color = AGMPopupTextColor;
					GUILayout.Label("DATA TASKS");

					// Add/Refresh All Data Tasks
					GUI.color = GA;
					if (GUILayout.Button(new GUIContent("Add/Refresh All Data Tasks", "Add/Refresh All Data Tasks"))) {
						Undo.RecordObject(CurrentAGM, "Add/Refresh All Data Tasks");
						CurrentAGM.CreateTaskOrder();
						EditorUtility.SetDirty(CurrentAGM);
						return;
					}

					// Tasks
					List<TaskDataNode> oldValueList = CurrentAGM.Tasks;
					for (int i = 0; i < oldValueList.Count; i++) {

						EditorGUILayout.BeginHorizontal();

						GUI.color = LOC;
						GUILayout.Label("Name");

						oldValue = CurrentAGM.Tasks[i].gameObject.name;
						newValue = EditorGUILayout.TextField(oldValue);
						// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
						if (Event.current.type == EventType.Repaint) {
							Rect lr = GUILayoutUtility.GetLastRect();
							currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
						}
						if (newValue != oldValue) {
							Undo.RecordObject(CurrentAGM, "Item Name Changed");
							CurrentAGM.Tasks[i].gameObject.name = newValue;
							EditorUtility.SetDirty(CurrentAGM);
						}

						// Set in Active Mode
						GUI.color = GA;
						bool oldValueBool = CurrentAGM.IsInActiveMode(CurrentAGM.Tasks[i]);
						bool newValueBool = GUILayout.Toggle(oldValueBool, "", GUILayout.Width(16));
						if (newValueBool != oldValueBool) {
							Undo.RecordObject(CurrentAGM, "Toggle Set in Active Mode");
							CurrentAGM.SetInActiveMode(CurrentAGM.Tasks[i], newValueBool);
							EditorUtility.SetDirty(CurrentAGM);
						}

						// Move Up Item
						GUI.color = LOC;
						if (GUILayout.Button(new GUIContent("↑", "Move Up Item"), GUILayout.Width(20))) {
							if (i < CurrentAGM.Tasks.Count) {
								Undo.RecordObject(CurrentAGM, "Move Up Item");
								if (i != 0) {
									CurrentAGM.Tasks.Insert(i - 1, CurrentAGM.Tasks[i]);
									CurrentAGM.Tasks.RemoveAt(i + 1);
								}
								EditorUtility.SetDirty(CurrentAGM);
								return;
							}
						}

						// Move Down Item
						GUI.color = LOC;
						if (GUILayout.Button(new GUIContent("↓", "Move Down Item"), GUILayout.Width(20))) {
							if (i < CurrentAGM.Tasks.Count - 1) {
								Undo.RecordObject(CurrentAGM, "Move Down Item");
								if (i < CurrentAGM.Tasks.Count) {
									CurrentAGM.Tasks.Insert(i + 2, CurrentAGM.Tasks[i]);
									CurrentAGM.Tasks.RemoveAt(i);
								}
								EditorUtility.SetDirty(CurrentAGM);
								return;
							}
						}

						// Remove Task
						GUI.color = RA;
						if (GUILayout.Button(new GUIContent("X", "Remove Item"), GUILayout.Width(20))) {
							if (i < CurrentAGM.Tasks.Count) {
								Undo.RecordObject(CurrentAGM, "Item removed");
								CurrentAGM.Tasks.RemoveAt(i);
								EditorUtility.SetDirty(CurrentAGM);
								return;
							}
						}
						EditorGUILayout.EndHorizontal();
					}

					GUI.color = Color.white;
					GUILayout.Label("-   -   -");

				} catch {
					EditorGUILayout.EndScrollView();
					return;
				}
			}

			EditorGUILayout.EndScrollView();
		}

		/// <summary>
		/// The user condition inspector view is needed by the task too, so it gets it own method.
		/// </summary>
		/// <param name="entry"></param>
		void drawConditionUser(ConditionUser entry, Rect cr, float oneLineHeight, float pad) {

			GUI.color = TSDC;
			GUILayout.Label("Short Description");
			string oldText = entry.ShortDescription;
			string newText = EditorGUILayout.TextArea(entry.GetShortDescription(), GUILayout.MinHeight(oneLineHeight * 3 + pad));
			// Remember the rect from the previously created textarea component to
			// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
			if (Event.current.type == EventType.Repaint) {
				Rect lr = GUILayoutUtility.GetLastRect();
				currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
			}
			// Just change the text if the textarea has been changed.
			if (newText != oldText) {
				Undo.RecordObject(entry, "Text changed");
				entry.ShortDescription = newText;
				EditorUtility.SetDirty(entry);
			}

			if (EditorApplication.isPlaying) {
				GUI.color = TDC;
				GUILayout.Label("STATE: " + entry.Result.ToString());
			}

			// Is Trigger?
			GUI.color = Color.white;
			bool oldValueBool = entry.IsTrigger;
			bool newValueBool = EditorGUILayout.Toggle(new GUIContent("Is Trigger", "Is Trigger"), oldValueBool);
			if (newValueBool != oldValueBool) {
				Undo.RecordObject(entry, "Is Trigger");
				entry.IsTrigger = newValueBool;
				EditorUtility.SetDirty(entry);
			}

			// Enable Timer
			GUI.color = Color.white;
			oldValueBool = entry.EnableTimer;
			newValueBool = EditorGUILayout.Toggle(new GUIContent("Enable Timer", "Enable Timer"), oldValueBool);
			if (newValueBool != oldValueBool) {
				Undo.RecordObject(entry, "Enable Timer");
				entry.EnableTimer = newValueBool;
				EditorUtility.SetDirty(entry);
			}

			if (entry.EnableTimer) {
				// Time value field
				float oldValue = entry.TimerValue;
				float newValue = EditorGUILayout.FloatField(new GUIContent("  Time in sec.", "Timer value"), oldValue);
				// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
				if (Event.current.type == EventType.Repaint) {
					Rect lr = GUILayoutUtility.GetLastRect();
					currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
				}
				if (newValue != oldValue) {
					Undo.RecordObject(entry, "Timer value changed");
					entry.TimerValue = newValue;
					EditorUtility.SetDirty(entry);
				}
				GUILayout.Label("  Current Time in sec. " + entry.CurrentTimerValue);

				// Timer triggers failure
				GUI.color = RA;
				oldValueBool = entry.TimerSignalFailure;
				newValueBool = EditorGUILayout.Toggle(new GUIContent("  Timer triggers failure", "Timer triggers failure"), oldValueBool);
				if (newValueBool != oldValueBool) {
					Undo.RecordObject(entry, "Timer triggers failure");
					entry.TimerSignalFailure = newValueBool;
					EditorUtility.SetDirty(entry);
				}
			}

			// Enable Counter
			GUI.color = Color.white;
			oldValueBool = entry.EnableCounter;
			newValueBool = EditorGUILayout.Toggle(new GUIContent("Enable Counter", "Enable Counter"), oldValueBool);
			if (newValueBool != oldValueBool) {
				Undo.RecordObject(entry, "Enable Counter");
				entry.EnableCounter = newValueBool;
				EditorUtility.SetDirty(entry);
			}

			if (entry.EnableCounter) {
				// Counter value
				int oldValueInt = entry.CounterValue;
				int newValueInt = EditorGUILayout.IntField(new GUIContent("  Counter value", "Counter value"), oldValueInt);
				// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
				if (Event.current.type == EventType.Repaint) {
					Rect lr = GUILayoutUtility.GetLastRect();
					currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
				}
				if (newValueInt != oldValueInt) {
					Undo.RecordObject(entry, "Counter value changed");
					entry.CounterValue = newValueInt;
					EditorUtility.SetDirty(entry);
				}

				// Counter current value
				oldValueInt = entry.CurrentCounterValue;
				newValueInt = EditorGUILayout.IntField(new GUIContent("  Counter current value", "Counter current value"), oldValueInt);
				// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
				if (Event.current.type == EventType.Repaint) {
					Rect lr = GUILayoutUtility.GetLastRect();
					currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
				}
				if (newValueInt != oldValueInt) {
					Undo.RecordObject(entry, "Counter current value");
					entry.CurrentCounterValue = newValueInt;
					EditorUtility.SetDirty(entry);
				}

				// Counter triggers failure
				GUI.color = RA;
				oldValueBool = entry.CounterSignalFailure;
				newValueBool = EditorGUILayout.Toggle(new GUIContent("  Counter triggers failure", "Counter triggers failure"), oldValueBool);
				if (newValueBool != oldValueBool) {
					Undo.RecordObject(entry, "Counter triggers failure");
					entry.CounterSignalFailure = newValueBool;
					EditorUtility.SetDirty(entry);
				}
			}

			// Enable Enable Container Access
			GUI.color = Color.white;
			oldValueBool = entry.EnableContainerAccess;
			newValueBool = EditorGUILayout.Toggle(new GUIContent("Enable Container Access", "Enable Container Access"), oldValueBool);
			if (newValueBool != oldValueBool) {
				Undo.RecordObject(entry, "Enable Container Access");
				entry.EnableContainerAccess = newValueBool;
				EditorUtility.SetDirty(entry);
			}

			if (entry.EnableContainerAccess) {

				GUI.color = LOC;
				GUILayout.Label("Container Name");

				string oldValue = entry.ContainerName;
				string newValue = EditorGUILayout.TextField(oldValue);
				// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
				if (Event.current.type == EventType.Repaint) {
					Rect lr = GUILayoutUtility.GetLastRect();
					currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
				}
				if (newValue != oldValue) {
					Undo.RecordObject(entry, "Container Name Changed");
					entry.ContainerName = newValue;
					EditorUtility.SetDirty(entry);
				}

				GUI.color = AGMPopupTextColor;
				GUILayout.Label("ITEMS");

				// Items
				List<ItemRef> oldValueList = entry.Items;
				for (int i = 0; i < oldValueList.Count; i++) {

					EditorGUILayout.BeginHorizontal();

					GUI.color = LOC;
					GUILayout.Label("Name");

					oldValue = entry.Items[i].GetName();
					newValue = EditorGUILayout.TextField(oldValue);
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					if (newValue != oldValue) {
						Undo.RecordObject(entry, "Item Name Changed");
						entry.SetItemName(entry.Items[i], newValue);
						EditorUtility.SetDirty(entry);
					}

					GUI.color = LBC;
					GUILayout.Label("Count");

					int oldValueI = entry.Items[i].Value;
					int newValueI = EditorGUILayout.IntField(oldValueI);
					// deactivate the focus, when the mouse not hover over the textarea. \sa mouse events
					if (Event.current.type == EventType.Repaint) {
						Rect lr = GUILayoutUtility.GetLastRect();
						currentTextFields.Add(new Rect(lr.x + cr.width, lr.y - scrollPosInspector.y, lr.width, lr.height));
					}
					if (newValueI != oldValueI) {
						Undo.RecordObject(entry, "Item Value Changed");
						entry.Items[i].Value = newValueI;
						EditorUtility.SetDirty(entry);
					}

					// Do consume?
					GUI.color = GA;
					oldValueBool = entry.Items[i].DoConsume;
					newValueBool = GUILayout.Toggle(oldValueBool, "", GUILayout.Width(16));
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Consume item value?");
						entry.Items[i].DoConsume = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Do consume manually?
					GUI.color = TDC;
					oldValueBool = entry.Items[i].DoConsumeManually;
					newValueBool = GUILayout.Toggle(oldValueBool, "", GUILayout.Width(16));
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Consume item value later in user code?");
						entry.Items[i].DoConsumeManually = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Move Up Item
					GUI.color = TDC;
					if (GUILayout.Button(new GUIContent("↑", "Move Up Item"))) {
						if (i < entry.Items.Count) {
							Undo.RecordObject(entry, "Move Up Item");
							if (i != 0) {
								// Does not need dictionary synchronization.
								ItemRef ir = entry.Items[i];
								entry.RemoveItemAt(i);
								entry.InsertItem(i - 1, ir);
							}
							EditorUtility.SetDirty(entry);
							return;
						}
					}

					GUILayout.Space(-4);

					// Move Down Item
					GUI.color = TDC;
					if (GUILayout.Button(new GUIContent("↓", "Move Down Item"))) {
						if (i < entry.Items.Count - 1) {
							Undo.RecordObject(entry, "Move Down Item");
							if (i < entry.Items.Count) {
								// Does not need dictionary synchronization.
								ItemRef ir = entry.Items[i];
								entry.RemoveItemAt(i);
								entry.InsertItem(i + 1, ir);
							}
							EditorUtility.SetDirty(entry);
							return;
						}
					}

					GUILayout.Space(-4);

					// Insert Item
					GUI.color = GA;
					if (GUILayout.Button(new GUIContent("+", "Insert Item"))) {
						if (i < entry.Items.Count) {
							Undo.RecordObject(entry, "Insert Item");
							entry.InsertItem(i + 1, new ItemRef(entry.GetUniqeItemName(), -1, true, false));
							EditorUtility.SetDirty(entry);
							return;
						}
					}

					GUILayout.Space(-4);

					// Remove Item
					GUI.color = RA;
					if (GUILayout.Button(new GUIContent("X", "Remove Item"))) {
						if (i < entry.Items.Count) {
							Undo.RecordObject(entry, "Item removed");
							entry.RemoveItemAt(i);
							EditorUtility.SetDirty(entry);
							return;
						}
					}

					EditorGUILayout.EndHorizontal();

				}


				// Add Entry
				GUI.color = GA;
				if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Add Item", "Add Item"))) {
					Undo.RecordObject(entry, "Item added");
					entry.AddItem(entry.GetUniqeItemName(), -1, true, false);
					EditorUtility.SetDirty(entry);
					return;
				}

				// Remove Last Entry
				GUI.color = RA;
				if (!EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Remove Last Item", "Remove Last Item"))) {
					if (entry.Items.Count > 0) {
						Undo.RecordObject(entry, "Item removed");
						entry.RemoveItemAt(entry.Items.Count - 1);
						EditorUtility.SetDirty(entry);
						return;
					}
				}
			}



			GUI.color = Color.white;
			GUILayout.Label("-   -   -");

			// Trigger success
			GUI.color = GA;
			if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Trigger success", "Trigger success"))) {
				Undo.RecordObject(entry, "Trigger success");
				entry.SetSuccessful();
				EditorUtility.SetDirty(entry);
			}

			// Trigger failure
			GUI.color = RA;
			if (EditorApplication.isPlaying && GUILayout.Button(new GUIContent("Trigger failure", "Trigger failure"))) {
				Undo.RecordObject(entry, "Trigger failure");
				entry.SetFailed();
				EditorUtility.SetDirty(entry);
			}


			GUI.color = ANDC;
			DrawMethodSelection(entry, "Action At Activate", ref entry.ScriptAtActivate, ref entry.MethodNameAtActivate);
			GUI.color = GA;
			DrawMethodSelection(entry, "Action At Success", ref entry.ScriptAtSuccess, ref entry.MethodNameAtSuccess);
			GUI.color = RA;
			DrawMethodSelection(entry, "Action At Failure", ref entry.ScriptAtFailure, ref entry.MethodNameAtFailure);
		}

		/// <summary>
		/// Helper method for drawing and changing the trigger components.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="title"></param>
		/// <param name="script"></param>
		/// <param name="methodName"></param>
		private void DrawMethodSelection(BaseNode entry, string title, ref MonoBehaviour script, ref string methodName) {
			GUILayout.Label(title);

			GameObject oldGo = script != null ? script.gameObject : null;
			GameObject selectedGameObject = EditorGUILayout.ObjectField("GameObject ", oldGo, typeof(GameObject), true) as GameObject;
			if (oldGo != selectedGameObject) {
				Undo.RecordObject(entry, "Trigger game object modified");
				script = null;
				EditorUtility.SetDirty(entry);
			}

			int selectedScriptIndex = 0;
			if (selectedGameObject) {
				MonoBehaviour[] comps = GetComps(selectedGameObject);
				int i = 0;
				List<string> comNames = new List<string>();
				foreach (MonoBehaviour mb in comps) {
					comNames.Add(mb.GetType().Name);
					if (script != null && mb.GetType().Name == script.GetType().Name) {
						selectedScriptIndex = i;
					}
					i++;
				}

				string[] v = comNames.ToArray();

				Component oldComp = script;
				selectedScriptIndex = EditorGUILayout.Popup("Script", selectedScriptIndex, v);
				if (comps.Length != 0 && oldComp != comps[selectedScriptIndex]) {
					Undo.RecordObject(entry, "Trigger script modified");
					script = comps[selectedScriptIndex];
					methodName = "";
					EditorUtility.SetDirty(entry);
				}

				int selectedMethodIndex = 0;
				if (comps.Length > selectedScriptIndex && comps[selectedScriptIndex]) {
					List<MethodInfo> mis = GetMethods(comps[selectedScriptIndex]);
					List<string> methodNames = new List<string>();
					int j = 0;
					methodNames.Add("<Invalid>");
					selectedMethodIndex = 0;
					j++;
					foreach (MethodInfo method in mis) {
						methodNames.Add(method.Name);
						if (method.Name == methodName) {
							selectedMethodIndex = j;
						}
						j++;
					}
					string[] mNames = methodNames.ToArray();

					string oldName = methodName;
					selectedMethodIndex = EditorGUILayout.Popup("Method", selectedMethodIndex, mNames);
					if (oldName != mNames[selectedMethodIndex]) {
						Undo.RecordObject(entry, "Trigger method modified");
						methodName = mNames[selectedMethodIndex];
						EditorUtility.SetDirty(entry);
					}
				}
			}
		}

		float taskBoxWidth = 22;
		float taskBoxHeight = 18;
		float lampTaskBoxWidth = 10;
		float lampTaskBoxHeight = 16;
		float lampOpBoxWidth = 8;
		float lampOpBoxHeight = 16;
		float lampEndBoxWidth = 40;
		float lampEndBoxHeight = 12;
		float lampCondBoxWidth = 18;
		float lampCondBoxHeight = 9;

		/// <summary>
		/// The callback method for the node "windows". The nodes will be painted here.
		/// </summary>
		/// <param name="id"></param>
		private void DrawNodeWindow(int id) {

			// Else, layout cannot be shown correctly.
			if (Event.current.type == EventType.MouseDown && !mainRect.Contains(currentMousePos)) {
				return;
			}

			// After removing a node this update can still happen.
			// Just skip painting.
			if (id >= nodes.Count)
				return;

			GUI.skin = guiSkin;

			// Undo/redo for the position of the window.
			NodeBase node = nodes[id];
			if (node == null || node.TheBaseNode == null)
				return;
			Vector2 currentPos = new Vector2(node.NodeRect.xMin, node.NodeRect.yMin);
			if (node.TheBaseNode.PosInNodeEditor != currentPos) {
				Undo.RecordObject(node.TheBaseNode, "Position changed");
				node.TheBaseNode.PosInNodeEditor = currentPos;
				EditorUtility.SetDirty(node.TheBaseNode);
			}

			float cornX = node.NodeRect.width;

			GUI.skin.label.fontSize = 9;

			if (node.TheBaseNode is TaskBase) {
				if (node.TheBaseNode is TaskDataNode) {
					Task entry = node.TheBaseNode as Task;

					EditorGUI.DrawRect(new Rect(6, 6, node.NodeRect.width - 12, node.NodeRect.height - 12), TaskBg);

					GUI.color = TC;
					GUI.Label(new Rect(12, 4, cornX - 18, 34), node.TheBaseNode.name);

					if (entry.Result != Task.TaskResult.InactiveType) {
						GUI.color = MissA;
						GUI.DrawTexture(new Rect(8, 35, taskBoxWidth, taskBoxHeight), BoxSuccess2);
					} else {
						GUI.color = MissN;
						GUI.DrawTexture(new Rect(8, 35, taskBoxWidth, taskBoxHeight), Box);
					}


					if (entry.Result == Task.TaskResult.RunningType) {
						GUI.color = YA;
						GUI.DrawTexture(new Rect(44, 36, lampTaskBoxWidth, lampTaskBoxHeight), BoxSuccess);
					} else {
						GUI.color = YN;
						GUI.DrawTexture(new Rect(44, 36, lampTaskBoxWidth, lampTaskBoxHeight), Box);
					}

					if (entry.Result == Task.TaskResult.SuccessType) {
						GUI.color = GA;
						GUI.DrawTexture(new Rect(57, 36, lampTaskBoxWidth, lampTaskBoxHeight), BoxSuccess);
					} else {
						GUI.color = GN;
						GUI.DrawTexture(new Rect(57, 36, lampTaskBoxWidth, lampTaskBoxHeight), Box);
					}

					if (entry.Result == Task.TaskResult.FailureType) {
						GUI.color = RA;
						GUI.DrawTexture(new Rect(70, 36, lampTaskBoxWidth, lampTaskBoxHeight), BoxSuccess);
					} else {
						GUI.color = RN;
						GUI.DrawTexture(new Rect(70, 36, lampTaskBoxWidth, lampTaskBoxHeight), Box);
					}


					// Set in Active Mode
					GUI.color = GA2;
					bool oldValue = CurrentAGM.IsInActiveMode(entry);
					bool newValue = EditorGUI.Toggle(new Rect(-1, -2, 16, 16), new GUIContent("", "Toggle Set in Active Mode"), oldValue);
					if (newValue != oldValue) {
						Undo.RecordObject(CurrentAGM, "Toggle Set in Active Mode");
						CurrentAGM.SetInActiveMode(entry, newValue);
						EditorUtility.SetDirty(CurrentAGM);
					}

				} else if (node.TheBaseNode is OperatorNode) {
					OperatorNode entry = node.TheBaseNode as OperatorNode;

					EditorGUI.DrawRect(new Rect(6, 6, node.NodeRect.width - 12, node.NodeRect.height - 12), OpBg);

					if (entry.Result == Task.TaskResult.RunningType) {
						GUI.color = YA;
						GUI.DrawTexture(new Rect(8, 17, lampOpBoxWidth, lampOpBoxHeight), BoxSuccess);
					} else {
						GUI.color = YN;
						GUI.DrawTexture(new Rect(8, 17, lampOpBoxWidth, lampOpBoxHeight), Box);
					}

					if (entry.Result == Task.TaskResult.SuccessType) {
						GUI.color = GA;
						GUI.DrawTexture(new Rect(18, 17, lampOpBoxWidth, lampOpBoxHeight), BoxSuccess);
					} else {
						GUI.color = GN;
						GUI.DrawTexture(new Rect(18, 17, lampOpBoxWidth, lampOpBoxHeight), Box);
					}

					if (entry.Result == Task.TaskResult.FailureType) {
						GUI.color = RA;
						GUI.DrawTexture(new Rect(28, 17, lampOpBoxWidth, lampOpBoxHeight), BoxSuccess);
					} else {
						GUI.color = RN;
						GUI.DrawTexture(new Rect(28, 17, lampOpBoxWidth, lampOpBoxHeight), Box);
					}


					// Set in Active Mode
					GUI.color = GA2;
					bool oldValue = CurrentAGM.IsInActiveMode(entry);
					bool newValue = EditorGUI.Toggle(new Rect(-1, -2, 16, 16), new GUIContent("", "Toggle Set in Active Mode"), oldValue);
					if (newValue != oldValue) {
						Undo.RecordObject(CurrentAGM, "Toggle Set in Active Mode");
						CurrentAGM.SetInActiveMode(entry, newValue);
						EditorUtility.SetDirty(CurrentAGM);
					}

					// Toggle and feature
					GUI.color = ANDC;
					oldValue = entry.IsAndNode;
					newValue = EditorGUI.Toggle(new Rect(-1, 34, 16, 16), new GUIContent("", "Toggle and feature"), oldValue);
					if (newValue != oldValue) {
						Undo.RecordObject(entry, "Toggle and feature");
						entry.IsAndNode = newValue;
						EditorUtility.SetDirty(entry);
					}

					// Trigger failure
					GUI.color = RA;
					bool oldValueBool = entry.TriggerFailure;
					bool newValueBool = EditorGUI.Toggle(new Rect(12, 34, 16, 16), new GUIContent("", "Toggle trigger failure"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Toggle trigger failure");
						entry.TriggerFailure = newValueBool;
						EditorUtility.SetDirty(entry);
					}

					// Trigger success on concurrent tasks
					GUI.color = GA;
					oldValueBool = entry.TriggerSucccessConcurrents;
					newValueBool = EditorGUI.Toggle(new Rect(37, -3, 16, 16), new GUIContent("", "Toggle success on concurrent tasks"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Toggle success on concurrent tasks");
						entry.TriggerSucccessConcurrents = newValueBool;
						if (newValueBool) {
							entry.TriggerFailureConcurrents = false;
							entry.TriggerInactiveConcurrents = false;
						}
						EditorUtility.SetDirty(entry);
					}

					// Trigger failure on concurrent tasks
					GUI.color = RA;
					oldValueBool = entry.TriggerFailureConcurrents;
					newValueBool = EditorGUI.Toggle(new Rect(37, 10, 16, 16), new GUIContent("", "Toggle failure on concurrent tasks"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Toggle failure on concurrent tasks");
						entry.TriggerFailureConcurrents = newValueBool;
						if (newValueBool) {
							entry.TriggerSucccessConcurrents = false;
							entry.TriggerInactiveConcurrents = false;
						}
						EditorUtility.SetDirty(entry);
					}

					// Trigger inactive on concurrent tasks
					GUI.color = YA;
					oldValueBool = entry.TriggerInactiveConcurrents;
					newValueBool = EditorGUI.Toggle(new Rect(37, 23, 16, 16), new GUIContent("", "Toggle inactive on concurrent tasks"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Toggle inactive on concurrent tasks");
						entry.TriggerInactiveConcurrents = newValueBool;
						if (newValueBool) {
							entry.TriggerSucccessConcurrents = false;
							entry.TriggerFailureConcurrents = false;
						}
						EditorUtility.SetDirty(entry);
					}

				} else if (node.TheBaseNode is SuccessEnd) {
					SuccessEnd entry = node.TheBaseNode as SuccessEnd;

					EditorGUI.DrawRect(new Rect(6, 6, node.NodeRect.width - 12, node.NodeRect.height - 12), SuccBg);

					GUI.color = TC;
					GUI.Label(new Rect(4, 4, cornX - 8, 32), node.TheBaseNode.name);

					if (entry.IsActivated) {
						GUI.color = WinA;
						GUI.DrawTexture(new Rect(20, 30, lampEndBoxWidth, lampEndBoxHeight), BoxSuccess);
					} else {
						GUI.color = WinN;
						GUI.DrawTexture(new Rect(20, 30, lampEndBoxWidth, lampEndBoxHeight), Box);
					}

					// Stop Task System
					GUI.color = RA;
					bool oldValueBool = entry.StopTaskSystem;
					bool newValueBool = EditorGUI.Toggle(new Rect(67, -3, 16, 16), new GUIContent("", "Stop Task System"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Stop Task System");
						entry.StopTaskSystem = newValueBool;
						EditorUtility.SetDirty(entry);
					}

				} else if (node.TheBaseNode is FailureEnd) {
					FailureEnd entry = node.TheBaseNode as FailureEnd;

					EditorGUI.DrawRect(new Rect(6, 6, node.NodeRect.width - 12, node.NodeRect.height - 12), FailBg);

					GUI.color = TC;
					GUI.Label(new Rect(4, 4, cornX - 8, 32), node.TheBaseNode.name);

					if (entry.IsActivated) {
						GUI.color = FailA;
						GUI.DrawTexture(new Rect(20, 30, lampEndBoxWidth, lampEndBoxHeight), BoxSuccess);
					} else {
						GUI.color = FailN;
						GUI.DrawTexture(new Rect(20, 30, lampEndBoxWidth, lampEndBoxHeight), Box);
					}

					// Stop Task System
					GUI.color = RA;
					bool oldValueBool = entry.StopTaskSystem;
					bool newValueBool = EditorGUI.Toggle(new Rect(67, -3, 16, 16), new GUIContent("", "Stop Task System"), oldValueBool);
					if (newValueBool != oldValueBool) {
						Undo.RecordObject(entry, "Stop Task System");
						entry.StopTaskSystem = newValueBool;
						EditorUtility.SetDirty(entry);
					}

				}

			} else if (node.TheBaseNode is ConditionTimer) {
				ConditionTimer entry = node.TheBaseNode as ConditionTimer;

				EditorGUI.DrawRect(new Rect(6, 6, node.NodeRect.width - 12, node.NodeRect.height - 12), CondBg);

				GUI.color = TC;
				GUI.Label(new Rect(6, 4, cornX - 12, 34), node.TheBaseNode.name);

				if (entry.Result != ConditionBase.ConditionResult.InactiveType)
					GUI.color = CondA;
				else
					GUI.color = CondN;
				GUIStyle style = new GUIStyle(GUI.skin.label);
				style.fontSize = 26;
				style.fontStyle = FontStyle.Bold;
				GUI.Label(new Rect(0, 28, 44, 26), "T", style);

				if (entry.Result == ConditionBase.ConditionResult.RunningType) {
					GUI.color = YA;
					GUI.DrawTexture(new Rect(45, 31, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = YN;
					GUI.DrawTexture(new Rect(45, 31, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

				if (entry.Result == ConditionBase.ConditionResult.SuccessType) {
					GUI.color = GA;
					GUI.DrawTexture(new Rect(35, 42, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = GN;
					GUI.DrawTexture(new Rect(35, 42, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

				if (entry.Result == ConditionBase.ConditionResult.FailureType) {
					GUI.color = RA;
					GUI.DrawTexture(new Rect(55, 42, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = RN;
					GUI.DrawTexture(new Rect(55, 42, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

				// Trigger failure
				GUI.color = RA;
				bool oldValueBool = entry.TriggerFailure;
				bool newValueBool = EditorGUI.Toggle(new Rect(67, -3, 16, 16), new GUIContent("", "Toggle trigger failure"), oldValueBool);
				if (newValueBool != oldValueBool) {
					Undo.RecordObject(entry, "Toggle trigger failure");
					entry.TriggerFailure = newValueBool;
					EditorUtility.SetDirty(entry);
				}

				// Time value field
				if (!EditorApplication.isPlaying) {

					if (entry.IsRandom) {

						GUI.color = ANDC;
						float oldValue = entry.MinTime;
						float newValue = EditorGUI.FloatField(new Rect(5, 54, 34, 18), new GUIContent("", "Timer value min"), oldValue);
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Timer value max changed");
							entry.MinTime = newValue;
							EditorUtility.SetDirty(entry);
						}

						oldValue = entry.MaxTime;
						newValue = EditorGUI.FloatField(new Rect(41f, 54, 34, 18), new GUIContent("", "Timer value max"), oldValue);
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Timer value max changed");
							entry.MaxTime = newValue;
							EditorUtility.SetDirty(entry);
						}

					} else {
						GUI.color = TIMEC;
						float oldValue = entry.TimerValue;
						float newValue = EditorGUI.FloatField(new Rect(10, 54, cornX - 20, 18), new GUIContent("", "Timer value"), oldValue);
						if (newValue != oldValue) {
							Undo.RecordObject(entry, "Timer value changed");
							entry.TimerValue = newValue;
							EditorUtility.SetDirty(entry);
						}
					}
				} else {
					GUI.color = RA2;
					// Just read only.
					EditorGUI.FloatField(new Rect(10, 54, cornX - 20, 18), new GUIContent("", "Timer value"), entry.CurrentTimerValue);
				}

			} else if (node.TheBaseNode is ConditionUser) {
				ConditionUser entry = node.TheBaseNode as ConditionUser;

				EditorGUI.DrawRect(new Rect(6, 6, node.NodeRect.width - 12, node.NodeRect.height - 12), CondBg);

				GUI.color = TC;
				GUI.Label(new Rect(6, 4, cornX - 12, 34), node.TheBaseNode.name);

				if (entry.Result != ConditionBase.ConditionResult.InactiveType)
					GUI.color = CondA;
				else
					GUI.color = CondN;
				GUIStyle style = new GUIStyle(GUI.skin.label);
				style.fontSize = 26;
				style.fontStyle = FontStyle.Bold;
				GUI.Label(new Rect(0, 28, 44, 26), "U", style);

				if (entry.Result == ConditionBase.ConditionResult.RunningType) {
					GUI.color = YA;
					GUI.DrawTexture(new Rect(45, 31, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = YN;
					GUI.DrawTexture(new Rect(45, 31, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

				if (entry.Result == ConditionBase.ConditionResult.SuccessType) {
					GUI.color = GA;
					GUI.DrawTexture(new Rect(35, 42, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = GN;
					GUI.DrawTexture(new Rect(35, 42, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

				if (entry.Result == ConditionBase.ConditionResult.FailureType) {
					GUI.color = RA;
					GUI.DrawTexture(new Rect(55, 42, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = RN;
					GUI.DrawTexture(new Rect(55, 42, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

			} else if (node.TheBaseNode is ConditionArrival) {
				ConditionArrival entry = node.TheBaseNode as ConditionArrival;

				EditorGUI.DrawRect(new Rect(6, 6, node.NodeRect.width - 12, node.NodeRect.height - 12), CondBg);

				GUI.color = TC;
				GUI.Label(new Rect(6, 4, cornX - 12, 34), node.TheBaseNode.name);

				if (entry.Result != ConditionBase.ConditionResult.InactiveType)
					GUI.color = CondA;
				else
					GUI.color = CondN;
				GUIStyle style = new GUIStyle(GUI.skin.label);
				style.fontSize = 26;
				style.fontStyle = FontStyle.Bold;
				GUI.Label(new Rect(0, 28, 44, 26), "A", style);

				if (entry.Result == ConditionBase.ConditionResult.RunningType) {
					GUI.color = YA;
					GUI.DrawTexture(new Rect(45, 31, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = YN;
					GUI.DrawTexture(new Rect(45, 31, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

				if (entry.Result == ConditionBase.ConditionResult.SuccessType) {
					GUI.color = GA;
					GUI.DrawTexture(new Rect(35, 42, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = GN;
					GUI.DrawTexture(new Rect(35, 42, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

				if (entry.Result == ConditionBase.ConditionResult.FailureType) {
					GUI.color = RA;
					GUI.DrawTexture(new Rect(55, 42, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = RN;
					GUI.DrawTexture(new Rect(55, 42, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

			} else if (node.TheBaseNode is ConditionDefeat) {
				ConditionDefeat entry = node.TheBaseNode as ConditionDefeat;

				EditorGUI.DrawRect(new Rect(6, 6, node.NodeRect.width - 12, node.NodeRect.height - 12), CondBg);

				GUI.color = TC;
				GUI.Label(new Rect(6, 4, cornX - 12, 34), node.TheBaseNode.name);

				if (entry.Result != ConditionBase.ConditionResult.InactiveType)
					GUI.color = CondA;
				else
					GUI.color = CondN;
				GUIStyle style = new GUIStyle(GUI.skin.label);
				style.fontSize = 26;
				style.fontStyle = FontStyle.Bold;
				GUI.Label(new Rect(0, 28, 44, 26), "D", style);

				if (entry.Result == ConditionBase.ConditionResult.RunningType) {
					GUI.color = YA;
					GUI.DrawTexture(new Rect(45, 31, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = YN;
					GUI.DrawTexture(new Rect(45, 31, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

				if (entry.Result == ConditionBase.ConditionResult.SuccessType) {
					GUI.color = GA;
					GUI.DrawTexture(new Rect(35, 42, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = GN;
					GUI.DrawTexture(new Rect(35, 42, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

				if (entry.Result == ConditionBase.ConditionResult.FailureType) {
					GUI.color = RA;
					GUI.DrawTexture(new Rect(55, 42, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = RN;
					GUI.DrawTexture(new Rect(55, 42, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

			} else if (node.TheBaseNode is ConditionSurvive) {
				ConditionSurvive entry = node.TheBaseNode as ConditionSurvive;

				EditorGUI.DrawRect(new Rect(6, 6, node.NodeRect.width - 12, node.NodeRect.height - 12), CondBg);

				GUI.color = TC;
				GUI.Label(new Rect(6, 4, cornX - 12, 34), node.TheBaseNode.name);

				if (entry.Result != ConditionBase.ConditionResult.InactiveType)
					GUI.color = CondA;
				else
					GUI.color = CondN;
				GUIStyle style = new GUIStyle(GUI.skin.label);
				style.fontSize = 26;
				style.fontStyle = FontStyle.Bold;
				GUI.Label(new Rect(0, 28, 44, 26), "S", style);

				if (entry.Result == ConditionBase.ConditionResult.RunningType) {
					GUI.color = YA;
					GUI.DrawTexture(new Rect(45, 31, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = YN;
					GUI.DrawTexture(new Rect(45, 31, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

				if (entry.Result == ConditionBase.ConditionResult.SuccessType) {
					GUI.color = GA;
					GUI.DrawTexture(new Rect(35, 42, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = GN;
					GUI.DrawTexture(new Rect(35, 42, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

				if (entry.Result == ConditionBase.ConditionResult.FailureType) {
					GUI.color = RA;
					GUI.DrawTexture(new Rect(55, 42, lampCondBoxWidth, lampCondBoxHeight), BoxSuccess);
				} else {
					GUI.color = RN;
					GUI.DrawTexture(new Rect(55, 42, lampCondBoxWidth, lampCondBoxHeight), Box);
				}

			} else if (node.TheBaseNode is Container) {
				EditorGUI.DrawRect(new Rect(6, 6, node.NodeRect.width - 12, node.NodeRect.height - 12), ContBg);

				GUI.color = TC;
				GUI.Label(new Rect(6, 4, cornX - 12, 34), node.TheBaseNode.name);

				GUI.color = ContTextColor;
				GUIStyle style = new GUIStyle(GUI.skin.label);
				style.fontSize = 26;
				style.fontStyle = FontStyle.Bold;
				GUI.Label(new Rect(8, 28, cornX - 16, 26), "C", style);

			} else if (node.TheBaseNode is ContainerManager) {
				EditorGUI.DrawRect(new Rect(6, 6, node.NodeRect.width - 12, node.NodeRect.height - 12), CMBg);

				GUI.color = TC;
				GUI.Label(new Rect(6, 4, cornX - 12, 34), node.TheBaseNode.name);

				GUI.color = ContManTextColor;
				GUIStyle style = new GUIStyle(GUI.skin.label);
				style.fontSize = 26;
				style.fontStyle = FontStyle.Bold;
				GUI.Label(new Rect(8, 28, cornX - 16, 26), "CM", style);

			}
			GUI.DragWindow();
		}

		/// <summary>
		/// Drawing the connections.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="endPos"></param>
		/// <param name="color"></param>
		private void DrawNodeCurve(Rect start, Vector3 endPos, Color color) {
			Vector3 startPos = new Vector3(start.x + start.width, start.y + start.height / 2, 0);
			DrawNodeCurve(startPos, endPos, color);
		}

		private void DrawNodeCurve(Vector3 startPos, Vector3 endPos, Color color) {
			Vector3 startTan = startPos;
			Vector3 endTan = endPos;

			Color shadowCol = new Color(color.r, color.g, color.b, 0.1f);
			for (int i = 0; i < 3; i++) {
				Handles.DrawBezier(startPos, endPos, startTan, endTan, shadowCol, null, (i + 1) * 5);
			}

			Handles.DrawBezier(startPos, endPos, startTan, endTan, color, LineTexture, 1.5f);
		}

		private void DrawNodeCurve(NodeBase node1, NodeBase node2, Color color) {

			Rect start = node1.NodeRect;
			Rect end = node2.NodeRect;
			Vector2 startPos = new Vector2(start.x + start.width, start.y + start.height / 2);
			Vector2 endPos = new Vector2(end.x, end.y + end.height / 2);

			Vector2 startTan = startPos + Vector2.right * start.width / 2;
			Vector2 endTan = endPos + Vector2.left * end.width / 2;

			// Start
			if (node1.TheBaseNode is ConditionBase || node1.TheBaseNode is FailureEnd) {
				if (node2.NodeRect.center.y > node1.NodeRect.center.y) {
					startPos = new Vector2(start.x + start.width / 2, start.y + start.height);
					endPos = new Vector2(end.x + end.width / 2, end.y);
					startTan = startPos + Vector2.up * start.height / 2;
					endTan = endPos + Vector2.down * end.height / 2;
				} else {
					startPos = new Vector2(start.x + start.width / 2, start.y);
					endPos = new Vector2(end.x + end.width / 2, end.y + end.height);
					startTan = startPos + Vector2.down * start.height / 2;
					endTan = endPos + Vector2.up * end.height / 2;
				}
			}

			// End
			if (node2.TheBaseNode is ConditionBase || node2.TheBaseNode is FailureEnd) {
				if (node1.NodeRect.center.y > node2.NodeRect.center.y) {
					startPos = new Vector2(start.x + start.width / 2, start.y);
					endPos = new Vector2(end.x + end.width / 2, end.y + end.height);
					startTan = startPos + Vector2.down * start.height / 2;
					endTan = endPos + Vector2.up * end.height / 2;
				} else {
					startPos = new Vector2(start.x + start.width / 2, start.y + start.height);
					endPos = new Vector2(end.x + end.width / 2, end.y);
					startTan = startPos + Vector2.up * start.height / 2;
					endTan = endPos + Vector2.down * end.height / 2;
				}
			}

			Color shadowCol = new Color(0, 0, 0, 0.08f);
			if (isPlaying)
				shadowCol = new Color(color.r, color.g, color.b, 0.16f);

			for (int i = 0; i < 3; i++) {
				Handles.DrawBezier(startPos, endPos, startTan, endTan, shadowCol, null, (i + 1) * 5);
			}

			Handles.DrawBezier(startPos, endPos, startTan, endTan, color, LineTexture, 1.5f);
		}

		/// <summary>
		/// Get all components for a game object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		private MonoBehaviour[] GetComps(GameObject target) {
			MonoBehaviour[] mbs = target.GetComponents<MonoBehaviour>();
			return mbs;
		}

		/// <summary>
		/// Get all methods without arguments for the trigger.
		/// With argument maybe in the next release.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		private List<MethodInfo> GetMethods(MonoBehaviour target) {
			List<MethodInfo> methods = new List<MethodInfo>();
			var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
			methods.AddRange(target.GetType().GetMethods(flags));

			List<MethodInfo> filteredMethods = new List<MethodInfo>();
			foreach (MethodInfo mb in methods) {
				//if (mb.GetParameters().Length == 0 || typeof(ActivationGraphSystem.BaseNode).IsAssignableFrom(mb.GetParameters()[0].GetType()) && mb.ReturnType == typeof(void))

				if ((mb.GetParameters().Length == 1 && mb.GetParameters()[0].ParameterType == typeof(ActivationGraphSystem.BaseNode)) && mb.ReturnType == typeof(void)) {
					filteredMethods.Add(mb);
				}
			}

			return filteredMethods;
		}
	}

}
