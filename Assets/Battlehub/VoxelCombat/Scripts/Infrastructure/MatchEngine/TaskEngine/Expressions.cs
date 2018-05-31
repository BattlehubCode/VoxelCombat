using System;

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

    public class VarExpression : IExpression
    {
        public void Evaluate(ExpressionInfo expression, ITaskEngine taskEngine, Action<object> callback)
        {
            if(expression.Value is PrimitiveContract)
            {
                PrimitiveContract primitive = (PrimitiveContract)expression.Value;
                callback(primitive.ValueBase);
            }
            else
            {
                callback(expression.Value);
            }
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

    public class TaskStatusExpression : ExpressionBase
    {
        protected override void OnEvaluating(ExpressionInfo expression, ITaskEngine taskEngine, Action<object> callback)
        {
            ExpressionInfo completedExpression = expression.Children[0];
            ExpressionInfo failedExpression = expression.Children[1];

            taskEngine.GetExpression(completedExpression.Code).Evaluate(completedExpression, taskEngine, isCompleted =>
            {
                if ((bool)isCompleted)
                {
                    callback(TaskState.Completed);
                }
                else
                {
                    taskEngine.GetExpression(failedExpression.Code).Evaluate(failedExpression, taskEngine, isFailed =>
                    {
                        if ((bool)isFailed)
                        {
                            callback(TaskState.Failed);
                        }
                        else
                        {
                            callback(TaskState.Active);
                        }
                    });
                }
            });
        }
    }

}