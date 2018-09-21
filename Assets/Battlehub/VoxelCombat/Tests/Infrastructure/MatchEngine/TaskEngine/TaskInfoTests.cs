using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace Battlehub.VoxelCombat.Tests
{
    public class TaskInfoTests
    {
        private static ProtobufSerializer m_protobufSerializer = new ProtobufSerializer();
        [Test]
        public void CoordinateArrayDeepClone()
        {
            Coordinate[] coord = new[] { new Coordinate(1, 2, 3, 4) };

            coord = m_protobufSerializer.DeepClone(coord);

            Assert.AreEqual(1, coord.Length);
        }
        
        [Test]
        public void ExpressionSerializationDeserialization()
        {
            ExpressionInfo expression = new ExpressionInfo();
            expression.Code = ExpressionCode.And;
            expression.Children = new[]
            {
                new ExpressionInfo
                {
                    Code = ExpressionCode.Eq,
                    Children = new[]
                    {
                        new ExpressionInfo
                        {
                            Code = ExpressionCode.Value,
                            Value = PrimitiveContract.Create(new Coordinate(1, 1, 1, 1))
                        },
                        new ExpressionInfo
                        {
                            Code = ExpressionCode.Value,
                            Value = PrimitiveContract.Create(new Coordinate(1, 1, 1, 1))
                        },
                    }
                },
                new ExpressionInfo
                {
                    Code = ExpressionCode.Value,
                    Value = PrimitiveContract.Create(true),
                }
            };

            ExpressionInfo clone = null;
            Assert.DoesNotThrow(() =>
            {
                TaskInfo task = new TaskInfo { Expression = expression };
                clone = SerializedTask.ToTaskInfo(m_protobufSerializer.DeepClone(SerializedTask.FromTaskInfo(task))).Expression;
            });

            Assert.IsNotNull(clone);
            Assert.AreEqual(expression.Code, clone.Code);
            Assert.IsNotNull(clone.Children);
            Assert.AreNotSame(expression.Children, clone.Children);
            Assert.AreEqual(expression.Children.Length, clone.Children.Length);
            Assert.IsNotNull(clone.Children[0].Children);
            Assert.AreEqual(expression.Children[0].Children.Length, clone.Children[0].Children.Length);
            Assert.AreEqual(expression.Children[0].Children[0].Code, clone.Children[0].Children[0].Code);
            Assert.AreEqual(expression.Children[0].Children[0].Value, clone.Children[0].Children[0].Value);
            Assert.AreEqual(expression.Children[1].Code, clone.Children[1].Code);
            Assert.AreEqual(expression.Children[1].Value, clone.Children[1].Value);
        }

        [Test]
        public void ExpreessionInfoCloneTest2()
        {

            ExpressionInfo canRun = ExpressionInfo.Eq(
                ExpressionInfo.PrimitiveVal(CmdResultCode.Success),
                ExpressionInfo.Val(new TaskInputInfo()));

            Assert.DoesNotThrow(() =>
            {
                TaskInfo task = new TaskInfo { Expression = canRun };
                byte[] b = m_protobufSerializer.Serialize(SerializedTask.FromTaskInfo(task));
                SerializedTask.ToTaskInfo(m_protobufSerializer.Deserialize<SerializedTask>(b));
            });
        }

        [Test]
        public void EatGrowSplitCloneTest()
        {
            TaskInfo task = TaskInfo.EatGrowSplit4(new TaskInputInfo(), new TaskInputInfo());

            Assert.DoesNotThrow(() =>
            {
                byte[] b = m_protobufSerializer.Serialize(SerializedTask.FromTaskInfo(task));
                SerializedTask.ToTaskInfo(m_protobufSerializer.Deserialize<SerializedTask>(b));
            });
        }

        [Test]
        public void SearchMoveOrRandomMoveCloneTest()
        {
            TaskInfo task = TaskInfo.SearchMoveOrRandomMove(TaskType.SearchForFood, new TaskInputInfo());

            Assert.DoesNotThrow(() =>
            {
                byte[] b = m_protobufSerializer.Serialize(SerializedTask.FromTaskInfo(task));
                SerializedTask.ToTaskInfo(m_protobufSerializer.Deserialize<SerializedTask>(b));
            });
        }

        [Test]
        public void SearchForPathCloneTest()
        {
            TaskInfo task = TaskInfo.SearchForPath(TaskType.SearchForFood, TaskInfo.Var(), new TaskInputInfo());

            Assert.DoesNotThrow(() =>
            {
                SerializedTask.ToTaskInfo(m_protobufSerializer.DeepClone(SerializedTask.FromTaskInfo(task)));
            });
        }

 
        [Test]
        public void SearchForPathCloneTest2()
        {
            TaskInfo searchForTask = TaskInfo.SearchFor(TaskType.SearchForFood, new TaskInputInfo());

            ExpressionInfo searchForSucceded = ExpressionInfo.TaskSucceded(searchForTask);

            TaskInputInfo coordinateInput = new TaskInputInfo(searchForTask, 1);
            TaskInfo findPathTask = TaskInfo.FindPath(new TaskInputInfo(), coordinateInput);
            ExpressionInfo findPathSucceded = ExpressionInfo.TaskSucceded(findPathTask);

            TaskInputInfo pathVariableInput = new TaskInputInfo(findPathTask, 0);
            ExpressionInfo assignPathVariable = ExpressionInfo.Assign(
                TaskInfo.Var(),
                ExpressionInfo.Val(pathVariableInput));

            ExpressionInfo whileTrue = ExpressionInfo.PrimitiveVal(true);

            TaskInfo task =
                TaskInfo.Procedure(
                    TaskInfo.Repeat(
                        whileTrue,
                        searchForTask,
                        TaskInfo.Branch(
                            searchForSucceded,
                            TaskInfo.Sequence(
                                findPathTask,
                                TaskInfo.Branch(
                                    findPathSucceded,
                                    TaskInfo.Sequence(
                                        TaskInfo.EvalExpression(assignPathVariable),
                                        TaskInfo.Return()
                                    ),
                                    TaskInfo.Continue()
                                )
                            ),

                      
                            TaskInfo.Return(ExpressionInfo.PrimitiveVal(TaskInfo.TaskFailed))
                        )
                    )
                );

            Assert.DoesNotThrow(() =>
            {
                SerializedTask.ToTaskInfo(m_protobufSerializer.DeepClone(SerializedTask.FromTaskInfo(task)));
            });
        }

        [Test]
        public void AssignPathVariableCloneTest()
        {
            TaskInfo findPathTask = TaskInfo.FindPath(new TaskInputInfo(), new TaskInputInfo());
            TaskInputInfo pathVariableInput =  new TaskInputInfo(findPathTask, 0);
            ExpressionInfo assignPathVariable = ExpressionInfo.Assign(
                TaskInfo.Var(),
                ExpressionInfo.Val(pathVariableInput));
            TaskInfo task = TaskInfo.Sequence(
                findPathTask,
                TaskInfo.EvalExpression(assignPathVariable)
                );

            Assert.DoesNotThrow(() =>
            {
                SerializedTask.ToTaskInfo(m_protobufSerializer.DeepClone(SerializedTask.FromTaskInfo(task)));
            });
        }

        [Test]
        public void ProcedureRepeatBranchSearchCloneTest()
        {
            ExpressionInfo whileTrue = ExpressionInfo.PrimitiveVal(true);
            TaskInfo searchForTask = TaskInfo.SearchFor(TaskType.SearchForFood, new TaskInputInfo());
            TaskInfo task = 
                TaskInfo.Procedure(
                    TaskInfo.Repeat(
                        whileTrue,
                        searchForTask,
                        TaskInfo.Branch(
                            whileTrue,
                            new TaskInfo()
                        )
                    )
                );
            Assert.DoesNotThrow(() =>
            {
                SerializedTask.ToTaskInfo(m_protobufSerializer.DeepClone(SerializedTask.FromTaskInfo(task)));
            });
        }

        [Test]
        public void FindPathCloneTest()
        {
            TaskInfo task = TaskInfo.FindPath(new TaskInputInfo(), new TaskInputInfo());

            Assert.DoesNotThrow(() =>
            {
                SerializedTask.ToTaskInfo(m_protobufSerializer.DeepClone(SerializedTask.FromTaskInfo(task)));
            });
        }

        [Test]
        public void SearchForCloneTest()
        {
            TaskInfo task = TaskInfo.SearchFor(TaskType.SearchForFood, new TaskInputInfo());

            Assert.DoesNotThrow(() =>
            {
                SerializedTask.ToTaskInfo(m_protobufSerializer.DeepClone(SerializedTask.FromTaskInfo(task)));
            });
        }
        
        [Test]
        public void TaskInfoSerializationDeserialization()
        {
            TaskInfo taskInfo = new TaskInfo(TaskType.Branch, TaskState.Active);
            taskInfo.TaskId = 1234;
            taskInfo.Expression = new ExpressionInfo(ExpressionCode.FoodVisible, new PrimitiveContract<bool>(true));
            taskInfo.Children = new[]
            {
                new TaskInfo(new Cmd(CmdCode.RotateLeft), TaskState.Active, new ExpressionInfo(ExpressionCode.EnemyVisible, new PrimitiveContract<bool>(true)), taskInfo),
                new TaskInfo
                {
                    TaskId = 1236,
                    TaskType = TaskType.Command,
                    Cmd = new MovementCmd(CmdCode.Move, 10, 10),
                    Parent = taskInfo,
                    State = TaskState.Idle
                }
            };

            TaskInfo clone = null;
            Assert.DoesNotThrow(() =>
            {
                clone = SerializedTask.ToTaskInfo(m_protobufSerializer.DeepClone(SerializedTask.FromTaskInfo(taskInfo)));
            });

            Assert.IsNotNull(clone);
            Assert.AreEqual(taskInfo.TaskId, 1234);
            Assert.AreEqual(taskInfo.TaskId, clone.TaskId);
            Assert.AreEqual(taskInfo.TaskType, TaskType.Branch);
            Assert.AreEqual(taskInfo.TaskType, clone.TaskType);
            Assert.AreEqual(taskInfo.State, clone.State);
            Assert.AreEqual(taskInfo.Parent, clone.Parent);
            Assert.IsNotNull(clone.Expression);
            Assert.AreEqual(taskInfo.Expression.Code, clone.Expression.Code);
            Assert.IsNotNull(clone.Children);
            Assert.AreEqual(taskInfo.Children.Length, clone.Children.Length);
            Assert.AreSame(taskInfo.Children[0].Parent, taskInfo);
            Assert.AreSame(clone.Children[0].Parent, clone);
            Assert.IsNotNull(clone.Children[0].Expression);
            Assert.AreEqual(clone.Children[0].Expression.Code, ExpressionCode.EnemyVisible);
            Assert.AreEqual(taskInfo.Children[0].Expression.Code, clone.Children[0].Expression.Code);
            Assert.AreSame(taskInfo.Children[1].Parent, taskInfo);
            Assert.AreSame(clone.Children[1].Parent, clone);
            Assert.AreEqual(taskInfo.Children[1].TaskId, clone.Children[1].TaskId);
            Assert.IsNotNull(taskInfo.Children[1].Cmd);
            Assert.AreEqual(taskInfo.Children[1].Cmd.Code, clone.Children[1].Cmd.Code);
            Assert.AreEqual(taskInfo.Children[1].State, clone.Children[1].State);
        }
    }
}