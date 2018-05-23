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
            Expression expression = new Expression();
            expression.Code = ExpressionCode.And;
            expression.Chidren = new[]
            {
                new Expression
                {
                    Code = ExpressionCode.Eq,
                    Chidren = new[]
                    {
                        new Expression
                        {
                            Code = ExpressionCode.Var,
                            Value = new Coordinate(1, 1, 1, 1)
                        },
                        new Expression
                        {
                            Code = ExpressionCode.Var,
                            Value = new Coordinate(1, 1, 1, 1)
                        },
                    }
                },
                new Expression
                {
                    Code = ExpressionCode.Var,
                    Value = PrimitiveContract.Create(true),
                }
            };

            Expression clone = null;
            Assert.DoesNotThrow(() =>
            {
                clone = ProtobufSerializer.DeepClone(expression);
            });

            Assert.IsNotNull(clone);
            Assert.AreEqual(expression.Code, clone.Code);
            Assert.IsNotNull(clone.Chidren);
            Assert.AreNotSame(expression.Chidren, clone.Chidren);
            Assert.AreEqual(expression.Chidren.Length, clone.Chidren.Length);
            Assert.IsNotNull(clone.Chidren[0].Chidren);
            Assert.AreEqual(expression.Chidren[0].Chidren.Length, clone.Chidren[0].Chidren.Length);
            Assert.AreEqual(expression.Chidren[0].Chidren[0].Code, clone.Chidren[0].Chidren[0].Code);
            Assert.AreEqual(expression.Chidren[0].Chidren[0].Value, clone.Chidren[0].Chidren[0].Value);
            Assert.AreEqual(expression.Chidren[1].Code, clone.Chidren[1].Code);
            Assert.AreEqual(expression.Chidren[1].Value, clone.Chidren[1].Value);
        }

        [Test]
        public void TaskInfoSerializationDeserialization()
        {
            TaskInfo taskInfo = new TaskInfo(TaskType.Branch, new Cmd(CmdCode.Nop), TaskState.Active, null);
            taskInfo.TaskId = 1234;
            taskInfo.Expression = new Expression(ExpressionCode.FoodVisible, new PrimitiveContract<bool>(true));
            taskInfo.Children = new[]
            {
                new TaskInfo(TaskType.Command, new Cmd(CmdCode.RotateLeft), TaskState.Active, new Expression(ExpressionCode.EnemyVisible, new PrimitiveContract<bool>(true)), taskInfo),
                new TaskInfo
                {
                    TaskId = 1236,
                    TaskType = TaskType.Command,
                    Cmd = new MovementCmd(CmdCode.MoveUnconditional, 10, 10),
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