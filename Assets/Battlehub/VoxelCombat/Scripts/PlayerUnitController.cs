using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public enum PlayerUnitConsoleCmd
    {
        Move,
        Split,
        Grow,
        Diminish,
        Convert,
        Heal
    }

    public interface IPlayerUnitController
    {
        void SubmitConsoleCommand(PlayerUnitConsoleCmd cmd, string[] args, IConsole console);
    }

    public class PlayerUnitController : MonoBehaviour, IPlayerUnitController
    {
        private IMatchEngineCli m_engine;
        private IUnitSelection m_unitSelection;
        private IUnitSelection m_targetSelection;
        private IVoxelInputManager m_inputManager;
        private IVoxelMap m_map;
        private IVoxelGame m_gameState;
        private IPlayerCameraController m_cameraController;
        private Dictionary<int, TaskInfo> m_activeTasks;
        private IBotController m_playersBot;
       

        [SerializeField]
        private GameViewport m_viewport;

        [SerializeField]
        private PlayerCommandsPanel m_commandsPanel;
        
        private int m_localPlayerIndex;
        private int LocalPlayerIndex
        {
            get { return m_localPlayerIndex; }
            set
            {
                if (m_localPlayerIndex != value)
                {
                    m_localPlayerIndex = value;
                    if(m_commandsPanel != null)
                    {
                        m_commandsPanel.LocalPlayerIndex = value;
                    }
                    
                    if (Dependencies.GameView != null)
                    {
                        m_cameraController = Dependencies.GameView.GetCameraController(LocalPlayerIndex);
                    }  
                }
            }
        }


        private void Awake()
        {
            m_engine = Dependencies.MatchEngine;
            m_unitSelection = Dependencies.UnitSelection;
            m_targetSelection = Dependencies.TargetSelection;
            m_inputManager = Dependencies.InputManager;
            m_map = Dependencies.Map;
            m_gameState = Dependencies.GameState;
            

            if (m_commandsPanel != null)
            {
                m_commandsPanel.Move += OnMove;
                m_commandsPanel.Attack += OnAttack;
                m_commandsPanel.Cancel += OnCancel;
                m_commandsPanel.Auto += OnAuto;
                m_commandsPanel.Wall += OnWall;
                m_commandsPanel.Bomb += OnBomb;
                m_commandsPanel.Spawner += OnSpawner;
                m_commandsPanel.Split += OnSplit;
                m_commandsPanel.Split4 += OnSplit4;
                m_commandsPanel.Grow += OnGrow;
                m_commandsPanel.Diminish += OnDiminish;
            }
           // m_commandsPanel.Closed += OnClosed;
        }

        private void OnDestroy()
        {
            if(m_commandsPanel != null)
            {
                m_commandsPanel.Move -= OnMove;
                m_commandsPanel.Attack -= OnAttack;
                m_commandsPanel.Cancel -= OnCancel;
                m_commandsPanel.Auto -= OnAuto;
                m_commandsPanel.Wall -= OnWall;
                m_commandsPanel.Bomb -= OnBomb;
                m_commandsPanel.Spawner -= OnSpawner;
                m_commandsPanel.Split -= OnSplit;
                m_commandsPanel.Split4 -= OnSplit4;
                m_commandsPanel.Grow -= OnGrow;
                m_commandsPanel.Diminish -= OnDiminish;
              //  m_commandsPanel.Closed -= OnClosed;
            }

            if(m_playersBot != null)
            {
                m_playersBot.Reset();
            }
        }

        private void Start()
        {
            LocalPlayerIndex = m_viewport.LocalPlayerIndex;

            int playerIndex = m_gameState.LocalToPlayerIndex(LocalPlayerIndex);

            m_cameraController = Dependencies.GameView.GetCameraController(LocalPlayerIndex);

            m_playersBot = MatchFactoryCli.CreateBotController(m_gameState.GetPlayer(playerIndex), m_engine.GetClientTaskEngine(playerIndex));
            m_playersBot.Init();

            SerializedTask[] taskTemplates = m_gameState.GetTaskTemplates(playerIndex);
            SerializedTaskTemplate[] taskTemplatesData = m_gameState.GetTaskTemplateData(playerIndex);
            for(int i = 0; i < taskTemplates.Length; ++i)
            {
                TaskTemplateType coreTaskType = taskTemplatesData[i].Type;
                m_playersBot.RegisterTask(coreTaskType, taskTemplates[i]);
            }
        }

        private bool m_wasAButtonDown;
        private void Update()
        {
            if (m_gameState.IsContextActionInProgress(LocalPlayerIndex))
            {
                return;
            }

            if(m_gameState.IsMenuOpened(LocalPlayerIndex))
            {
                return;
            }

            if (m_gameState.IsPaused || m_gameState.IsPauseStateChanging)
            {
                return;
            }

            if (m_inputManager.GetButtonDown(InputAction.A, LocalPlayerIndex) || m_inputManager.GetButtonDown(InputAction.RMB, LocalPlayerIndex))
            {
                m_wasAButtonDown = true;
                
                CreateMovementCmd(false, cmd =>
                {
                    if (cmd != null && cmd.Count > 0)
                    {
                        MovementCmd movementCmd = (MovementCmd)cmd[0];

                        if (!m_inputManager.IsKeyboardAndMouse(LocalPlayerIndex))
                        {
                            m_cameraController.SetVirtualMousePosition(movementCmd.Coordinates.Last(), true, true);
                        }
                    }
                });
            }
            else if (m_inputManager.GetButtonUp(InputAction.A, LocalPlayerIndex) || m_inputManager.GetButtonUp(InputAction.RMB, LocalPlayerIndex))
            {
                if(m_wasAButtonDown)
                {
                    CreateMovementCmd(false, cmd =>
                    {
                        if (cmd != null && cmd.Count > 0)
                        {
                            MovementCmd movementCmd = (MovementCmd)cmd[0];
                            if (!m_inputManager.IsKeyboardAndMouse(LocalPlayerIndex))
                            {
                                m_cameraController.SetVirtualMousePosition(movementCmd.Coordinates.Last(), true, true);
                            }
                            SubmitToEngine(m_gameState.LocalToPlayerIndex(LocalPlayerIndex), cmd);
                        }
                    });
                    m_wasAButtonDown = false;
                }
            }
            else if(m_inputManager.GetButtonDown(InputAction.B, LocalPlayerIndex, false, false))
            {
                OnCancel();
            }
            else if (m_inputManager.GetButtonDown(InputAction.Y, LocalPlayerIndex, false, false))
            {
                m_commandsPanel.IsOpen = true;
            } 
        }

        private void OnMove()
        {
            //throw new NotImplementedException();
        }

        private void OnAttack()
        {
           // throw new NotImplementedException();
        }

        private void OnCancel()
        {
            SubmitStdCommand(() => new Cmd(CmdCode.Cancel), (playerIndex, unitId) =>
            {
                return true;
            });
        }

        private void OnAuto(int action)
        {
            int playerIndex = m_gameState.LocalToPlayerIndex(m_localPlayerIndex);
            SerializedTaskTemplate[] templatesInfo = m_gameState.GetTaskTemplateData(playerIndex);
            for(int i = 0; i < templatesInfo.Length; ++i)
            {
                SerializedTaskTemplate templateInfo = templatesInfo[i];
                int index = templateInfo.Index;
                if(index == action)
                {
                    SubmitTaskToClientTaskEngine(templateInfo.Type);
                    break;
                }
            }
        }

        private void OnTaskStateChanged(TaskInfo taskInfo)
        {
            if(taskInfo.State != TaskState.Active)
            {

            }
        }

        private void SubmitTaskToClientTaskEngine(TaskTemplateType templateType)
        {
            m_playersBot.Init();
            int playerIndex = m_gameState.LocalToPlayerIndex(m_localPlayerIndex);
            IMatchPlayerView playerView = m_gameState.GetPlayerView(playerIndex);
            long[] selection = m_unitSelection.GetSelection(playerIndex, playerIndex);
            for (int i = 0; i < selection.Length; ++i)
            {
                m_playersBot.SubmitTask(Time.time, templateType, playerView.GetUnit(selection[i]));
            }
        }

        private void OnDiminish()
        {
            SubmitStdCommand(() => new Cmd(CmdCode.Diminish), (playerIndex, unitId) =>
            {
                IVoxelDataController dataController = m_gameState.GetVoxelDataController(playerIndex, unitId);
                if (dataController.CanDiminishImmediate() != CmdResultCode.Success)
                {
                    Debug.LogWarning("Can't diminish unit " + unitId);
                    return false;
                }
                return true;
            });
        }

        private void OnGrow()
        {
            SubmitStdCommand(() => new Cmd(CmdCode.Grow), (playerIndex, unitId) =>
            {
                IVoxelDataController dataController = m_gameState.GetVoxelDataController(playerIndex, unitId);
                if (dataController.CanGrowImmediate() != CmdResultCode.Success)
                {
                    Debug.LogWarning("Can't grow unit " + unitId);
                    return false;
                }
                return true;
            });
        }

        private void OnSplit4()
        {
            SubmitStdCommand(() => new Cmd(CmdCode.Split4), (playerIndex, unitId) =>
            {
                IVoxelDataController dataController = m_gameState.GetVoxelDataController(playerIndex, unitId);
                if (dataController.CanSplit4Immediate() != CmdResultCode.Success)
                {
                    Debug.LogWarning("Can't split4 unit " + unitId);
                    return false;
                }
                return true;
            });
        }

        private void OnSplit()
        {
            SubmitStdCommand(() => new Cmd(CmdCode.Split), (playerIndex, unitId) =>
            {
                IVoxelDataController dataController = m_gameState.GetVoxelDataController(playerIndex, unitId);
                if (dataController.CanSplitImmediate() != CmdResultCode.Success)
                {
                    Debug.LogWarning("Can't split unit " + unitId);
                    return false;
                }
                return true;
            });
        }

        private void OnSpawner()
        {
            int type = (int)KnownVoxelTypes.Spawner;
            ConvertUnitTo(type);
        }

        private void OnBomb()
        {
            int type = (int)KnownVoxelTypes.Bomb;
            ConvertUnitTo(type);
        }

        private void OnWall()
        {
            int type = (int)KnownVoxelTypes.Ground;
            ConvertUnitTo(type);
        }


        private void ConvertUnitTo(int type)
        {
            SubmitStdCommand(() => new ChangeParamsCmd(CmdCode.Convert)
            {
                IntParams = new int[] { type }
            },
            (playerIndex, unitId) =>
            {
                int controllableUnitsCount = m_gameState.GetStats(playerIndex).ControllableUnitsCount;
                long[] selection = m_unitSelection.GetSelection(playerIndex, playerIndex);
                IVoxelDataController dataController = m_gameState.GetVoxelDataController(playerIndex, unitId);
                if (dataController.CanConvertImmediate(type) != CmdResultCode.Success || selection.Length == controllableUnitsCount) 
                {
                    Debug.LogWarning("Can't convert unit " + unitId + " to " + type);
                    return false;
                }
                return true;
            });
        }

        private void CreateMovementCmd(bool serverSide, Action<List<Cmd>> callback)
        {
            Guid playerId = m_gameState.GetLocalPlayerId(m_localPlayerIndex);
            int playerIndex = m_gameState.GetPlayerIndex(playerId);
            
            long[] selectedUnitIds = m_unitSelection.GetSelection(playerIndex, playerIndex);
            if (selectedUnitIds.Length > 0)
            {
                List<Cmd> commandsToSubmit = new List<Cmd>();

                for (int i = 0; i < selectedUnitIds.Length; ++i)
                {
                    long unitIndex = selectedUnitIds[i];
                    IVoxelDataController dataController = m_gameState.GetVoxelDataController(playerIndex, unitIndex);
                 
                    MapCell cell = m_map.GetCell(m_cameraController.MapCursor, m_cameraController.Weight, null);
                    int deltaWeight = dataController.ControlledData.Weight - m_cameraController.Weight;
                    while (deltaWeight > 0)
                    {
                        cell = cell.Parent;
                        deltaWeight--;
                    }

                    VoxelData selectedTarget = null;
                    //MapCell selectedTargetCell = null;
                    for (int p = 0; p < m_gameState.PlayersCount; ++p)
                    {
                        long[] targetSelection = m_targetSelection.GetSelection(playerIndex, p);
                        if (targetSelection.Length > 0)
                        {
                            MatchAssetCli asset = m_gameState.GetAsset(p, targetSelection[0]);
                            if (asset != null)
                            {
                                selectedTarget = asset.VoxelData;
                                // selectedTargetCell = asset.Cell;
                            }
                            else
                            {
                                IVoxelDataController dc = m_gameState.GetVoxelDataController(p, targetSelection[0]);
                                selectedTarget = dc.ControlledData;
                                // selectedTargetCell = m_map.GetCell(dc.Coordinate.MapPos, dc.Coordinate.Weight, null);
                            }
                        }
                    }

                    int dataType = dataController.ControlledData.Type;
                    int dataWeight = dataController.ControlledData.Weight;


                    VoxelData beneath = null;
                    if (cell != null)
                    {
                        if (selectedTarget == null)
                        {
                            VoxelData defaultTarget;
                            beneath = cell.GetDefaultTargetFor(dataType, dataWeight, playerIndex, false, out defaultTarget);
                        }
                        else
                        {
                            beneath = cell.GetPreviousFor(selectedTarget, dataType, dataWeight, playerIndex);
                        }
                    }
                   
                    VoxelData closestBeneath = beneath;
                    float minDistance = float.MaxValue;

                    if(closestBeneath == null && cell != null)
                    {
                        MapPos pos = cell.GetPosition();
                        for(int r = -1; r <= 1; r++)
                        {
                            for(int c = -1; c <= 1; c++)
                            {
                                MapCell neighbourCell = m_map.GetCell(new MapPos(pos.Row + r, pos.Col + c), dataController.ControlledData.Weight, null);
                                if(neighbourCell != null)
                                {
                                    VoxelData defaultTarget;
                                    VoxelData data = neighbourCell.GetDefaultTargetFor(dataType, dataWeight, playerIndex, false, out defaultTarget);
                                    Vector3 worldPoint = m_map.GetWorldPosition(new MapPos(pos.Row + r, pos.Col + c), dataWeight);
                                    if(data != null)
                                    {
                                        worldPoint.y = (data.Altitude + data.Height) * GameConstants.UnitSize;
                                    }
                                  
                                    Vector2 screenPoint = m_cameraController.WorldToScreenPoint(worldPoint);
                                    if(m_cameraController.InScreenBounds(screenPoint))
                                    {
                                        float distance = (screenPoint - m_cameraController.VirtualMousePosition).magnitude;
                                        if (data != null && distance < minDistance)
                                        {
                                            minDistance = distance;
                                            closestBeneath = data;
                                            cell = neighbourCell;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    beneath = closestBeneath;     

                    if (beneath != null)
                    {
                        int weight = dataController.ControlledData.Weight;
                        // Vector3 hitPoint = beneath.Weight <= weight ? m_map.GetWorldPosition(m_cameraController.MapCursor, m_cameraController.Weight) : m_cameraController.Cursor;
                        //MapPos mapPos = m_map.GetMapPosition(hitPoint, weight);

                        MapPos mapPos = cell.GetPosition();
                        int altitude = beneath.Altitude + beneath.Height;

                        if(serverSide)
                        {
                            MovementCmd movementCmd = new MovementCmd();

                            if (selectedTarget != null)
                            {
                                movementCmd.HasTarget = true;
                                movementCmd.TargetIndex = selectedTarget.UnitOrAssetIndex;
                                movementCmd.TargetPlayerIndex = selectedTarget.Owner;
                            }


                            Coordinate targetCoordinate = new Coordinate(mapPos, weight, altitude);
                            movementCmd.Code = CmdCode.Move;
                            movementCmd.Coordinates = new[] { targetCoordinate };
                            movementCmd.UnitIndex = unitIndex;
                            commandsToSubmit.Add(movementCmd);
                        }
                        else
                        {
                            Coordinate targetCoordinate = new Coordinate(mapPos, weight, altitude);
                            m_engine.GetPathFinder(m_gameState.LocalToPlayerIndex(LocalPlayerIndex)).Find(unitIndex, -1, dataController.Clone(), new[] { dataController.Coordinate, targetCoordinate },
                                 (unitId, path) =>
                                 {
                                     MovementCmd movementCmd = new MovementCmd();
                                     movementCmd.Code = CmdCode.Move;
                                     movementCmd.Coordinates = path;
                                     movementCmd.UnitIndex = unitIndex;
                                     commandsToSubmit.Add(movementCmd);
                                     callback(commandsToSubmit);
                                 },
                                 null);
                        }
                    }
                }

                if(serverSide)
                {
                    callback(commandsToSubmit);
                }
                
            }
            callback(null);
        }


        private void SubmitStdCommand(Func<Cmd> createCmd, Func<int, long, bool> check)
        {
            int playerIndex = m_gameState.LocalToPlayerIndex(m_localPlayerIndex);
            
            long[] selectedUnitIds = m_unitSelection.GetSelection(playerIndex, playerIndex);
                //.OrderBy(unitIndex => m_gameState.GetVoxelDataController(playerIndex, unitIndex).ControlledVoxelData.Altitude).ToArray();

            List<Cmd> commandsToSubmit = new List<Cmd>();
            if (selectedUnitIds.Length > 0)
            {
                for (int i = 0; i < selectedUnitIds.Length; ++i)
                {
                    long unitId = selectedUnitIds[i];

                    if (!check(playerIndex, unitId))
                    {
                        continue;
                    }

                    Cmd command = createCmd();
                    command.UnitIndex = unitId;

                    commandsToSubmit.Add(command);
                }
            }
            SubmitToEngine(playerIndex, commandsToSubmit);
        }

        private void SubmitToEngine(int playerIndex, List<Cmd> commandsToSubmit)
        {
            if (commandsToSubmit.Count == 1)
            {
                ITaskEngine taskEngine = m_engine.GetClientTaskEngine(playerIndex);
                taskEngine.TerminateAll();

                m_engine.Submit(playerIndex, commandsToSubmit[0]);

                //Render hit;
            }
            else if(commandsToSubmit.Count > 1)
            {
                CompositeCmd cmd = new CompositeCmd
                {
                    Commands = commandsToSubmit.ToArray()
                };

                ITaskEngine taskEngine = m_engine.GetClientTaskEngine(playerIndex);
                taskEngine.TerminateAll();

                m_engine.Submit(playerIndex, cmd);
            }
            else
            {
                Debug.LogWarning("No commands to submit");
            }
        }

        public void SubmitConsoleCommand(PlayerUnitConsoleCmd cmd, string[] args, IConsole console)
        {
            int playerIndex = m_gameState.LocalToPlayerIndex(m_localPlayerIndex);

            long[] selectedUnits = m_unitSelection.GetSelection(playerIndex, playerIndex);

            List<Cmd> commandsToSubmit = new List<Cmd>();
            if (cmd == PlayerUnitConsoleCmd.Move)
            {
                int row;
                int col;
                int weight;
                int altitude;
                
                if (args.Length < 4 || !int.TryParse(args[0], out row) || !int.TryParse(args[1], out col) || !int.TryParse(args[2], out weight) || !int.TryParse(args[3], out altitude))
                {
                    console.GetChild(LocalPlayerIndex).Echo("Move <unitId> <row> <col> <weight> <altitude>");
                    return;
                }

                for (int i = 0; i < selectedUnits.Length; ++i)
                {
                    long selectedUnitIndex = selectedUnits[i];

                    CoordinateCmd coordCmd = new CoordinateCmd();
                    coordCmd.Code = CmdCode.Move;
                    coordCmd.Coordinates = new[] { new Coordinate(row, col, weight, altitude) };
                    coordCmd.UnitIndex = selectedUnitIndex;
                    commandsToSubmit.Add(coordCmd);
                }
            }
            else if(cmd == PlayerUnitConsoleCmd.Split)
            {
                CreateStdCommand(selectedUnits, commandsToSubmit, CmdCode.Split);
            }
            else if(cmd == PlayerUnitConsoleCmd.Grow)
            {
                CreateStdCommand(selectedUnits, commandsToSubmit, CmdCode.Grow);
            }
            else if(cmd == PlayerUnitConsoleCmd.Diminish)
            {
                CreateStdCommand(selectedUnits, commandsToSubmit, CmdCode.Diminish);
            }
            else if(cmd == PlayerUnitConsoleCmd.Convert)
            {
                KnownVoxelTypes voxelType;
                if (args.Length < 1 || !(args[0].TryParse(true, out voxelType)))
                {
                    console.GetChild(LocalPlayerIndex).Echo("Convert KnowVoxelType");
                    return;
                }

                CreateStdCommand(selectedUnits, commandsToSubmit, CmdCode.Convert,
                    () => new ChangeParamsCmd() { IntParams = new[] { (int)voxelType } });
                
            }
            else if (cmd == PlayerUnitConsoleCmd.Heal)
            {
                int health = -1;
                if (args.Length < 1 || !int.TryParse(args[0], out health))
                {
                    console.GetChild(LocalPlayerIndex).Echo("SetHealth <health>");
                    return;
                }

                for (int i = 0; i < selectedUnits.Length; ++i)
                {
                    long selectedUnitIndex = selectedUnits[i];

                    ChangeParamsCmd command = new ChangeParamsCmd
                    {
                        Code = CmdCode.SetHealth,
                        UnitIndex = selectedUnitIndex,
                        IntParams = new int[] { health }
                    };

                    commandsToSubmit.Add(command);
                }
            }

            SubmitToEngine(playerIndex, commandsToSubmit);
        }

        private static void CreateStdCommand(long[] selectedUnits, List<Cmd> commandsToSubmit, int cmdCode, Func<Cmd> createCmd = null)
        {
            for (int i = 0; i < selectedUnits.Length; ++i)
            {
                long selectedUnitIndex = selectedUnits[i];

                Cmd command;
                if (createCmd == null)
                {
                    command = new Cmd
                    {
                        Code = cmdCode,
                        UnitIndex = selectedUnitIndex,
                    };
                }
                else
                {
                    command = createCmd();
                    command.Code = cmdCode;
                    command.UnitIndex = selectedUnitIndex;
                }
                 

                commandsToSubmit.Add(command);
            }
        }
    }

    public static class EnumExt
    {
        public static bool TryParse<TEnum>(this string value, bool ignoreCase, out TEnum result)
            where TEnum : struct, IConvertible
        {
            var retValue = value == null ?
                false :
                Enum.IsDefined(typeof(TEnum), value);
            result = retValue ? (TEnum)Enum.Parse(typeof(TEnum), value) : default(TEnum);

            if (!retValue && ignoreCase)
            {
                string[] names = Enum.GetNames(typeof(TEnum));
                for (int i = 0; i < names.Length; ++i)
                {
                    if (string.Compare(names[i], value, true) == 0)
                    {
                        result = (TEnum)Enum.Parse(typeof(TEnum), names[i]);
                        retValue = true;
                        break;
                    }
                }
            }

            return retValue;
        }
    }
}

