using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat.Tests
{
    public class MatchEngineTestsBase
    {
        protected IMatchEngine m_engine;
        protected IReplaySystem m_replay;
        protected MapRoot m_map;
        private float m_prevTickTime;
        private long m_tick;
        private static ProtobufSerializer m_protobufSerializer = new ProtobufSerializer();
        protected const int MAX_TICKS = 1000;
        //4 Players, Depth 6, Flat square, Size 4x4, Cell weight 4 (Map name test_env_0 4 players)
        protected readonly string TestEnv0 = "021ef2f8-789c-44ff-b59b-0f43064c581b.data";

        protected Guid[] m_players;
        protected Dictionary<int, VoxelAbilities[]> m_abilities;

        private VoxelAbilities[] CreateTemporaryAbilies()
        {
            List<VoxelAbilities> abilities = new List<VoxelAbilities>();
            Array voxelTypes = Enum.GetValues(typeof(KnownVoxelTypes));
            for (int typeIndex = 0; typeIndex < voxelTypes.Length; ++typeIndex)
            {
                VoxelAbilities ability = new VoxelAbilities((int)voxelTypes.GetValue(typeIndex));
                abilities.Add(ability);
            }
            return abilities.ToArray();
        }

        protected void BeginTest(string mapName, int playersCount)
        {
            m_abilities = new Dictionary<int, VoxelAbilities[]>();
            m_players = new Guid[playersCount];
            for(int i = 0; i < m_players.Length; ++i)
            {
                m_players[i] = Guid.NewGuid();
                m_abilities.Add(i, CreateTemporaryAbilies());
            }

            string dataPath = Application.streamingAssetsPath + "/Maps/";
            string filePath = dataPath + mapName;

            m_replay = MatchFactory.CreateReplayRecorder();
           
            Dictionary<int, VoxelAbilities>[] allAbilities = new Dictionary<int, VoxelAbilities>[m_players.Length];
            for (int i = 0; i < m_players.Length; ++i)
            {
                allAbilities[i] = m_abilities[i].ToDictionary(a => a.Type);
            }

            MapData mapData = m_protobufSerializer.Deserialize<MapData>(File.ReadAllBytes(filePath));
            m_map = m_protobufSerializer.Deserialize<MapRoot>(mapData.Bytes);
            m_engine = MatchFactory.CreateMatchEngine(m_map, playersCount);
            for (int i = 0; i < m_players.Length; ++i)
            {
                m_engine.RegisterPlayer(m_players[i], i, allAbilities);
            }
            m_engine.CompletePlayerRegistration();
        }

    
        protected void RunEngine(int ticks = MAX_TICKS)
        {
            for (int i = 0; i < ticks; ++i)
            {
                m_engine.Update();
                
                m_replay.Tick(m_engine, m_tick);
                CommandsBundle commands;
                if (m_engine.Tick(m_tick, out commands))
                {
                    commands.Tick = m_tick;
                }

                m_tick++;
                m_prevTickTime += GameConstants.MatchEngineTick;
            }
        }

        protected void EndTest()
        {
            MatchFactory.DestroyMatchEngine(m_engine);
        }

        protected virtual void OnTaskStateChanged(TaskInfo taskInfo)
        {

        }
    }

    public class TaskEngineTests : MatchEngineTestsBase
    {
        private void PrepareTestData1(int playerId, int offsetX, int offsetY, out Cmd cmd)
        {
            Coordinate[] coords = m_map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
            Assert.AreEqual(coords.Length, 2);

            VoxelData unit = m_map.Get(coords[0]);
            Coordinate targetCoordinate = coords[0].Add(offsetX, offsetY);

            cmd = new MovementCmd(CmdCode.Move, unit.UnitOrAssetIndex, 0)
            {
                Coordinates = new[] { targetCoordinate },
            };
        }

        private Cmd PrepareTestData2(int playerId, int cmdCode, int param)
        {
            Cmd cmd;
            Coordinate[] coords = m_map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);

            if(playerId == 1)
            {
                Assert.AreEqual(coords.Length, 2);
            }
            else
            {
                Assert.AreEqual(coords.Length, 1);
            }
    

            VoxelData unit = m_map.Get(coords[0]);

            switch (cmdCode)
            {
                case CmdCode.Convert:
                {
                    cmd = new ChangeParamsCmd(CmdCode.Convert)
                    {
                        UnitIndex = unit.UnitOrAssetIndex,
                        IntParams = new int[] { param }
                    };
                    break;
                }
                case CmdCode.Diminish:
                case CmdCode.Grow:
                case CmdCode.Split:
                case CmdCode.Split4:
                {
                    cmd = new Cmd(cmdCode, unit.UnitOrAssetIndex);
                    break;
                }
                case CmdCode.SetHealth:
                {
                    cmd = new ChangeParamsCmd(CmdCode.SetHealth)
                    {
                        UnitIndex = unit.UnitOrAssetIndex,
                        IntParams = new int[] { param }
                    };

                    break;
                }

                default:
                {
                    cmd = null;
                    break;
                }
            }

            return cmd;
        }

        [Test]
        public void SequentialTaskTest()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 4);
            });

            
            TaskInfo task = new TaskInfo();
            task.TaskType = TaskType.Sequence;
            task.Children = new TaskInfo[]
            {
                new TaskInfo { TaskType = TaskType.TEST_Mock },
                new TaskInfo { TaskType = TaskType.TEST_MockImmediate },
                new TaskInfo { TaskType = TaskType.TEST_MockImmediate },
                new TaskInfo { TaskType = TaskType.TEST_Mock },
                new TaskInfo { TaskType = TaskType.TEST_Mock },
            };
            task.SetParents();

            const int playerId = 1;
            BeginCleanupCheck(playerId);
            FinializeTest(playerId, task, result =>
            {
                Assert.AreEqual(TaskState.Completed, result.State);
                Assert.IsFalse(result.IsFailed);
                CleanupCheck(playerId);
                Assert.Pass();
            });
        }

        [Test]
        public void BranchTaskTest1()
        {
            TaskInfo yes = new TaskInfo { TaskType = TaskType.TEST_Mock };
            TaskInfo no = new TaskInfo { TaskType = TaskType.TEST_Mock };
            BranchTaskTest(true, 
                new[]
                {
                    yes, no
                }, 
                result =>
                {
                    yes = TaskInfo.FindById(yes.TaskId, result);
                    no = TaskInfo.FindById(no.TaskId, result);

                    Assert.AreEqual(TaskState.Completed, yes.State);
                    Assert.IsFalse(yes.IsFailed);
                    Assert.AreEqual(TaskState.Idle, no.State);
                    Assert.IsFalse(no.IsFailed);
                });
        }

        [Test]
        public void BranchTaskTest2()
        {
            TaskInfo yes = new TaskInfo { TaskType = TaskType.TEST_Mock };
            TaskInfo no = new TaskInfo { TaskType = TaskType.TEST_Mock };
            BranchTaskTest(false,
                new[]
                {
                    yes, no
                },
                result =>
                {
                    yes = TaskInfo.FindById(yes.TaskId, result);
                    no = TaskInfo.FindById(no.TaskId, result);

                    Assert.AreEqual(TaskState.Completed, no.State);
                    Assert.IsFalse(no.IsFailed);
                    Assert.AreEqual(TaskState.Idle, yes.State);
                    Assert.IsFalse(yes.IsFailed);
                });
        }

        [Test]
        public void BranchTaskTest3()
        {
            TaskInfo yes = new TaskInfo { TaskType = TaskType.TEST_MockImmediate };
            TaskInfo no = new TaskInfo { TaskType = TaskType.TEST_MockImmediate };
            BranchTaskTest(false,
                new[]
                {
                    yes, no
                },
                result =>
                {
                    yes = TaskInfo.FindById(yes.TaskId, result);
                    no = TaskInfo.FindById(no.TaskId, result);

                    Assert.AreEqual(TaskState.Completed, no.State);
                    Assert.IsFalse(no.IsFailed);
                    Assert.AreEqual(TaskState.Idle, yes.State);
                    Assert.IsFalse(yes.IsFailed);
                }, 0);
        }

        [Test]
        public void BranchTaskTest4()
        {
            TaskInfo yes = new TaskInfo { TaskType = TaskType.TEST_Mock };
            BranchTaskTest(true,
                new[]
                {
                    yes
                },
                result =>
                {
                    yes = TaskInfo.FindById(yes.TaskId, result);

                    Assert.AreEqual(TaskState.Completed, yes.State);
                    Assert.IsFalse(yes.IsFailed);
                });
        }


        [Test]
        public void BranchTaskTest5()
        {
            TaskInfo yes = new TaskInfo { TaskType = TaskType.TEST_Mock };
            BranchTaskTest(false,
                new[]
                {
                    yes
                },
                result =>
                {
                    yes = TaskInfo.FindById(yes.TaskId, result);
                    Assert.AreEqual(TaskState.Idle, yes.State);
                    Assert.IsFalse(yes.IsFailed);
                },
            0);
        }


        public void BranchTaskTest(bool value, TaskInfo[] children, Action<TaskInfo> done, int correction = 1)
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 4);
            });

            ExpressionInfo expression = new ExpressionInfo
            {
                Code = ExpressionCode.Value,
                Value = value,
            };

            TaskInfo task = new TaskInfo();
            task.TaskType = TaskType.Branch;
            task.Expression = expression;
            task.Children = children;
          
            const int playerId = 1;
            BeginCleanupCheck(playerId);
            FinializeTest(playerId, task, result =>
            {
                Assert.AreEqual(TaskState.Completed, result.State);
                Assert.IsFalse(result.IsFailed);
                CleanupCheck(playerId);
                done(result);
                Assert.Pass();
            });

        }

        [Test]
        public void IncrementTaskTest()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 4);
            });

            TaskInputInfo input = new TaskInputInfo
            {
                OutputIndex = 0
            };

            ExpressionInfo setToZero = ExpressionInfo.Val(PrimitiveContract.Create(0));
            ExpressionInfo add = ExpressionInfo.Add(
                ExpressionInfo.Val(input),
                ExpressionInfo.Val(PrimitiveContract.Create(1)));
         
            TaskInfo setToZeroTask = new TaskInfo
            {
                TaskType = TaskType.EvalExpression,
                OutputsCount = 1,
                Expression = setToZero,
            };

            ExpressionInfo increment = ExpressionInfo.Assign(setToZeroTask, add);
            TaskInfo incrementTask = new TaskInfo
            {
                TaskType = TaskType.EvalExpression,
                Expression = increment,
                OutputsCount = 1,
                Inputs = new[] { input },
            };

            TaskInfo task = new TaskInfo
            {
                TaskType = TaskType.Sequence,
                Children = new[]
                {
                    setToZeroTask,
                    incrementTask
                }
            };
            task.SetParents();

            input.OutputTask = setToZeroTask;
            input.SetScope();

            bool isIncremented = false;
            const int playerId = 1;
            BeginCleanupCheck(playerId);
            FinializeTest(playerId, task, result =>
            {
                Assert.IsTrue(isIncremented);
                Assert.AreEqual(TaskState.Completed, result.State);
                Assert.IsFalse(result.IsFailed);
                CleanupCheck(playerId);
                Assert.Pass();
            }, 
            childTask =>
            {
                if(childTask.TaskId == incrementTask.TaskId && childTask.State == TaskState.Completed)
                {
                    ITaskMemory memory = m_engine.GetTaskEngine(playerId).Memory;
                    Assert.AreEqual(1, memory.ReadOutput(setToZeroTask.Parent.TaskId, setToZeroTask.TaskId, 0));
                    isIncremented = true;
                }
            });
        }

        public void RepeatTaskTest(TaskInfo repeatChildTask, TaskInputInfo input, int iterations, int expectedIterations, TaskEngineEvent<TaskInfo> childTaskCallback = null, Action pass = null)
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 4);
            });

            ExpressionInfo setToZero = ExpressionInfo.Val(PrimitiveContract.Create(0));
            ExpressionInfo add = ExpressionInfo.Add(
                ExpressionInfo.Val(input),
                ExpressionInfo.Val(PrimitiveContract.Create(1)));
            ExpressionInfo lessThanValue = ExpressionInfo.Lt(
                ExpressionInfo.Val(input),
                ExpressionInfo.Val(PrimitiveContract.Create(iterations)));

            TaskInfo setToZeroTask = new TaskInfo
            {
                TaskType = TaskType.EvalExpression,
                OutputsCount = 1,
                Expression = setToZero,
            };
            input.OutputTask = setToZeroTask;

            ExpressionInfo increment = ExpressionInfo.Assign(setToZeroTask, add);
            TaskInfo incrementTask = new TaskInfo
            {
                TaskType = TaskType.EvalExpression,
                Expression = increment,
                OutputsCount = 1,
                Inputs = new[] { input },
            };

            TaskInfo repeatTask = new TaskInfo();
            repeatTask.TaskType = TaskType.Repeat;
            repeatTask.Expression = lessThanValue;

            repeatTask.Children = new[]
            {
                 repeatChildTask,
                 incrementTask
            };

            TaskInfo task = new TaskInfo
            {
                TaskType = TaskType.Sequence,
                Children = new[]
                {
                    setToZeroTask,
                    repeatTask
                }
            };
            task.SetParents();
            const int playerId = 1;
            task.Initialize(playerId);

            //
            //input.SetScope();

            bool isIncremented = false;
            
            BeginCleanupCheck(playerId);
            FinializeTest(playerId, task, result =>
            {
                Assert.IsTrue(isIncremented);
                Assert.AreEqual(TaskState.Completed, result.State);
                Assert.IsFalse(result.IsFailed);
                CleanupCheck(playerId);
                if(pass != null)
                {
                    pass();
                }
                Assert.Pass();
            },
            childTask =>
            {
                if (childTask.TaskId == repeatTask.TaskId && childTask.State == TaskState.Completed)
                {
                    ITaskMemory memory = m_engine.GetTaskEngine(playerId).Memory;
                    Assert.AreEqual(expectedIterations, memory.ReadOutput(setToZeroTask.Parent.TaskId, setToZeroTask.TaskId, 0));
                    isIncremented = true;
                }
                else
                {
                    if(childTaskCallback != null)
                    {
                        childTaskCallback(childTask);
                    }
                }
            });
        }

        [Test]
        public void RepeatTaskTest()
        {
            TaskInputInfo input = new TaskInputInfo { OutputIndex = 0 };
            TaskInfo repeatTask = new TaskInfo { TaskType = TaskType.TEST_Mock };
            RepeatTaskTest(repeatTask, input, 10, 10);
        }

        [Test]
        public void RepeatTaskTestImmediate()
        {
            TaskInputInfo input = new TaskInputInfo { OutputIndex = 0 };
            TaskInfo repeatTask = new TaskInfo { TaskType = TaskType.TEST_MockImmediate };
            RepeatTaskTest(repeatTask, input, 10, 10);
        }

        [Test]
        public void RepeatBreakTaskTest()
        {
            TaskInputInfo input = new TaskInputInfo { OutputIndex = 0 };
            ExpressionInfo eqTo5 = ExpressionInfo.Eq(
                ExpressionInfo.Val(input),
                ExpressionInfo.Val(PrimitiveContract.Create(5)));
            TaskInfo branchTask = new TaskInfo
            {
                TaskType = TaskType.Branch,
                Expression = eqTo5,
                Children = new[]
                {
                    new TaskInfo
                    {
                       TaskType = TaskType.Break,
                    },
                    null
                }
            };
            RepeatTaskTest(branchTask, input, 10, 5);
        }

        [Test]
        public void ContinueInSequenceTest()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 4);
            });

            const int playerId = 2;

            ExpressionInfo expr = ExpressionInfo.PrimitiveVal(true);
            TaskInfo task = TaskInfo.Repeat(expr);
            task.Children = new[]
            {
                new TaskInfo(TaskType.TEST_Mock),
                new TaskInfo(TaskType.TEST_Mock),
                TaskInfo.Sequence
                (
                    new TaskInfo(TaskType.TEST_MockImmediate),
                    TaskInfo.Assert((tb, ti) =>
                    {
                        expr = TaskInfo.FindById(task.TaskId, ti.Root).Expression;
                        expr.Value = PrimitiveContract.Create(false);
                        return TaskState.Completed;
                    }),
                    TaskInfo.Continue(),
                    new TaskInfo(TaskType.TEST_Fail)
                ),
                new TaskInfo(TaskType.TEST_MockImmediate),
                new TaskInfo(TaskType.TEST_MockImmediate)
            };


            BeginCleanupCheck(playerId);
            FinializeTest(playerId, task, result =>
            {
                Assert.AreEqual(TaskState.Completed, result.State);
                Assert.IsFalse(result.IsFailed);
                CleanupCheck(playerId);
                Assert.Pass();
            });
        }


        [Test]
        public void BreakInSequenceTest()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 4);
            });

            const int playerId = 2;

            ExpressionInfo expr = ExpressionInfo.PrimitiveVal(true);
            TaskInfo task = TaskInfo.Repeat
                (
                    expr,
                    new TaskInfo(TaskType.TEST_Mock),
                    new TaskInfo(TaskType.TEST_Mock),
                    TaskInfo.Sequence
                    (
                        new TaskInfo(TaskType.TEST_MockImmediate),
                        TaskInfo.Assert((tb, ti) =>
                        {
                            expr.Value = PrimitiveContract.Create(false);
                            return TaskState.Completed;
                        }),
                        TaskInfo.Break(),
                        new TaskInfo(TaskType.TEST_Fail)
                    ),
                    new TaskInfo(TaskType.TEST_MockImmediate),
                    new TaskInfo(TaskType.TEST_MockImmediate)
                );


            BeginCleanupCheck(playerId);
            FinializeTest(playerId, task, result =>
            {
                Assert.AreEqual(TaskState.Completed, result.State);
                Assert.IsFalse(result.IsFailed);
                CleanupCheck(playerId);
                Assert.Pass();
            });
        }

        [Test]
        public void RepeatContinueTaskTest()
        {
            TaskInputInfo input = new TaskInputInfo { OutputIndex = 0 };
            ExpressionInfo isTrue = ExpressionInfo.Val(PrimitiveContract.Create(true));
            TaskInfo continueTask = new TaskInfo
            {
                TaskType = TaskType.Continue,
            };
            TaskInfo branchTask = new TaskInfo
            {
                TaskType = TaskType.Branch,
                Expression = isTrue,
                Children = new[]
                {
                    continueTask,
                    null
                }
            };

            int continueCounter = 0;
            RepeatTaskTest(branchTask, input, 2, 2, childTask =>
            {
                if(continueTask.TaskId == childTask.TaskId && childTask.State == TaskState.Active)
                {
                    continueCounter++;
                    if(continueCounter == 1)
                    {
                        isTrue = TaskInfo.FindById(branchTask.TaskId, childTask.Root).Expression;
                        isTrue.Value = false;
                    }
                }
            },
            () =>
            {
                Assert.AreEqual(1, continueCounter);
            });

        }

        [Test]
        public void IterateTest()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 4);
            });

            List<int> integers = new List<int>()
            {
                1, 2, 3, 4, 5
            };
            TaskInfo iterateTask = new TaskInfo
            {
                TaskType = TaskType.EvalExpression,
                Expression = ExpressionInfo.Iterate(integers),
                OutputsCount = 1,
            };

            TaskInfo sequence = new TaskInfo
            {
                TaskType = TaskType.Sequence,
                Children = new[]
                {
                    iterateTask,
                }
            };

            sequence.SetParents();

            const int playerId = 1;
            bool isIterated = false;
            BeginCleanupCheck(playerId);
            FinializeTest(playerId, sequence, result =>
            {
                Assert.IsTrue(isIterated);

                Assert.AreEqual(TaskState.Completed, result.State);
                Assert.IsFalse(result.IsFailed);
                CleanupCheck(playerId);
      
                Assert.Pass();
            },
            childTask =>
            {
                if(childTask.State == TaskState.Completed)
                {
                    ITaskMemory memory = m_engine.GetTaskEngine(playerId).Memory;
                    IterationResult iterResult = (IterationResult)memory.ReadOutput(iterateTask.Parent.TaskId, iterateTask.TaskId, 0);
                    Assert.IsFalse(iterResult.IsLast);
                    Assert.AreEqual(1, (int)iterResult.Object);
                    isIterated = true;
                } 
            });
        }

        [Test]
        public void IterateRepeatTest()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 4);
            });

            List<int> integers = new List<int>()
            {
                1, 2, 3, 4, 5
            };
            TaskInfo iterateTask = new TaskInfo
            {
                TaskType = TaskType.EvalExpression,
                Expression = ExpressionInfo.Iterate(integers),
                OutputsCount = 1,
            };

            TaskInputInfo input = new TaskInputInfo
            {
                OutputIndex = 0,
                OutputTask = iterateTask,
            };

            ExpressionInfo isTrue = ExpressionInfo.Eq(
                ExpressionInfo.Val(PrimitiveContract.Create(true)),
                ExpressionInfo.Get(
                    ExpressionInfo.Val(input),
                    ExpressionInfo.PrimitiveVal("IsLast")));

            TaskInfo breakIfCompleted = new TaskInfo
            {
                TaskType = TaskType.Branch,
                Expression = isTrue,
                Children = new[]
                {
                    new TaskInfo
                    {
                        TaskType = TaskType.Break,
                    },
                    null
                }
            };

            TaskInfo repeat = new TaskInfo
            {
                TaskType = TaskType.Repeat,
                Expression = ExpressionInfo.PrimitiveVal(true),
                Children = new[]
                {
                    iterateTask,
                    breakIfCompleted,
                }
            };

            repeat.SetParents();
            input.SetScope();

            const int playerId = 1;
            bool isIterated = false;
            int counter = 0;
            BeginCleanupCheck(playerId);
            
            FinializeTest(playerId, repeat, result =>
            {
                Assert.IsTrue(isIterated);

                Assert.AreEqual(TaskState.Completed, result.State);
                Assert.IsFalse(result.IsFailed);
                CleanupCheck(playerId);

                Assert.Pass();
            },
            childTask =>
            {
                if(childTask.TaskId == iterateTask.TaskId && childTask.State == TaskState.Completed)
                {
                    counter++;
                    ITaskMemory memory = m_engine.GetTaskEngine(playerId).Memory;
                    IterationResult iterResult = (IterationResult)memory.ReadOutput(iterateTask.Parent.TaskId, iterateTask.TaskId, 0);
                    if (iterResult.IsLast)
                    {
                        isIterated = true;
                        Assert.IsNull(iterResult.Object);
                    }
                    else
                    {
                        Assert.AreEqual(counter, (int)iterResult.Object);
                    }
                }
            });
        }

        [Test]
        public void ProcedureReturnTest()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 4);
            });


            TaskInfo procedure = TaskInfo.Procedure(
                new TaskInfo(TaskType.TEST_Mock),
                new TaskInfo(TaskType.TEST_MockImmediate),
                new TaskInfo(TaskType.TEST_MockImmediate),
                TaskInfo.Return(ExpressionInfo.PrimitiveVal(TaskInfo.TaskSucceded)),
                new TaskInfo(TaskType.TEST_Fail)
            );

            TaskInfo root = TaskInfo.Sequence
                (
                    procedure,
                    TaskInfo.Branch(
                        ExpressionInfo.TaskSucceded(procedure),
                        new TaskInfo(TaskType.Nop),
                        new TaskInfo(TaskType.TEST_Fail)
                    )
                );

            root.SetParents();

            const int playerId = 1;
            BeginCleanupCheck(playerId);

            FinializeTest(playerId, root, result =>
            {
                Assert.AreEqual(root.TaskId, result.TaskId);
                Assert.AreEqual(TaskState.Completed, result.State);
                Assert.IsFalse(result.IsFailed);
                CleanupCheck(playerId);

                Assert.Pass();
            },
            childTask =>
            {
               
            });
        }

        [Test]
        public void ProcedureReturnFailTest()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 4);
            });


            TaskInfo procedure = TaskInfo.Procedure(
                new TaskInfo(TaskType.TEST_Mock),
                new TaskInfo(TaskType.TEST_MockImmediate),
                new TaskInfo(TaskType.TEST_MockImmediate),
                TaskInfo.Return(ExpressionInfo.PrimitiveVal(TaskInfo.TaskFailed)),
                new TaskInfo(TaskType.TEST_Fail)
            );

            TaskInfo root = TaskInfo.Sequence
                (
                    procedure,
                    TaskInfo.Branch(
                        ExpressionInfo.TaskSucceded(procedure),
                        new TaskInfo(TaskType.TEST_Fail),
                        new TaskInfo(TaskType.Nop)
                    )
                );

            root.SetParents();

            const int playerId = 1;
            BeginCleanupCheck(playerId);

            FinializeTest(playerId, root, result =>
            {
                Assert.AreEqual(root.TaskId, result.TaskId);
                Assert.AreEqual(TaskState.Completed, result.State);
                Assert.IsFalse(result.IsFailed);
                CleanupCheck(playerId);

                Assert.Pass();
            },
            childTask =>
            {

            });
        }

        [Test]
        public void RepeatProcedureReturnTest()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 4);
            });


            TaskInfo procedure = TaskInfo.Procedure(
                TaskInfo.Repeat(
                    ExpressionInfo.PrimitiveVal(true),
                    new TaskInfo(TaskType.TEST_Mock),
                    new TaskInfo(TaskType.TEST_MockImmediate),
                    new TaskInfo(TaskType.TEST_MockImmediate),
                    TaskInfo.Return(ExpressionInfo.PrimitiveVal(TaskInfo.TaskSucceded)),
                    new TaskInfo(TaskType.TEST_Fail)
                )
            );

            TaskInfo root = TaskInfo.Sequence
                (
                    procedure,
                    TaskInfo.Branch(
                        ExpressionInfo.TaskSucceded(procedure),
                        new TaskInfo(TaskType.Nop),
                        new TaskInfo(TaskType.TEST_Fail)
                    )
                );

            root.SetParents();

            const int playerId = 1;
            BeginCleanupCheck(playerId);

            FinializeTest(playerId, root, result =>
            {
                Assert.AreEqual(root.TaskId, result.TaskId);
                Assert.AreEqual(TaskState.Completed, result.State);
                Assert.IsFalse(result.IsFailed);
                CleanupCheck(playerId);

                Assert.Pass();
            },
            childTask =>
            {

            });
        }

        [Test]
        public void SimpleMoveWithoutExpression()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 4);
            });
            const int playerId = 1;
            Cmd cmd;
            PrepareTestData1(playerId, -1, 1,
                out cmd);
            TaskInfo task = new TaskInfo(cmd);
            FinializeTest(playerId, task, TaskMovementCompleted);
        }

        [Test]
        public void SimpleMoveFailWithoutExpression()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 4);
            });
            const int playerId = 1;
            Cmd cmd;
            PrepareTestData1(playerId, 10, 1,
                out cmd);
            TaskInfo task = new TaskInfo(cmd);
            FinializeTest(playerId, task, TaskMovementFailed);
        }

        [Test]
        public void ConvertToBombTaskTest()
        {
            const int playerId = 1;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.Convert, (int)KnownVoxelTypes.Bomb), ConvertTaskCompleted);
        }

        [Test]
        public void ConvertToWallTaskTest()
        {
            const int playerId = 1;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.Convert, (int)KnownVoxelTypes.Ground), ConvertTaskCompleted);
        }

        [Test]
        public void ConvertToSpawnerTaskTest()
        {
            const int playerId = 1;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.Convert, (int)KnownVoxelTypes.Spawner), ConvertTaskCompleted);
        }

        [Test]
        public void GrowTaskTest()
        {
            const int playerId = 1;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.SetHealth, 64), setHealthTaskInfo =>
            {
                Assert.AreEqual(setHealthTaskInfo.State, TaskState.Completed);
                Assert.IsFalse(setHealthTaskInfo.IsFailed);
                ExecuteGenericTaskTest(CmdCode.Grow, GrowTaskCompleted, false);
            });

        }

        [Test]
        public void DiminishTaskTest()
        {
            const int playerId = 1;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.SetHealth, 64), setHealthTaskInfo =>
            {
                Assert.AreEqual(TaskState.Completed, setHealthTaskInfo.State);
                Assert.IsFalse(setHealthTaskInfo.IsFailed);
                ExecuteGenericTaskTest(CmdCode.Grow, growTaskInfo =>
                {
                    Assert.AreEqual(TaskState.Completed, growTaskInfo.State);
                    Assert.IsFalse(growTaskInfo.IsFailed);
                    ExecuteGenericTaskTest(CmdCode.Diminish, DiminishTaskCompleted, false);
                },
                false);
            });

        }

        [Test]
        public void SplitTaskTest()
        {
            const int playerId = 3;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.SetHealth, 64), setHealthTaskInfo =>
            {
                Assert.AreEqual(TaskState.Completed, setHealthTaskInfo.State);
                ExecuteGenericTaskTest(CmdCode.Split, SplitTaskCompleted, false, playerId);
            },
            true, playerId);
        }

        [Test]
        public void Split4TaskTest()
        {
            const int playerId = 3;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.SetHealth, 64), setHealthTaskInfo =>
            {
                Assert.AreEqual(TaskState.Completed, setHealthTaskInfo.State);
                ExecuteGenericTaskTest(CmdCode.Grow, growTaskInfo =>
                {
                    ExecuteGenericTaskTest(CmdCode.Split4, Split4TaskCompleted, false, playerId);
                }, 
                false, playerId);
            }, 
            true, playerId);
        }
        

        private void ExecuteGenericTaskTest(int cmdCode, TaskEngineEvent<TaskInfo> taskStateChangeEventHandler, bool begin = true, int playerId = 1)
        {
            ExecuteTaskTest(() =>
            {
                return PrepareTestData2(playerId, cmdCode, 0);
            }, taskStateChangeEventHandler, begin, playerId);
        }

        private void ExecuteTaskTest(Func<Cmd> runTestCallback,  TaskEngineEvent<TaskInfo> taskStateChangeEventHandler, bool begin = true, int playerId = 1)
        {
            if(begin)
            {
                Assert.DoesNotThrow(() =>
                {
                    BeginTest(TestEnv0, 4);
                });
            }
           
            Cmd cmd = runTestCallback();
            TaskInfo task = new TaskInfo(cmd);
            FinializeTest(playerId, task, taskStateChangeEventHandler);
        }

        protected void TaskMovementCompleted(TaskInfo taskInfo)
        {
            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.AreEqual(TaskState.Completed, taskInfo.State);
            Assert.IsFalse(taskInfo.IsFailed);

            MovementCmd cmd = (MovementCmd)taskInfo.Cmd;
            Coordinate[] coords = m_map.FindDataOfType((int)KnownVoxelTypes.Eater, 1);
            Assert.AreEqual(cmd.Coordinates[0], coords[0]);

            Assert.Pass();
        }

        protected void TaskMovementFailed(TaskInfo taskInfo)
        {
            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.IsTrue(taskInfo.IsFailed);

            MovementCmd cmd = (MovementCmd)taskInfo.Cmd;
            Coordinate[] coords = m_map.FindDataOfType((int)KnownVoxelTypes.Eater, 1);
            Assert.AreNotEqual(cmd.Coordinates[0], coords[0]);

            Assert.Pass();
        }

        protected void ConvertTaskCompleted(TaskInfo taskInfo)
        {
            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.AreEqual(TaskState.Completed, taskInfo.State);
            Assert.IsFalse(taskInfo.IsFailed);

            ChangeParamsCmd cmd = (ChangeParamsCmd)taskInfo.Cmd;
            Coordinate[] coords = m_map.FindDataOfType(cmd.IntParams[0], 1);
            VoxelData data = m_map.Get(coords[0]);
            Assert.IsNotNull(data);
            Assert.AreEqual(data.Type, cmd.IntParams[0]);

            Assert.Pass();
        }

        protected void GrowTaskCompleted(TaskInfo taskInfo)
        {
            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.AreEqual(TaskState.Completed, taskInfo.State);
            Assert.IsFalse(taskInfo.IsFailed);

            IMatchUnitController controller = m_engine.GetUnitController(1, taskInfo.Cmd.UnitIndex); 
            Assert.IsNotNull(controller);
            Assert.AreEqual(controller.Data.Weight, 3);
            
            Assert.Pass();
        }

        protected void DiminishTaskCompleted(TaskInfo taskInfo)
        {
            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.AreEqual(TaskState.Completed, taskInfo.State);
            Assert.IsFalse(taskInfo.IsFailed);

            IMatchUnitController controller = m_engine.GetUnitController(1, taskInfo.Cmd.UnitIndex);
            Assert.IsNotNull(controller);
            Assert.AreEqual(controller.Data.Weight, 2);

            Assert.Pass();
        }

        protected void Split4TaskCompleted(TaskInfo taskInfo)
        {
            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.AreEqual(TaskState.Completed, taskInfo.State);
            Assert.IsFalse(taskInfo.IsFailed);

            const int playerId = 3;
            IMatchUnitController controller = m_engine.GetUnitController(playerId, taskInfo.Cmd.UnitIndex);
            Assert.IsNull(controller);

            Coordinate[] coords = m_map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
            Assert.AreEqual(4, coords.Length);
            for(int i = 0; i < coords.Length; ++i)
            {
                Assert.AreEqual(2, coords[i].Weight);

                VoxelData data = m_map.Get(coords[i]);
                Assert.IsNotNull(data);
                Assert.AreEqual((int)KnownVoxelTypes.Eater, data.Type); 
            }

            Assert.AreEqual(1, coords[0].MapPos.SqDistanceTo(coords[1].MapPos));
            Assert.AreEqual(1, coords[2].MapPos.SqDistanceTo(coords[3].MapPos));
            Assert.AreEqual(2, coords[1].MapPos.SqDistanceTo(coords[2].MapPos));
            Assert.AreEqual(2, coords[0].MapPos.SqDistanceTo(coords[3].MapPos));

            Assert.Pass();
        }

        protected void SplitTaskCompleted(TaskInfo taskInfo)
        {
            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.AreEqual(TaskState.Completed, taskInfo.State);
            Assert.IsFalse(taskInfo.IsFailed);

            const int playerId = 3;
            IMatchUnitController controller = m_engine.GetUnitController(playerId, taskInfo.Cmd.UnitIndex);
            Assert.IsNull(controller);

            Coordinate[] coords = m_map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
            Assert.AreEqual(2, coords.Length);
            for (int i = 0; i < coords.Length; ++i)
            {
                Assert.AreEqual(2, coords[i].Weight);

                VoxelData data = m_map.Get(coords[i]);
                Assert.IsNotNull(data);
                Assert.AreEqual((int)KnownVoxelTypes.Eater, data.Type);
            }

            Assert.AreEqual(1, coords[0].MapPos.SqDistanceTo(coords[1].MapPos));
            Assert.Pass();
        }

        protected void FinializeTest(int playerIndex, TaskInfo task, TaskEngineEvent<TaskInfo> callback, TaskEngineEvent<TaskInfo> childTaskCallback = null)
        {
            long identity = 0;
            TaskEngine.GenerateIdentitifers(task, ref identity);

            TaskEngineEvent<TaskInfo> taskStateChangedEventHandler = null;
            taskStateChangedEventHandler = taskInfo =>
            {
                if (taskInfo.TaskId == task.TaskId)
                {
                    if (taskInfo.State != TaskState.Active)
                    {
                        m_engine.GetTaskEngine(playerIndex).TaskStateChanged -= taskStateChangedEventHandler;
                        callback(taskInfo);
                    }
                }
                else
                {
                    if (childTaskCallback != null)
                    {
                        childTaskCallback(taskInfo);
                    }
                }
            };
            m_engine.GetTaskEngine(playerIndex).TaskStateChanged += taskStateChangedEventHandler;
            m_engine.Submit(playerIndex, new TaskCmd(SerializedTask.FromTaskInfo(task)));

            RunEngine();
            Assert.Fail();
        }

        private int m_pooledObjectsCount;

        private void BeginCleanupCheck(int playerIndex)
        {
            ITaskEngineTestView engine = m_engine.GetTaskEngine(playerIndex) as ITaskEngineTestView;
            m_pooledObjectsCount = engine.PoolObjectsCount;
        }

        private void CleanupCheck(int playerIndex)
        {
            ITaskEngineTestView engine = m_engine.GetTaskEngine(playerIndex) as ITaskEngineTestView;
            Assert.AreEqual(0, engine.ActiveTasks.Count);
            Assert.AreEqual(0, engine.IdToActiveTask.Count);
            Assert.AreEqual(0, engine.PendingRequestsCount);
            Assert.AreEqual(0, engine.TimedoutRequestsCount);
            Assert.AreEqual(0, engine.TaskMemory.Memory.Count);

            //for certain objects objects are released after callback event. Correction needed in this cases
            Assert.AreEqual(m_pooledObjectsCount, engine.PoolObjectsCount);

        }
    }

}
