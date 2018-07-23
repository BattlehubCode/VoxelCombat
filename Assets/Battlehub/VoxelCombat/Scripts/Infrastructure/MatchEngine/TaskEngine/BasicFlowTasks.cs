//#define DEBUG_OUTPUT
using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class BasicFlowTask : TaskBase
    {
        protected override void OnConstruct()
        {
            base.OnConstruct();
        }

        protected override void Reset()
        {
            m_taskInfo.Reset();
            m_taskInfo.State = TaskState.Active;
            if (m_taskInfo.Children != null)
            {
                for (int i = 0; i < m_taskInfo.Children.Length; ++i)
                {
                    if (m_taskInfo.Children[i] != null && m_taskInfo.Children[i].State != TaskState.Idle)
                    {
                        m_taskInfo.Children[i].Reset();
                    }
                }
            }
        }

        protected override void OnBreak()
        {
            m_taskInfo.State = TaskState.Completed;
            BreakParent();
        }

        protected override void OnContinue()
        {
            m_taskInfo.State = TaskState.Completed;
            ContinueParent();
        }
    }


    public class ProcedureTask : SequentialTask
    {
        protected override void ReturnParent()
        {
            
        }

        protected override void OnBreak()
        {
            throw new InvalidOperationException();
        }

        protected override void OnContinue()
        {
            throw new InvalidOperationException();
        }
    }


    public class SequentialTask : BasicFlowTask
    {
        protected int m_activeChildIndex;

        protected override void OnConstruct()
        {
            base.OnConstruct();
            Reset();
        }

        protected override void Reset()
        {
            base.Reset();
            m_activeChildIndex = -1;
            if (m_taskInfo.Children == null || m_taskInfo.Children.Length == 0)
            {
                m_taskInfo.State = TaskState.Completed;
            }
            else
            {
                ActivateNextTask();
            }
        }

        protected override void OnTick()
        {
            TaskInfo childTask = m_activeChildIndex >= 0 ? m_taskInfo.Children[m_activeChildIndex] : null;
            if (childTask == null || childTask.State != TaskState.Active)
            {
                ActivateNextTask();
            }
        }

        private void ActivateNextTask()
        {
            TaskInfo childTask;

            m_activeChildIndex++;
            if (m_activeChildIndex >= m_taskInfo.Children.Length)
            {
                m_activeChildIndex = -1;
                m_taskInfo.State = TaskState.Completed;
                return;
            }

            childTask = m_taskInfo.Children[m_activeChildIndex];
            RaiseChildTaskActivated(childTask);
            if (childTask.State != TaskState.Active && childTask.State != TaskState.Completed)
            {
                m_taskInfo.State = TaskState.Terminated;
            }
        }
    }

    public class BranchTask : BasicFlowTask
    {
        private TaskInfo m_childTask;

        protected override void OnReleased()
        {
            base.OnReleased();
            m_childTask = null;
        }

        protected override void OnConstruct()
        {
            base.OnConstruct();
       
            m_childTask = null;

            if (m_taskInfo.Expression.IsEvaluating)
            {
                throw new InvalidOperationException("Unable to reset while Expression is evaluating");
            }

            m_expression.Evaluate(m_taskInfo.Expression, m_taskEngine, value =>
            {
                #if DEBUG_OUTPUT
                Debug.Log("Branch " + m_taskInfo.TaskId + " evaluated to " + (bool)value);
                #endif
                int index = (bool)value ? 0 : 1;
                m_childTask = m_taskInfo.Children.Length > index ? m_taskInfo.Children[index] : null;
                if (m_childTask == null)
                {
                    //empty else or if block 
                    m_taskInfo.State = TaskState.Completed;
                }
                else
                {
                    m_childTask.State = TaskState.Active;
                    RaiseChildTaskActivated(m_childTask);
                    WaitChildTaskDeactivation();
                }
            });
        }

        protected override void OnTick()
        {
            if (!m_taskInfo.Expression.IsEvaluating)
            {
                WaitChildTaskDeactivation();
            }
        }

        private void WaitChildTaskDeactivation()
        {
            if (m_childTask.State != TaskState.Active)
            {
                if (m_childTask.State == TaskState.Completed)
                {
                    m_taskInfo.State = TaskState.Completed;
                }
                else
                {
                    m_taskInfo.State = TaskState.Terminated;
                }
            }
        }
    }

    public class RepeatTask : SequentialTask
    {
        private bool m_break;
        private bool m_continue;

        protected override void OnReleased()
        {
            base.OnReleased();
            m_break = false;
            m_continue = false;
        }

        protected override void Reset()
        {
            if (m_taskInfo.Expression.IsEvaluating)
            {
                throw new InvalidOperationException("Unable to reset while Expression is evaluating");
            }

            m_break = false;
            m_continue = false;

            EvaluateExpression(value =>
            {
                if (value)
                {
                    base.Reset();
                }
                else
                {
                    m_taskInfo.State = TaskState.Completed;
                }
            });
        }

        protected override void OnTick()
        {
            if (!m_taskInfo.Expression.IsEvaluating)
            {
                if(m_continue)
                {
                    m_continue = false;
                    Reset();
                }
                else if(m_break)
                {
                    m_break = false;
                    m_taskInfo.State = TaskState.Completed;
                }
                else
                {
                    base.OnTick();
                    if (m_taskInfo.State == TaskState.Completed)
                    {
                        Debug.Assert(!m_break);
                        Debug.Assert(!m_continue);

                        m_taskInfo.State = TaskState.Active;
                        Reset();
                    }
                }
            }
        }

        protected override void OnBreak()
        {
            m_break = true;
        }

        protected override void OnContinue()
        {
            m_continue = true;
        }

        protected override void ReturnParent()
        {
            m_break = true;
            base.ReturnParent();
        }

        protected virtual void EvaluateExpression(Action<bool> callback)
        {
            m_expression.Evaluate(m_taskInfo.Expression, m_taskEngine, value =>
            {
                callback((bool)value);
            });
        }
    }

    //public class ForeachTask : RepeatTask
    //{
    //    private int m_index = -1;
    //    private IList m_list;

    //    protected override void OnInitialized()
    //    {
    //        if (InputsCount != 1)
    //        {
    //            throw new ArgumentException("InputsCount != 1", "taskInfo");
    //        }

    //        if (m_taskInfo.OutputsCount < 1)
    //        {
    //            throw new ArgumentException("taskInfo.OutputsCount < 1", "taskInfo");
    //        }
    //    }

    //    protected override void OnConstruct()
    //    {
    //        m_list = ReadInput<IList>(m_taskInfo.Inputs[0]);
    //        if (m_list != null && m_list.Count > 0)
    //        {
    //            base.OnConstruct();
    //        }
    //        else
    //        {
    //            m_taskInfo.State = TaskState.Completed;
    //        }
    //    }

    //    protected override void EvaluateExpression(Action<bool> callback)
    //    {
    //        m_index++;
    //        if (m_index < m_list.Count)
    //        {
    //            WriteOutput(0, m_list[m_index]);
    //            WriteOutput(1, m_index);
    //            callback(true);
    //        }
    //        else
    //        {
    //            callback(false);
    //        }
    //    }
    //}

    public class BreakTask : TaskBase
    {
        protected override void OnTick()
        {
            m_taskInfo.State = TaskState.Completed;
            BreakParent();
        }
    }

    public class ContinueTask : TaskBase
    {
        protected override void OnTick()
        {
            m_taskInfo.State = TaskState.Completed;
            ContinueParent();
        }
    }

    public class ReturnTask : TaskBase
    {
        protected override void OnTick()
        {
            if (m_expression != null)
            {
                if (!m_taskInfo.Expression.IsEvaluating)
                {
                    m_expression.Evaluate(m_taskInfo.Expression, m_taskEngine, taskStatus =>
                    {
                        m_taskInfo.State = TaskState.Completed;
                        m_taskInfo.StatusCode = (int)taskStatus;
                        ReturnParent();
                    });
                }
            }
            else
            {
                m_taskInfo.State = TaskState.Completed;
                ReturnParent();
            }
        }
    }
}