using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface IAssignmentsController
    {
        void AddAssignment(Guid groupId, long unitId, SerializedTaskLaunchInfo taskLaunchInfo, bool hasTarget = false, int targetPlayerIndex = -1, long targetId = 0);

        /// <summary>
        /// Remove unit from assignment. Do not remove if assignment has target
        /// </summary>
        /// <param name="unitId"></param>
        void RemoveUnitFromAssignment(IMatchUnitAssetView unit);


        /// <summary>
        /// Remove target from assignment. Do not remove if assignment has unit
        /// </summary>
        /// <param name="targetId"></param>
        void RemoveTargetFromAssignments(IMatchUnitAssetView unitOrAsset);

        /// <summary>
        /// Remove assignment. Unconditional
        /// </summary>
        /// <param name="unitId"></param>
        void RemoveAssignment(IMatchUnitAssetView unit);

        void RemoveAssignmentGroup(Guid groupId);
    }

    public class AssignmentsController : IAssignmentsController
    {
        private readonly Dictionary<Guid, List<Assignment>> m_groupIdToAssignments = new Dictionary<Guid, List<Assignment>>();

        private IMatchPlayerView[] m_players;

        private int m_playerIndex;

        public AssignmentsController(int playerIndex, IMatchPlayerView[] players)
        {
            m_playerIndex = playerIndex;
            m_players = players;
        }

        public void AddAssignment(Guid groupId, long unitId, SerializedTaskLaunchInfo taskLaunchInfo, bool hasTarget = false, int targetPlayerIndex = -1, long targetId = 0)
        {
            IMatchPlayerView targetPlayerView = null;
            IMatchUnitAssetView targetUnitOrAsset = null;
            if (hasTarget)
            {
                targetPlayerView = m_players[targetPlayerIndex];
                targetUnitOrAsset = targetPlayerView.GetUnitOrAsset(targetId);
                if (targetUnitOrAsset == null || !targetUnitOrAsset.IsAlive)
                {
                    hasTarget = false;
                    targetId = 0;
                    targetPlayerIndex = -1;
                }
            }

            IMatchUnitAssetView unit = m_players[m_playerIndex].GetUnitOrAsset(unitId);

            Assignment assignment = new Assignment
            {
                GroupId = groupId,
                UnitId = unitId,
                HasUnit = true,
                TaskLaunchInfo = taskLaunchInfo,
                TargetPlayerIndex = targetPlayerIndex,
                TargetId = targetId,
                HasTarget = hasTarget
            };

            if (unit.Assignment != null)
            {
                RemoveUnitFromAssignment(unit);
            }

            unit.Assignment = assignment;
            if (hasTarget)
            {
                if (targetUnitOrAsset.TargetForAssignments == null)
                {
                    targetUnitOrAsset.TargetForAssignments = new List<Assignment>();
                }
                targetUnitOrAsset.TargetForAssignments.Add(assignment);
            }

            List<Assignment> group;
            if (!m_groupIdToAssignments.TryGetValue(groupId, out group))
            {
                group = new List<Assignment>();
                m_groupIdToAssignments.Add(groupId, group);
            }

            group.Add(assignment);
        }

        public void RemoveUnitFromAssignment(IMatchUnitAssetView unit)
        {
            Assignment assignment = unit.Assignment;
            if (assignment == null)
            {
                return;
            }
            unit.Assignment = null;

            assignment.HasUnit = false;
            assignment.UnitId = 0;

            if (!assignment.HasTarget)
            {
                List<Assignment> groupAssignments;
                if (m_groupIdToAssignments.TryGetValue(assignment.GroupId, out groupAssignments))
                {
                    groupAssignments.Remove(assignment);
                    if (groupAssignments.Count == 0)
                    {
                        m_groupIdToAssignments.Remove(assignment.GroupId);
                    }
                }
            }
        }

        public void RemoveTargetFromAssignments(IMatchUnitAssetView unitOrAsset)
        {
            List<Assignment> targetForAssignments = unitOrAsset.TargetForAssignments;
            if (targetForAssignments == null)
            {
                return;
            }
            unitOrAsset.TargetForAssignments = null;

            for (int i = 0; i < targetForAssignments.Count; ++i)
            {
                Assignment assignment = targetForAssignments[i];
                assignment.HasTarget = false;
                assignment.TargetId = 0;
                assignment.TargetPlayerIndex = -1;

                if (!assignment.HasUnit)
                {
                    List<Assignment> groupAssignments;
                    if (m_groupIdToAssignments.TryGetValue(assignment.GroupId, out groupAssignments))
                    {
                        groupAssignments.Remove(assignment);
                        if (groupAssignments.Count == 0)
                        {
                            m_groupIdToAssignments.Remove(assignment.GroupId);
                        }
                    }
                }
            }
        }

        public void RemoveAssignment(IMatchUnitAssetView unit)
        {
            Assignment assignment = unit.Assignment;
            if (assignment == null)
            {
                return;
            }
            RemoveAssignment(assignment);
        }

        private void RemoveAssignment(Assignment assignment)
        {
            if (assignment.HasUnit)
            {
                IMatchPlayerView playerView = m_players[m_playerIndex];
                IMatchUnitAssetView unitOrAsset = playerView.GetUnitOrAsset(assignment.UnitId);
                if (unitOrAsset != null)
                {
                    unitOrAsset.Assignment = null;
                }
            }

            assignment.HasUnit = false;
            assignment.UnitId = 0;

            if (assignment.HasTarget)
            {
                IMatchPlayerView targetPlayerView = m_players[assignment.TargetPlayerIndex];
                IMatchUnitAssetView targetUnitOrAsset = targetPlayerView.GetUnitOrAsset(assignment.TargetId);
                if (targetUnitOrAsset != null && targetUnitOrAsset.TargetForAssignments != null)
                {
                    targetUnitOrAsset.TargetForAssignments.Remove(assignment);
                    if (targetUnitOrAsset.TargetForAssignments.Count == 0)
                    {
                        targetUnitOrAsset.TargetForAssignments = null;

                        //destroy preview here
                    }
                }

                assignment.HasTarget = false;
                assignment.TargetId = 0;
                assignment.TargetPlayerIndex = -1;
            }

            List<Assignment> groupAssignments;
            if (m_groupIdToAssignments.TryGetValue(assignment.GroupId, out groupAssignments))
            {
                groupAssignments.Remove(assignment);
                if (groupAssignments.Count == 0)
                {
                    m_groupIdToAssignments.Remove(assignment.GroupId);
                }
            }
        }

        public void RemoveAssignmentGroup(Guid groupId)
        {
            List<Assignment> assignments;
            if (!m_groupIdToAssignments.TryGetValue(groupId, out assignments))
            {
                return;
            }

            assignments = assignments.ToList();
            for (int i = assignments.Count - 1; i >= 0; --i)
            {
                Assignment assignment = assignments[i];
                RemoveAssignment(assignment);
            }
        }
    }
}
