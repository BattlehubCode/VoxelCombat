using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace Battlehub.VoxelCombat.Tests
{
    public class TaskInfoTests
    {
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
                            Value = new Coordinate(1, 1, 1, 1)
                        },
                        new ExpressionInfo
                        {
                            Code = ExpressionCode.Value,
                            Value = new Coordinate(1, 1, 1, 1)
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
                clone = ProtobufSerializer.DeepClone(expression);
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
                clone = ProtobufSerializer.DeepClone(taskInfo);
            });

            Assert.IsNotNull(clone);
            Assert.AreEqual(taskInfo.TaskId, 1234);
            Assert.AreEqual(taskInfo.TaskId, clone.TaskId);
            Assert.AreEqual(taskInfo.TaskType, TaskType.Branch);
            Assert.AreEqual(taskInfo.TaskType, clone.TaskType);
            Assert.IsNotNull(clone.Cmd);
            Assert.AreEqual(taskInfo.Cmd.Code, CmdCode.Nop);
            Assert.AreEqual(taskInfo.Cmd.Code, clone.Cmd.Code);
            Assert.AreEqual(taskInfo.Cmd.UnitIndex, clone.Cmd.UnitIndex);
            Assert.AreEqual(taskInfo.Cmd.Duration, clone.Cmd.Duration);
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