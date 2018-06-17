using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Battlehub.VoxelCombat
{
    public interface IExpression
    {
        void Evaluate(ExpressionInfo expression, ITaskEngine taskEngine, Action<object> callback);
    }

    public abstract class ExpressionBase : IExpression
    {
        public void Evaluate(ExpressionInfo expression, ITaskEngine taskEngine, Action<object> callback)
        {
            if (expression.IsEvaluating)
            {
                throw new InvalidOperationException("expression.IsEvaluating == true");
            }

            OnEvaluating(expression, taskEngine, result =>
            {
                expression.IsEvaluating = false;
                callback(result);
            });
        }

        protected abstract void OnEvaluating(ExpressionInfo expression, ITaskEngine taskEngine, Action<object> callback);
    }

    public class ValueExpression : IExpression
    {
        public void Evaluate(ExpressionInfo expression, ITaskEngine taskEngine, Action<object> callback)
        {
            if(expression.Value is PrimitiveContract)
            {
                PrimitiveContract primitive = (PrimitiveContract)expression.Value;
                callback(primitive.ValueBase);
            }
            else if(expression.Value is TaskInputInfo)
            {
                TaskInputInfo input = (TaskInputInfo)expression.Value;
                object value = taskEngine.Memory.ReadOutput(input.Scope.TaskId, input.ConnectedTask.TaskId, input.OuputIndex);
                callback(value);
            }
            else
            {
                callback(expression.Value);
            }
        }
    }

    public class TaskStateExpression : IExpression
    {
        public void Evaluate(ExpressionInfo expression, ITaskEngine taskEngine, Action<object> callback)
        {
            TaskInfo taskInfo = (TaskInfo)expression.Value;
            callback(taskInfo.State);
        }
    }

    public class NotExpression : IExpression
    {
        public void Evaluate(ExpressionInfo expression, ITaskEngine taskEngine, Action<object> callback)
        {
            ExpressionInfo child = expression.Children[0];
            taskEngine.GetExpression(child.Code).Evaluate(child, taskEngine, value =>
            {
                callback(!((bool)value));
            });
       }
    }

    public abstract class BinaryExpression : ExpressionBase
    {
        protected override void OnEvaluating(ExpressionInfo expression, ITaskEngine taskEngine, Action<object> callback)
        {
            ExpressionInfo left = expression.Children[0];
            ExpressionInfo right = expression.Children[1];

            taskEngine.GetExpression(left.Code).Evaluate(left, taskEngine, lvalue =>
            {
                taskEngine.GetExpression(right.Code).Evaluate(right, taskEngine, rvalue =>
                {
                    OnEvaluating(lvalue, rvalue, callback);
                });
            });
        }
        protected abstract void OnEvaluating(object lvalue, object rvalue, Action<object> callback);
    }

    public class AddExpression : BinaryExpression
    {
        private Func<object, object, object> m_add;

        protected override void OnEvaluating(object lvalue, object rvalue, Action<object> callback)
        {
            if(lvalue is int)
            {
                callback((int)lvalue + (int)rvalue);
                return;
            }
            throw new NotSupportedException();
        }
    }

    public class SubExpression : BinaryExpression
    {
        protected override void OnEvaluating(object lvalue, object rvalue, Action<object> callback)
        {
            Type type = lvalue.GetType();
            var mi = type.GetMethod("op_Subtraction", BindingFlags.Static | BindingFlags.Public);
            object result = mi.Invoke(null, new[] { lvalue, rvalue });
            callback(result);
        }
    }


    public class AndExpression : BinaryExpression
    {
        protected override void OnEvaluating(object lvalue, object rvalue, Action<object> callback)
        {
            callback(((bool)lvalue) && ((bool)rvalue));
        }
    }

    public class OrExpression : BinaryExpression
    {
        protected override void OnEvaluating(object lvalue, object rvalue, Action<object> callback)
        {
            callback(((bool)lvalue) || ((bool)rvalue));
        }
    }


    public class EqExpression : BinaryExpression
    {
        protected override void OnEvaluating(object lvalue, object rvalue, Action<object> callback)
        {
            if (lvalue == null)
            {
                callback(rvalue == null);
            }
            else
            {
                callback(lvalue.Equals(rvalue));
            }
        }
    }

    public class NotEqExpression : BinaryExpression
    {
        protected override void OnEvaluating(object lvalue, object rvalue, Action<object> callback)
        {
            if (lvalue == null)
            {
                callback(rvalue != null);
            }
            else
            {
                callback(!lvalue.Equals(rvalue));
            }
        }
    }

    public abstract class UnitExpression : ExpressionBase
    {
        protected override void OnEvaluating(ExpressionInfo expression, ITaskEngine taskEngine, Action<object> callback)
        {
            long unitId = ((PrimitiveContract<long>)expression.Children[0].Value).Value;
            int playerId = ((PrimitiveContract<int>)expression.Children[1].Value).Value;

            IMatchPlayerView player = taskEngine.MatchEngine.GetPlayerView(playerId);
            IMatchUnitAssetView unit = player.GetUnitOrAsset(unitId);

            OnEvaluating(player, unit, taskEngine, callback);
        }

        protected abstract void OnEvaluating(IMatchPlayerView player, IMatchUnitAssetView unit, ITaskEngine taskEngine, Action<object> callback);
    }

    public class UnitExistsExpression : UnitExpression
    {
        protected override void OnEvaluating(IMatchPlayerView player, IMatchUnitAssetView unit, ITaskEngine taskEngine, Action<object> callback)
        {
            callback(unit != null);
        }
    }

    public class UnitCoordinateExpression : UnitExpression
    {
        protected override void OnEvaluating(IMatchPlayerView player, IMatchUnitAssetView unit, ITaskEngine taskEngine, Action<object> callback)
        {
            callback(unit.DataController.Coordinate);
        }
    }

    public class UnitStateExpression : UnitExpression
    {
        protected override void OnEvaluating(IMatchPlayerView player, IMatchUnitAssetView unit, ITaskEngine taskEngine, Action<object> callback)
        {
            VoxelDataState state = unit.DataController.GetVoxelDataState();
            callback(state);   
        }
    }
}